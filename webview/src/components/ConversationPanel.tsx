import { useEffect, useState } from "react";

export interface ConversationResponse {
  label: string;
  next?: string;
  /** Optional action ID for parent-controlled side effects (hire/fire/etc). */
  action?: string;
  /** Optional disabled state for response buttons. */
  disabled?: boolean;
}

export interface ConversationNode {
  text: string;
  responses: ConversationResponse[];
}

export type ConversationTree = Record<string, ConversationNode>;

interface ConversationPanelProps {
  tree: ConversationTree;
  speakerName: string;
  speakerPortraitSrc: string;
  speakerPortraitAlt: string;
  classNamePrefix: string;
  /** Node shown first when this panel mounts or the topic changes. */
  initialNodeId?: string;
  /** Nodes that should render full text immediately (no typewriter). */
  instantNodeIds?: string[];
  /** Typewriter speed in milliseconds per character. */
  typingSpeedMs?: number;
  /** Optional callback to run side effects and optionally redirect node flow. */
  onAction?: (actionId: string) => string | void;
}

function classFor(prefix: string, suffix: string): string {
  return `${prefix}-${suffix}`;
}

export function ConversationPanel({
  tree,
  speakerName,
  speakerPortraitSrc,
  speakerPortraitAlt,
  classNamePrefix,
  initialNodeId = "root",
  instantNodeIds = [],
  typingSpeedMs = 25,
  onAction,
}: ConversationPanelProps) {
  const [nodeId, setNodeId] = useState(initialNodeId);
  const [charIndex, setCharIndex] = useState(0);
  const [animKey, setAnimKey] = useState(0);

  const currentNode = tree[nodeId] ?? tree[initialNodeId];

  const shouldRenderInstant = (id: string) => instantNodeIds.includes(id);

  // Reset flow when the conversation source changes (new NPC/topic).
  useEffect(() => {
    const startNode = tree[initialNodeId];
    const startLen = startNode?.text.length ?? 0;
    setNodeId(initialNodeId);
    setCharIndex(shouldRenderInstant(initialNodeId) ? startLen : 0);
    setAnimKey((k) => k + 1);
  }, [tree, initialNodeId]);

  const isTyping = !!currentNode && charIndex < currentNode.text.length;

  useEffect(() => {
    if (!currentNode || !isTyping) return;
    const id = setTimeout(() => setCharIndex((c) => c + 1), typingSpeedMs);
    return () => clearTimeout(id);
  }, [currentNode, isTyping, charIndex, typingSpeedMs]);

  if (!currentNode) {
    return (
      <div className={classFor(classNamePrefix, "container")}>
        <div className="empty-state">Conversation unavailable.</div>
      </div>
    );
  }

  const skip = () => {
    if (isTyping) setCharIndex(currentNode.text.length);
  };

  const navigate = (nextId: string) => {
    const nextNode = tree[nextId];
    if (!nextNode) return;

    setNodeId(nextId);
    setCharIndex(shouldRenderInstant(nextId) ? nextNode.text.length : 0);
    setAnimKey((k) => k + 1);
  };

  const handleResponseClick = (response: ConversationResponse) => {
    if (response.disabled) return;

    let nextId = response.next;

    if (response.action && onAction) {
      const redirect = onAction(response.action);
      if (redirect) nextId = redirect;
    }

    if (nextId) navigate(nextId);
  };

  return (
    <div className={classFor(classNamePrefix, "container")}>
      <div className={classFor(classNamePrefix, "portrait-wrap")}>
        <img
          className={classFor(classNamePrefix, "portrait")}
          src={speakerPortraitSrc}
          alt={speakerPortraitAlt}
        />
        <div className={classFor(classNamePrefix, "name")}>{speakerName}</div>
      </div>

      <div className={classFor(classNamePrefix, "speech")} onClick={skip}>
        <div className={classFor(classNamePrefix, "text")}>
          {currentNode.text.slice(0, charIndex)}
          {isTyping && (
            <span className={classFor(classNamePrefix, "cursor")}>{"\u258C"}</span>
          )}
        </div>
        {isTyping && (
          <div className={classFor(classNamePrefix, "skip-hint")}>click to skip</div>
        )}
      </div>

      {!isTyping && (
        <div className={classFor(classNamePrefix, "responses")}>
          {currentNode.responses.map((response, i) => (
            <button
              key={`${animKey}-${nodeId}-${i}`}
              className={classFor(classNamePrefix, "response-btn")}
              style={{ animationDelay: `${i * 0.08}s` }}
              onClick={() => handleResponseClick(response)}
              disabled={response.disabled}
            >
              <span className={classFor(classNamePrefix, "response-num")}>{i + 1}</span>
              {response.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
