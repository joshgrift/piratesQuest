import { useEffect, useMemo, useRef, useState } from "react";
import type { ConversationResponse, ConversationTree } from "./ConversationPanel";

interface ConversationThreadPanelProps {
  tree: ConversationTree;
  speakerPortraitSrc: string;
  speakerPortraitAlt: string;
  initialNodeId?: string;
  instantNodeIds?: string[];
  typingSpeedMs?: number;
  onAction?: (actionId: string) => string | void;
}

type ThreadRole = "npc" | "player";

interface ThreadMessage {
  id: number;
  role: ThreadRole;
  text: string;
  visibleChars: number;
}

function asCompleteMessage(id: number, role: ThreadRole, text: string): ThreadMessage {
  return {
    id,
    role,
    text,
    visibleChars: text.length,
  };
}

export function ConversationThreadPanel({
  tree,
  speakerPortraitSrc,
  speakerPortraitAlt,
  initialNodeId = "root",
  instantNodeIds = [],
  typingSpeedMs = 16,
  onAction,
}: ConversationThreadPanelProps) {
  const [nodeId, setNodeId] = useState(initialNodeId);
  const [messages, setMessages] = useState<ThreadMessage[]>([]);
  const nextMessageIdRef = useRef(1);
  const logRef = useRef<HTMLDivElement | null>(null);

  const currentNode = tree[nodeId] ?? tree[initialNodeId] ?? null;
  const lastMessage = messages[messages.length - 1];
  const isTyping = !!lastMessage
    && lastMessage.role === "npc"
    && lastMessage.visibleChars < lastMessage.text.length;

  const appendNpcMessage = (
    text: string,
    shouldRenderInstant: boolean,
  ) => {
    const id = nextMessageIdRef.current;
    nextMessageIdRef.current += 1;
    setMessages((prev) => [
      ...prev,
      {
        id,
        role: "npc",
        text,
        visibleChars: shouldRenderInstant ? text.length : 0,
      },
    ]);
  };

  // Reset the threaded transcript when the conversation source changes.
  useEffect(() => {
    const startNode = tree[initialNodeId] ?? null;
    setNodeId(initialNodeId);
    nextMessageIdRef.current = 1;

    if (!startNode) {
      setMessages([]);
      return;
    }

    const shouldInstant = instantNodeIds.includes(initialNodeId);
    setMessages([
      {
        id: 0,
        role: "npc",
        text: startNode.text,
        visibleChars: shouldInstant ? startNode.text.length : 0,
      },
    ]);
  }, [tree, initialNodeId, instantNodeIds]);

  // Typewriter effect for the latest NPC message only.
  useEffect(() => {
    if (!isTyping) return;

    const timeoutId = window.setTimeout(() => {
      setMessages((prev) => {
        if (prev.length === 0) return prev;
        const copy = [...prev];
        const latest = copy[copy.length - 1];
        if (!latest) return prev;

        if (latest.role !== "npc") return prev;
        if (latest.visibleChars >= latest.text.length) return prev;

        copy[copy.length - 1] = {
          ...latest,
          visibleChars: latest.visibleChars + 1,
        };

        return copy;
      });
    }, typingSpeedMs);

    return () => window.clearTimeout(timeoutId);
  }, [isTyping, typingSpeedMs, messages]);

  // Keep the latest message in view as text streams in.
  useEffect(() => {
    if (!logRef.current) return;
    logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [messages, isTyping]);

  const skipTyping = () => {
    if (!isTyping) return;

    setMessages((prev) => {
      if (prev.length === 0) return prev;
      const copy = [...prev];
      const latest = copy[copy.length - 1];
      if (!latest) return prev;
      if (latest.role !== "npc") return prev;

      copy[copy.length - 1] = {
        ...latest,
        visibleChars: latest.text.length,
      };

      return copy;
    });
  };

  const navigateToNode = (nextNodeId: string) => {
    const nextNode = tree[nextNodeId];
    if (!nextNode) return;

    setNodeId(nextNodeId);
    appendNpcMessage(nextNode.text, instantNodeIds.includes(nextNodeId));
  };

  const handleResponse = (response: ConversationResponse) => {
    if (response.disabled) return;
    if (!currentNode) return;

    const id = nextMessageIdRef.current;
    nextMessageIdRef.current += 1;
    setMessages((prev) => [...prev, asCompleteMessage(id, "player", response.label)]);

    let nextId = response.next;

    if (response.action && onAction) {
      const redirect = onAction(response.action);
      if (redirect) nextId = redirect;
    }

    if (nextId) navigateToNode(nextId);
  };

  const renderedMessages = useMemo(
    () => messages.map((message) => ({
      ...message,
      renderedText: message.text.slice(0, message.visibleChars),
    })),
    [messages],
  );

  return (
    <div className="conversation-thread-shell">
      <div className="conversation-thread-chat-intro">
        <img
          className="conversation-thread-chat-intro-portrait"
          src={speakerPortraitSrc}
          alt={speakerPortraitAlt}
        />
        <div className="conversation-thread-chat-intro-name">{speakerPortraitAlt}</div>
      </div>

      <div
        className="conversation-thread-log"
        ref={logRef}
        onClick={skipTyping}
      >
        {renderedMessages.map((message) => {
          const isNpc = message.role === "npc";
          const isCurrentTyping = isNpc && message.visibleChars < message.text.length;

          return (
            <div
              key={message.id}
              className={`conversation-thread-row ${isNpc ? "npc" : "player"}`}
            >
              {isNpc && (
                <img
                  className="conversation-thread-avatar"
                  src={speakerPortraitSrc}
                  alt={speakerPortraitAlt}
                />
              )}

              <div className={`conversation-thread-bubble ${isNpc ? "npc" : "player"}`}>
                {message.renderedText}
                {isCurrentTyping && <span className="conversation-thread-cursor">{"\u258C"}</span>}
              </div>
            </div>
          );
        })}
      </div>

      <div className="conversation-thread-controls">
        {isTyping ? (
          <button
            type="button"
            className="conversation-thread-skip-btn"
            onClick={skipTyping}
          >
            Skip Typing
          </button>
        ) : (
          <div className="conversation-thread-options">
            {(currentNode?.responses ?? []).map((response, i) => (
              <button
                key={`${nodeId}-${i}`}
                type="button"
                className="conversation-thread-option-btn"
                onClick={() => handleResponse(response)}
                disabled={response.disabled}
              >
                {response.label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
