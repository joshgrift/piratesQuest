// Godot -> menu webview state snapshot.
export interface MenuState {
  apiBaseUrl: string;
  version: string;
  username: string;
  isAuthenticated: boolean;
  statusMessage: string;
  statusTone: "info" | "error";
  isAuthenticating: boolean;
  isBackgroundMuted: boolean;
  servers: MenuServer[];
}

// One row in the server browser list.
export interface MenuServer {
  serverName: string;
  description: string;
  ipAddress: string;
  port: number;
  playerCount: number;
  playerMax: number;
  status: "online" | "offline" | "unknown" | string;
  serverVersion: string;
}

// Menu -> Godot messages sent over the webview bridge.
export type MenuIpcMessage =
  | { action: "ready" }
  | { action: "login"; username: string; password: string }
  | { action: "signup"; username: string; password: string }
  | { action: "logout" }
  | { action: "refresh_servers" }
  | { action: "join_server"; ipAddress: string; port: number }
  | { action: "set_background_muted"; muted: boolean }
  | { action: "open_url"; url: string };

// Release list row from GitHub Releases API.
export interface GithubRelease {
  id: number;
  name: string;
  tag_name: string;
  html_url: string;
  published_at: string;
  body: string;
  body_html?: string;
  draft: boolean;
  prerelease: boolean;
}

// Public API status payload used by the menu to compare client version vs latest.
export interface ApiStatusResponse {
  version: string;
  updatedAt?: string | null;
}

declare global {
  interface Window {
    ipc?: { postMessage: (json: string) => void };

    // Godot pushes menu state by calling this function with JSON data.
    updateMenuState?: (state: MenuState) => void;
  }
}
