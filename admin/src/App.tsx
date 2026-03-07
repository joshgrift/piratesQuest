import { FormEvent, useEffect, useMemo, useState } from "react";

type UserRole = "Player" | "Mod" | "Admin";

type UserSummary = {
  id: number;
  username: string;
  role: UserRole;
  createdAt: string;
};

type GameServer = {
  id: number;
  name: string;
  description: string;
  address: string;
  port: number;
  isActive: boolean;
  playerCount: number;
  playerMax: number;
  serverVersion: string;
  lastSeenUtc: string | null;
  createdAt: string;
};

type StatusServer = {
  name: string;
  lastSeenUtc: string | null;
};

type PublicStatus = {
  version: string;
  updatedAt: string | null;
  servers: StatusServer[];
};

type ServerDraft = {
  name: string;
  description: string;
};

const TOKEN_STORAGE_KEY = "pq_admin_token";
const API_STORAGE_KEY = "pq_admin_api_url";

class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function normalizeApiBase(input: string): string {
  return input.trim().replace(/\/+$/, "");
}

function prettyDate(value: string | null): string {
  if (!value) {
    return "Never";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
}

async function apiRequest<T>(
  baseUrl: string,
  path: string,
  method = "GET",
  token = "",
  body?: unknown
): Promise<T> {
  const headers: Record<string, string> = {};

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  if (!response.ok) {
    let message = `HTTP ${response.status}`;

    if (text) {
      try {
        const errorBody = JSON.parse(text) as { error?: string };
        message = errorBody.error ?? text;
      } catch {
        message = text;
      }
    }

    throw new ApiError(response.status, message);
  }

  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}

function defaultApiUrl(): string {
  const stored = localStorage.getItem(API_STORAGE_KEY);
  if (stored) {
    return normalizeApiBase(stored);
  }

  return normalizeApiBase(window.location.origin || "http://localhost:5236");
}

export default function App() {
  const [apiBaseUrl, setApiBaseUrl] = useState(defaultApiUrl);
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_STORAGE_KEY) ?? "");

  const [status, setStatus] = useState<PublicStatus | null>(null);
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [servers, setServers] = useState<GameServer[]>([]);
  const [serverDrafts, setServerDrafts] = useState<Record<number, ServerDraft>>({});

  const [versionInput, setVersionInput] = useState("");
  const [loginUser, setLoginUser] = useState("");
  const [loginPass, setLoginPass] = useState("");

  const [newServerName, setNewServerName] = useState("");
  const [newServerAddress, setNewServerAddress] = useState("");
  const [newServerPort, setNewServerPort] = useState("7777");
  const [newServerDescription, setNewServerDescription] = useState("");

  const [isLoading, setIsLoading] = useState(false);
  const [notice, setNotice] = useState("Ready.");
  const [error, setError] = useState("");

  const isAuthed = token.trim().length > 0;

  const sortedUsers = useMemo(
    () => [...users].sort((a, b) => a.username.localeCompare(b.username)),
    [users]
  );

  const sortedServers = useMemo(
    () => [...servers].sort((a, b) => a.name.localeCompare(b.name)),
    [servers]
  );

  useEffect(() => {
    localStorage.setItem(API_STORAGE_KEY, apiBaseUrl);
  }, [apiBaseUrl]);

  useEffect(() => {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
  }, [token]);

  useEffect(() => {
    void refreshStatus();
  }, [apiBaseUrl]);

  useEffect(() => {
    if (isAuthed) {
      void refreshAdminData(token);
    }
  }, [apiBaseUrl, isAuthed, token]);

  async function refreshStatus() {
    try {
      const payload = await apiRequest<PublicStatus>(apiBaseUrl, "/api/status");
      setStatus(payload);
      setVersionInput(payload.version ?? "");
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function refreshAdminData(tokenOverride?: string) {
    const activeToken = tokenOverride ?? token;
    if (!activeToken) {
      return;
    }

    setIsLoading(true);
    setError("");

    try {
      const [userPayload, serverPayload] = await Promise.all([
        apiRequest<UserSummary[]>(apiBaseUrl, "/api/management/users", "GET", activeToken),
        apiRequest<GameServer[]>(apiBaseUrl, "/api/management/servers", "GET", activeToken),
      ]);

      setUsers(userPayload);
      setServers(serverPayload);

      // We keep local draft values so edits are explicit before clicking save.
      const nextDrafts: Record<number, ServerDraft> = {};
      for (const server of serverPayload) {
        nextDrafts[server.id] = { name: server.name, description: server.description ?? "" };
      }
      setServerDrafts(nextDrafts);

      setNotice("Admin data refreshed.");
    } catch (requestError) {
      setError(getMessage(requestError));
    } finally {
      setIsLoading(false);
    }
  }

  async function onLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setNotice("Logging in...");

    try {
      const payload = await apiRequest<{ token: string }>(apiBaseUrl, "/api/login", "POST", "", {
        username: loginUser.trim(),
        password: loginPass,
      });

      if (!payload.token) {
        throw new Error("No token returned from API.");
      }

      setToken(payload.token);
      setLoginPass("");
      setNotice("Logged in. Loading admin data...");
      await refreshAdminData(payload.token);
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  function onLogout() {
    setToken("");
    setUsers([]);
    setServers([]);
    setServerDrafts({});
    setNotice("Logged out.");
  }

  async function onSetVersion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token) {
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, "/api/management/version", "POST", token, {
        version: versionInput.trim(),
      });
      setNotice("Version updated.");
      await refreshStatus();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function onSetRole(userId: number, role: UserRole) {
    if (!token) {
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, `/api/management/user/${userId}/role`, "PUT", token, { role });
      setNotice(`Updated role for user ${userId}.`);
      await refreshAdminData();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function onAddServer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token) {
      return;
    }

    const parsedPort = Number.parseInt(newServerPort, 10);
    if (Number.isNaN(parsedPort) || parsedPort < 1 || parsedPort > 65535) {
      setError("Port must be between 1 and 65535.");
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, "/api/management/server", "PUT", token, {
        name: newServerName.trim(),
        address: newServerAddress.trim(),
        port: parsedPort,
        description: newServerDescription.trim(),
      });

      setNewServerName("");
      setNewServerAddress("");
      setNewServerPort("7777");
      setNewServerDescription("");
      setNotice("Server added.");
      await refreshAdminData();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function onSaveServerName(serverId: number) {
    if (!token) {
      return;
    }

    const draft = serverDrafts[serverId];
    if (!draft) {
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, `/api/management/server/${serverId}/name`, "PATCH", token, {
        name: draft.name.trim(),
      });
      setNotice(`Renamed server ${serverId}.`);
      await refreshAdminData();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function onSaveServerDescription(serverId: number) {
    if (!token) {
      return;
    }

    const draft = serverDrafts[serverId];
    if (!draft) {
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, `/api/management/server/${serverId}/description`, "PATCH", token, {
        description: draft.description,
      });
      setNotice(`Updated description for server ${serverId}.`);
      await refreshAdminData();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  async function onDeleteServer(serverId: number) {
    if (!token) {
      return;
    }

    const shouldDelete = window.confirm(`Delete server ${serverId}?`);
    if (!shouldDelete) {
      return;
    }

    try {
      setError("");
      await apiRequest(apiBaseUrl, `/api/management/server/${serverId}`, "DELETE", token);
      setNotice(`Deleted server ${serverId}.`);
      await refreshAdminData();
    } catch (requestError) {
      setError(getMessage(requestError));
    }
  }

  function updateServerDraft(serverId: number, next: Partial<ServerDraft>) {
    setServerDrafts((current) => {
      const previous = current[serverId] ?? { name: "", description: "" };
      return {
        ...current,
        [serverId]: {
          ...previous,
          ...next,
        },
      };
    });
  }

  return (
    <div className="admin-page">
      <div className="fog fog-left" />
      <div className="fog fog-right" />

      <header className="hero">
        <div>
          <p className="eyebrow">PiratesQuest</p>
          <h1>Captain Admin Panel</h1>
          <p className="subtitle">Web replacement for manage.sh. No backend changes required.</p>
        </div>

        <div className="api-config">
          <label htmlFor="api-url">API URL</label>
          <input
            id="api-url"
            onBlur={(event) => setApiBaseUrl(normalizeApiBase(event.target.value))}
            onChange={(event) => setApiBaseUrl(event.target.value)}
            value={apiBaseUrl}
          />
          <div className="api-shortcuts">
            <button type="button" onClick={() => setApiBaseUrl("http://localhost:5236")}>Local</button>
            <button type="button" onClick={() => setApiBaseUrl("https://pirates.quest")}>Production</button>
          </div>
        </div>
      </header>

      <section className="status-strip">
        <strong>Status:</strong> {notice}
        {error && <span className="error">Error: {error}</span>}
      </section>

      <main className="layout">
        <section className="card">
          <h2>Public Status</h2>
          <button type="button" onClick={() => void refreshStatus()}>Refresh Status</button>
          <p>
            <strong>Version:</strong> {status?.version ?? "unknown"}
          </p>
          <p>
            <strong>Updated:</strong> {prettyDate(status?.updatedAt ?? null)}
          </p>
          <ul>
            {(status?.servers ?? []).map((server) => (
              <li key={server.name}>{server.name}: last seen {prettyDate(server.lastSeenUtc)}</li>
            ))}
          </ul>
        </section>

        <section className="card">
          <h2>Admin Login</h2>
          {!isAuthed ? (
            <form onSubmit={onLogin} className="stack-form">
              <label>
                Username
                <input value={loginUser} onChange={(event) => setLoginUser(event.target.value)} autoComplete="username" />
              </label>
              <label>
                Password
                <input
                  type="password"
                  value={loginPass}
                  onChange={(event) => setLoginPass(event.target.value)}
                  autoComplete="current-password"
                />
              </label>
              <button type="submit">Login</button>
            </form>
          ) : (
            <div className="auth-ok">
              <p>Token loaded and ready.</p>
              <div className="row-buttons">
                <button type="button" onClick={() => void refreshAdminData()} disabled={isLoading}>
                  {isLoading ? "Loading..." : "Refresh Admin Data"}
                </button>
                <button type="button" onClick={onLogout} className="secondary">Logout</button>
              </div>
            </div>
          )}
        </section>

        <section className="card">
          <h2>Set Game Version</h2>
          <form onSubmit={onSetVersion} className="stack-form">
            <label>
              Version
              <input value={versionInput} onChange={(event) => setVersionInput(event.target.value)} placeholder="0.5.1" />
            </label>
            <button type="submit" disabled={!isAuthed}>Update Version</button>
          </form>
        </section>

        <section className="card full-width">
          <h2>Users</h2>
          <p className="help">Matches: manage.sh users + set-role</p>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Username</th>
                  <th>Role</th>
                  <th>Created</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {sortedUsers.map((user) => (
                  <tr key={user.id}>
                    <td>{user.id}</td>
                    <td>{user.username}</td>
                    <td>
                      <select
                        value={user.role}
                        onChange={(event) => void onSetRole(user.id, event.target.value as UserRole)}
                        disabled={!isAuthed}
                      >
                        <option value="Player">Player</option>
                        <option value="Mod">Mod</option>
                        <option value="Admin">Admin</option>
                      </select>
                    </td>
                    <td>{prettyDate(user.createdAt)}</td>
                    <td>Auto-save on role change</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="card full-width">
          <h2>Servers</h2>
          <p className="help">Matches: servers, add-server, rename-server, set-server-description, rm-server</p>

          <form onSubmit={onAddServer} className="server-create">
            <input
              placeholder="Server name"
              value={newServerName}
              onChange={(event) => setNewServerName(event.target.value)}
            />
            <input
              placeholder="Address"
              value={newServerAddress}
              onChange={(event) => setNewServerAddress(event.target.value)}
            />
            <input placeholder="Port" value={newServerPort} onChange={(event) => setNewServerPort(event.target.value)} />
            <input
              placeholder="Description"
              value={newServerDescription}
              onChange={(event) => setNewServerDescription(event.target.value)}
            />
            <button type="submit" disabled={!isAuthed}>Add Server</button>
          </form>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Address</th>
                  <th>Runtime</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {sortedServers.map((server) => {
                  const draft = serverDrafts[server.id] ?? {
                    name: server.name,
                    description: server.description ?? "",
                  };

                  return (
                    <tr key={server.id}>
                      <td>{server.id}</td>
                      <td>
                        <input
                          value={draft.name}
                          onChange={(event) => updateServerDraft(server.id, { name: event.target.value })}
                        />
                      </td>
                      <td>
                        <textarea
                          value={draft.description}
                          onChange={(event) => updateServerDraft(server.id, { description: event.target.value })}
                          rows={2}
                        />
                      </td>
                      <td>
                        {server.address}:{server.port}
                        <br />
                        active={String(server.isActive)}
                      </td>
                      <td>
                        {server.playerCount}/{server.playerMax}
                        <br />
                        v{server.serverVersion || "unknown"}
                        <br />
                        last {prettyDate(server.lastSeenUtc)}
                      </td>
                      <td>
                        <div className="row-buttons">
                          <button type="button" onClick={() => void onSaveServerName(server.id)} disabled={!isAuthed}>
                            Save Name
                          </button>
                          <button
                            type="button"
                            onClick={() => void onSaveServerDescription(server.id)}
                            disabled={!isAuthed}
                          >
                            Save Desc
                          </button>
                          <button
                            type="button"
                            className="danger"
                            onClick={() => void onDeleteServer(server.id)}
                            disabled={!isAuthed}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}

function getMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return "Unauthorized. Login again.";
    }

    if (error.status === 403) {
      return "Admin access required.";
    }

    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Unknown error.";
}
