import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { sendIpc } from "./utils/ipc";
import type { GithubRelease, MenuServer, MenuState } from "./types";

const RELEASES_URL = "https://api.github.com/repos/joshgrift/piratesQuest/releases?per_page=6";
const DISCORD_INVITE_URL = "https://discord.gg/R9Fz54UNud";

const EMPTY_STATE: MenuState = {
  apiBaseUrl: "",
  version: "",
  username: "",
  isAuthenticated: false,
  statusMessage: "Connecting to game...",
  statusTone: "info",
  isAuthenticating: false,
  isBackgroundMuted: false,
  servers: [],
};

function formatReleaseDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Unknown date";
  }

  return date.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

function releaseBodyFallback(body: string): string {
  if (!body.trim()) {
    return "Open release notes for full details.";
  }

  // Keep all release details visible while trimming only trailing whitespace noise.
  return body.replace(/\s+$/g, "");
}

function serverLabel(server: MenuServer): string {
  return server.serverName?.trim() || "Unnamed Server";
}

function serverStatusLabel(status: string): string {
  const normalizedStatus = (status || "unknown").toLowerCase();
  if (normalizedStatus === "online") {
    return "Online";
  }

  if (normalizedStatus === "offline") {
    return "Offline";
  }

  return "Unknown";
}

function serverStatusClass(status: string): string {
  const normalizedStatus = (status || "unknown").toLowerCase();
  if (normalizedStatus === "online") {
    return "online";
  }

  if (normalizedStatus === "offline") {
    return "offline";
  }

  return "unknown";
}

function serverKey(server: MenuServer): string {
  return `${server.ipAddress}:${server.port}`;
}

export default function App() {
  const [menuState, setMenuState] = useState<MenuState>(EMPTY_STATE);

  // We keep one shared form state for login + signup to reduce duplicated inputs.
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [authMode, setAuthMode] = useState<"login" | "signup">("login");

  const [customAddress, setCustomAddress] = useState("");
  const [customPort, setCustomPort] = useState("7777");
  const [isCustomJoinOpen, setIsCustomJoinOpen] = useState(false);

  const [releases, setReleases] = useState<GithubRelease[]>([]);
  const [isLoadingReleases, setIsLoadingReleases] = useState(true);
  const [releaseError, setReleaseError] = useState("");

  useEffect(() => {
    // Godot pushes state snapshots through this callback.
    window.updateMenuState = (state: MenuState) => {
      setMenuState(state);

      // Prefill username when Godot already knows it.
      if (state.username) {
        setUsername((previousUsername) => previousUsername || state.username);
      }
    };

    sendIpc({ action: "ready" });

    return () => {
      window.updateMenuState = undefined;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadReleases() {
      setIsLoadingReleases(true);
      setReleaseError("");

      try {
        const response = await fetch(RELEASES_URL, {
          headers: {
            // Ask GitHub to include rendered markdown HTML in body_html.
            Accept: "application/vnd.github.full+json",
          },
        });

        if (!response.ok) {
          throw new Error(`GitHub returned ${response.status}`);
        }

        const payload = (await response.json()) as GithubRelease[];
        const published = payload.filter((release) => !release.draft);

        if (!cancelled) {
          setReleases(published);
        }
      } catch {
        if (!cancelled) {
          setReleaseError("Could not load release notes right now.");
        }
      } finally {
        if (!cancelled) {
          setIsLoadingReleases(false);
        }
      }
    }

    loadReleases();

    return () => {
      cancelled = true;
    };
  }, []);

  const canSubmitAuth = username.trim().length > 0 && password.length > 0 && !menuState.isAuthenticating;
  const statusClass = menuState.statusTone === "error" ? "status status-error" : "status status-info";

  const visibleReleases = useMemo(() => releases.slice(0, 5), [releases]);

  function submitAuth(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedUsername = username.trim();
    if (!trimmedUsername || !password) {
      return;
    }

    if (authMode === "signup") {
      sendIpc({ action: "signup", username: trimmedUsername, password });
      return;
    }

    sendIpc({ action: "login", username: trimmedUsername, password });
  }

  function joinServer(server: MenuServer) {
    sendIpc({ action: "join_server", ipAddress: server.ipAddress, port: server.port });
  }

  function joinCustomServer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const ipAddress = customAddress.trim();
    const port = Number.parseInt(customPort, 10);

    if (!ipAddress || Number.isNaN(port) || port < 1 || port > 65535) {
      return;
    }

    sendIpc({ action: "join_server", ipAddress, port });
  }

  return (
    <div className="menu-root">
      <div className="menu-overlay" />

      <div className="hud-top-left">
        <button
          className="discord-btn discord-btn-compact"
          onClick={() => sendIpc({ action: "open_url", url: DISCORD_INVITE_URL })}
          type="button"
        >
          Discord
        </button>
        <button
          className="ghost-btn"
          onClick={() => sendIpc({ action: "set_background_muted", muted: !menuState.isBackgroundMuted })}
          type="button"
        >
          {menuState.isBackgroundMuted ? "Unmute Sea" : "Mute Sea"}
        </button>

        {menuState.isAuthenticated && (
          <button className="ghost-btn" onClick={() => sendIpc({ action: "logout" })} type="button">
            Logout
          </button>
        )}
      </div>

      <main className="menu-shell">
        <section className="menu-main">
          <header className="hero">
            <h1>PiratesQuest</h1>
            <p className="hero-version">Version {menuState.version || "unknown"}</p>
          </header>

          {!menuState.isAuthenticated && (
            <section className="panel auth-panel">
              <div className="auth-tabs" role="tablist" aria-label="Account actions">
                <button
                  className={`tab-btn ${authMode === "login" ? "active" : ""}`}
                  onClick={() => setAuthMode("login")}
                  role="tab"
                  type="button"
                >
                  Login
                </button>
                <button
                  className={`tab-btn ${authMode === "signup" ? "active" : ""}`}
                  onClick={() => setAuthMode("signup")}
                  role="tab"
                  type="button"
                >
                  Sign Up
                </button>
              </div>

              <form className="auth-form" onSubmit={submitAuth}>
                <label>
                  Username
                  <input
                    autoComplete="username"
                    onChange={(event) => setUsername(event.target.value)}
                    placeholder="Captain name"
                    value={username}
                  />
                </label>

                <label>
                  Password
                  <input
                    autoComplete={authMode === "signup" ? "new-password" : "current-password"}
                    onChange={(event) => setPassword(event.target.value)}
                    placeholder="Secret phrase"
                    type="password"
                    value={password}
                  />
                </label>

                <button className="confirm-btn" disabled={!canSubmitAuth} type="submit">
                  {menuState.isAuthenticating
                    ? authMode === "signup"
                      ? "Signing Up..."
                      : "Logging In..."
                    : authMode === "signup"
                      ? "Create Account"
                      : "Enter The Sea"}
                </button>
              </form>
            </section>
          )}

          {menuState.isAuthenticated && (
            <>
              <section className="panel server-panel">
                <div className="panel-header">
                  <h2 className="server-title">Server Browser</h2>
                  <button className="ghost-action" onClick={() => sendIpc({ action: "refresh_servers" })} type="button">
                    Refresh
                  </button>
                </div>

                {menuState.servers.length === 0 ? (
                  <p className="empty-text">No ships on the horizon yet. Try refresh.</p>
                ) : (
                  <ul className="server-list">
                    {menuState.servers.map((server) => (
                      <li key={serverKey(server)}>
                        <article className="server-card">
                          <div className="server-card-head">
                            <div className="server-details">
                              <span className="server-name">{serverLabel(server)}</span>
                              <span className="server-meta">
                                <span className={`server-pill server-pill-${serverStatusClass(server.status)}`}>
                                  {serverStatusLabel(server.status)}
                                </span>
                                <span className="server-pill">{server.playerCount}/{server.playerMax} crew</span>
                                <span className="server-pill">v{server.serverVersion || "unknown"}</span>
                              </span>
                            </div>

                            <button className="join-chip" onClick={() => joinServer(server)} type="button">
                              Join
                            </button>
                          </div>
                          <p className="server-description">{server.description?.trim() || "No captain notes yet for this server."}</p>
                          <div className="server-card-foot">
                            <span className="server-address">{server.ipAddress}:{server.port}</span>
                          </div>
                        </article>
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section className="panel custom-panel">
                <div className="panel-header">
                  <h2>Custom Join</h2>
                  <button
                    className="arrow-toggle"
                    onClick={() => setIsCustomJoinOpen((isOpen) => !isOpen)}
                    type="button"
                    aria-label={isCustomJoinOpen ? "Collapse custom join" : "Expand custom join"}
                  >
                    {isCustomJoinOpen ? "v" : ">"}
                  </button>
                </div>

                {isCustomJoinOpen && (
                  <form className="custom-join-form" onSubmit={joinCustomServer}>
                    <label>
                      Address
                      <input
                        onChange={(event) => setCustomAddress(event.target.value)}
                        placeholder="127.0.0.1"
                        value={customAddress}
                      />
                    </label>

                    <label>
                      Port
                      <input
                        inputMode="numeric"
                        onChange={(event) => setCustomPort(event.target.value)}
                        placeholder="7777"
                        value={customPort}
                      />
                    </label>

                    <button className="confirm-btn" type="submit">
                      Sail To Server
                    </button>
                  </form>
                )}
              </section>
            </>
          )}

          <div className={statusClass} role="status" aria-live="polite">
            {menuState.statusMessage || "Ready."}
          </div>
        </section>

        <aside className="menu-side panel">
          <div className="panel-header">
            <h2>Release Notes</h2>
            <button
              className="ghost-action"
              onClick={() => sendIpc({ action: "open_url", url: "https://github.com/joshgrift/piratesQuest/releases" })}
              type="button"
            >
              Open All
            </button>
          </div>

          {isLoadingReleases && <p className="empty-text">Fetching latest releases...</p>}
          {!isLoadingReleases && releaseError && <p className="empty-text">{releaseError}</p>}

          {!isLoadingReleases && !releaseError && (
            <ul className="release-list">
              {visibleReleases.map((release) => (
                <li key={release.id}>
                  <button
                    className="release-card"
                    onClick={() => sendIpc({ action: "open_url", url: release.html_url })}
                    type="button"
                  >
                    <strong className="release-name">{release.name || release.tag_name}</strong>
                    <span className="release-date">{formatReleaseDate(release.published_at)}</span>
                    {release.body_html ? (
                      <div
                        className="release-markdown"
                        dangerouslySetInnerHTML={{ __html: release.body_html }}
                      />
                    ) : (
                      <p className="release-fallback">{releaseBodyFallback(release.body || "")}</p>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          )}

          <p className="api-line">API: {menuState.apiBaseUrl || "Unknown"}</p>
        </aside>
      </main>
    </div>
  );
}
