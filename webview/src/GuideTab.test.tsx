import { describe, it, expect, vi } from "vitest";
import { screen, act, fireEvent, within } from "@testing-library/react";
import { renderApp } from "./test/helpers";

// The Guide tab is the default tab, so renderApp() lands here automatically.
function renderGuide() {
  return renderApp();
}

// Navigate to a node and skip the typewriter animation by clicking the
// speech bubble. This is simpler and more reliable than fake timers
// because the typewriter chains setTimeout calls (one per character).
function clickAndSkip(label: string, container: HTMLElement) {
  fireEvent.click(screen.getByText(label));
  const speech = container.querySelector(".guide-speech")!;
  fireEvent.click(speech);
}

describe("GuideTab", () => {
  // ── Portrait & chrome ─────────────────────────────────────────────

  describe("layout", () => {
    it("shows Scarlett's portrait", () => {
      renderGuide();
      expect(screen.getByAltText("Scarlett")).toBeInTheDocument();
    });

    it("shows Scarlett's name label beneath portrait", () => {
      const { container } = renderGuide();
      const nameEl = container.querySelector(".guide-name");
      expect(nameEl).toBeInTheDocument();
      expect(nameEl!.textContent).toBe("Scarlett");
    });
  });

  // ── Root dialogue ─────────────────────────────────────────────────

  describe("root dialogue", () => {
    it("shows the root dialogue text fully (no typewriter on root)", () => {
      renderGuide();
      expect(screen.getByText(/Ahoy there, sailor/)).toBeInTheDocument();
    });

    it("does not show typing cursor on root node", () => {
      const { container } = renderGuide();
      expect(container.querySelector(".guide-cursor")).not.toBeInTheDocument();
    });

    it("does not show 'click to skip' hint on root node", () => {
      renderGuide();
      expect(screen.queryByText("click to skip")).not.toBeInTheDocument();
    });

    it("shows all root response buttons", () => {
      renderGuide();
      expect(screen.getByText("How do I sail my ship?")).toBeInTheDocument();
      expect(screen.getByText("How does trading work?")).toBeInTheDocument();
      expect(screen.getByText("Tell me about combat")).toBeInTheDocument();
      expect(screen.getByText("What are resources for?")).toBeInTheDocument();
      expect(screen.getByText("How do I collect resources?")).toBeInTheDocument();
      expect(screen.getByText("How do ship upgrades work?")).toBeInTheDocument();
      expect(screen.getByText("What if I'm overburdened?")).toBeInTheDocument();
      expect(screen.getByText("How does the leaderboard work?")).toBeInTheDocument();
      expect(screen.getByText("What happens when I die?")).toBeInTheDocument();
      expect(screen.getByText("What can I do at ports?")).toBeInTheDocument();
    });

    it("shows numbered badges on response buttons", () => {
      const { container } = renderGuide();
      const nums = container.querySelectorAll(".guide-response-num");
      expect(nums).toHaveLength(10);
      expect(nums[0]!.textContent).toBe("1");
      expect(nums[9]!.textContent).toBe("10");
    });
  });

  // ── Navigation ────────────────────────────────────────────────────

  describe("navigation", () => {
    it("navigates to sailing topic when clicked", () => {
      const { container } = renderGuide();
      clickAndSkip("How do I sail my ship?", container);
      expect(screen.getByText(/Sailin' is simple/)).toBeInTheDocument();
    });

    it("shows 'Ask about something else' to return to root", () => {
      const { container } = renderGuide();
      clickAndSkip("How do I sail my ship?", container);
      expect(screen.getByText("Ask about something else")).toBeInTheDocument();
    });

    it("returns to root when 'Ask about something else' is clicked", () => {
      const { container } = renderGuide();
      clickAndSkip("How do I sail my ship?", container);
      fireEvent.click(screen.getByText("Ask about something else"));
      // Root text appears immediately (no typewriter on root)
      expect(screen.getByText(/Ahoy there, sailor/)).toBeInTheDocument();
    });
  });

  // ── Typewriter effect ─────────────────────────────────────────────

  describe("typewriter effect", () => {
    it("starts with typewriter animation on non-root nodes", () => {
      const { container } = renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));
      expect(container.querySelector(".guide-cursor")).toBeInTheDocument();
    });

    it("shows 'click to skip' during typewriter animation", () => {
      renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));
      expect(screen.getByText("click to skip")).toBeInTheDocument();
    });

    it("hides responses while typing", () => {
      renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));
      expect(screen.queryByText("Any sailing tips?")).not.toBeInTheDocument();
    });

    it("shows responses after typewriter completes (via skip)", () => {
      const { container } = renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));
      // Skip the animation
      fireEvent.click(container.querySelector(".guide-speech")!);
      expect(screen.getByText("Any sailing tips?")).toBeInTheDocument();
    });

    it("clicking the speech bubble skips the typewriter", () => {
      const { container } = renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));
      fireEvent.click(container.querySelector(".guide-speech")!);

      expect(screen.getByText(/Sailin' is simple/)).toBeInTheDocument();
      expect(screen.getByText("Any sailing tips?")).toBeInTheDocument();
      expect(container.querySelector(".guide-cursor")).not.toBeInTheDocument();
    });

    it("progressively reveals text one character at a time", () => {
      vi.useFakeTimers();
      const { container } = renderGuide();
      fireEvent.click(screen.getByText("How do I sail my ship?"));

      const getText = () =>
        container.querySelector(".guide-text")!.textContent!.replace("▌", "");

      // charIndex starts at 0 (empty text), first character appears after 25ms
      expect(getText().length).toBe(0);

      act(() => { vi.advanceTimersByTime(25); });
      expect(getText().length).toBe(1);

      act(() => { vi.advanceTimersByTime(25); });
      expect(getText().length).toBe(2);

      act(() => { vi.advanceTimersByTime(25); });
      expect(getText().length).toBe(3);

      vi.useRealTimers();
    });
  });

  // ── Quiz flows ────────────────────────────────────────────────────

  describe("quiz: sailing", () => {
    function navigateToSailingQuiz(container: HTMLElement) {
      clickAndSkip("How do I sail my ship?", container);
      clickAndSkip("Any sailing tips?", container);
    }

    it("shows correct response for right answer", () => {
      const { container } = renderGuide();
      navigateToSailingQuiz(container);
      clickAndSkip("It turns slower", container);
      expect(screen.getByText(/Sharp as a cutlass/)).toBeInTheDocument();
    });

    it("shows feedback for wrong answer", () => {
      const { container } = renderGuide();
      navigateToSailingQuiz(container);
      clickAndSkip("It goes faster", container);
      expect(screen.getByText(/Not quite, love/)).toBeInTheDocument();
    });
  });

  describe("quiz: trading", () => {
    function navigateToTradingQuiz(container: HTMLElement) {
      clickAndSkip("How does trading work?", container);
      clickAndSkip("How do I buy and sell?", container);
    }

    it("correct trading answer shows success response", () => {
      const { container } = renderGuide();
      navigateToTradingQuiz(container);
      clickAndSkip("Buy low at one port, sell high at another", container);
      expect(screen.getByText(/merchant prince/)).toBeInTheDocument();
    });

    it("wrong trading answer shows correction", () => {
      const { container } = renderGuide();
      navigateToTradingQuiz(container);
      clickAndSkip("Sell everything at the first port", container);
      expect(screen.getByText(/Bless yer heart/)).toBeInTheDocument();
    });
  });

  describe("quiz: combat", () => {
    function navigateToCombatQuiz(container: HTMLElement) {
      clickAndSkip("Tell me about combat", container);
      clickAndSkip("Any combat tips?", container);
    }

    it("correct answer (Q) shows success", () => {
      const { container } = renderGuide();
      navigateToCombatQuiz(container);
      clickAndSkip("Q", container);
      expect(screen.getByText(/Q for port, E for starboard/)).toBeInTheDocument();
    });

    it("wrong answer shows correction", () => {
      const { container } = renderGuide();
      navigateToCombatQuiz(container);
      clickAndSkip("E", container);
      expect(screen.getByText(/Almost!/)).toBeInTheDocument();
    });
  });

  describe("quiz: resources", () => {
    function navigateToResourcesQuiz(container: HTMLElement) {
      clickAndSkip("What are resources for?", container);
      clickAndSkip("What resources are there?", container);
    }

    it("correct answer (Wood and Fish) shows success", () => {
      const { container } = renderGuide();
      navigateToResourcesQuiz(container);
      clickAndSkip("Wood and Fish", container);
      expect(screen.getByText(/5 Wood and 1 Fish/)).toBeInTheDocument();
    });

    it("wrong answer shows correction", () => {
      const { container } = renderGuide();
      navigateToResourcesQuiz(container);
      clickAndSkip("Just Gold", container);
      expect(screen.getByText(/Not quite!/)).toBeInTheDocument();
    });
  });

  describe("quiz: upgrades", () => {
    function navigateToUpgradesQuiz(container: HTMLElement) {
      clickAndSkip("How do ship upgrades work?", container);
      clickAndSkip("How do I equip them?", container);
    }

    it("correct answer (all lost) shows success", () => {
      const { container } = renderGuide();
      navigateToUpgradesQuiz(container);
      clickAndSkip("They're all lost", container);
      expect(screen.getByText(/harsh truth of the sea/)).toBeInTheDocument();
    });

    it("wrong answer shows correction", () => {
      const { container } = renderGuide();
      navigateToUpgradesQuiz(container);
      clickAndSkip("They stay equipped", container);
      expect(screen.getByText(/I wish, love!/)).toBeInTheDocument();
    });
  });

  // ── Deep navigation paths ─────────────────────────────────────────

  describe("deep navigation", () => {
    it("navigates collecting → how → upgrades chain", () => {
      const { container } = renderGuide();
      clickAndSkip("How do I collect resources?", container);
      clickAndSkip("How does it work exactly?", container);
      clickAndSkip("Can I collect faster?", container);
      expect(screen.getByText(/Advanced Fish Nets/)).toBeInTheDocument();
    });

    it("navigates overburdened → effects", () => {
      const { container } = renderGuide();
      clickAndSkip("What if I'm overburdened?", container);
      clickAndSkip("What happens when I'm heavy?", container);
      expect(screen.getByText(/turns slower and handles like a brick/)).toBeInTheDocument();
    });

    it("navigates death → protect", () => {
      const { container } = renderGuide();
      clickAndSkip("What happens when I die?", container);
      clickAndSkip("How can I protect my stuff?", container);
      expect(screen.getByText(/spend yer resources before headin' into danger/)).toBeInTheDocument();
    });

    it("navigates leaderboard → trophies", () => {
      const { container } = renderGuide();
      clickAndSkip("How does the leaderboard work?", container);
      clickAndSkip("How do I get trophies?", container);
      expect(screen.getByText(/sinkin' other players/)).toBeInTheDocument();
    });

    it("navigates ports → features", () => {
      const { container } = renderGuide();
      clickAndSkip("What can I do at ports?", container);
      clickAndSkip("What can I do here?", container);
      expect(screen.getByText(/Plenty!/)).toBeInTheDocument();
    });

    it("navigates combat → targets", () => {
      const { container } = renderGuide();
      clickAndSkip("Tell me about combat", container);
      clickAndSkip("What can I fight?", container);
      expect(screen.getByText(/Other players are the biggest prize/)).toBeInTheDocument();
    });
  });
});
