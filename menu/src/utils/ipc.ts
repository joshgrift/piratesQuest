import type { MenuIpcMessage } from "../types";

// Small helper so components never stringify IPC payloads directly.
export function sendIpc(message: MenuIpcMessage): void {
  window.ipc?.postMessage(JSON.stringify(message));
}
