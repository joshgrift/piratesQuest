import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import App from "../App";
import { renderApp } from "./helpers";

describe("App", () => {
  it("renders nothing when no port is open", () => {
    const { container } = render(<App />);
    // App returns null when portState is null and not closing
    expect(container.innerHTML).toBe("");
  });

  it("shows the port panel after openPort is called", () => {
    const { container } = renderApp();
    // The port panel should now be in the DOM
    expect(container.querySelector(".port-panel")).toBeInTheDocument();
  });

  it("displays the port name", () => {
    const { getByText } = renderApp({ portName: "Nassau" });
    expect(getByText("Nassau")).toBeInTheDocument();
  });

  it("sends a ready IPC message on mount", () => {
    const { ipcSpy } = renderApp();
    // App sends { action: "ready" } when it first mounts
    expect(ipcSpy).toHaveBeenCalledWith(
      JSON.stringify({ action: "ready" }),
    );
  });
});
