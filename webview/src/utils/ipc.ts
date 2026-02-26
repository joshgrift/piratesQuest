import type { IpcMessage, PortState } from "../types";

// Send an IPC message to Godot via the webview bridge.
export function sendIpc(msg: IpcMessage): void {
  window.ipc?.postMessage(JSON.stringify(msg));
}

/**
 * Send an IPC message and wait for Godot to push updated state back
 * via window.updateState(). This lets the UI chain dependent actions
 * (e.g. buy materials, then heal) without a race condition.
 */
export function sendIpcAndWait(msg: IpcMessage): Promise<PortState> {
  return new Promise<PortState>((resolve) => {
    const prev = window.updateState;
    window.updateState = (data: PortState) => {
      window.updateState = prev;
      prev?.(data);
      resolve(data);
    };
    sendIpc(msg);
  });
}
