import { useState, useEffect } from "react";
import { BASE } from "../utils/helpers";
import { GUIDE_DIALOGUE } from "./guideDialogue";

export function GuideTab() {
  const [nodeId, setNodeId] = useState("root");
  const rootNode = GUIDE_DIALOGUE["root"]!;
  const [charIndex, setCharIndex] = useState(rootNode.text.length);
  const [animKey, setAnimKey] = useState(0);

  const node = GUIDE_DIALOGUE[nodeId];
  const isTyping = charIndex < node!.text.length;

  useEffect(() => {
    if (!isTyping) return;
    const id = setTimeout(() => setCharIndex((c) => c + 1), 25);
    return () => clearTimeout(id);
  }, [charIndex, isTyping]);

  const skip = () => {
    if (isTyping) setCharIndex(node!.text.length);
  };

  const navigate = (next: string) => {
    setNodeId(next);
    setCharIndex(next === "root" ? rootNode.text.length : 0);
    setAnimKey((k) => k + 1);
  };

  return (
    <div className="guide-container">
      <div className="guide-portrait-wrap">
        <img
          className="guide-portrait"
          src={`${BASE}images/characters/character2.png`}
          alt="Scarlett"
        />
        <div className="guide-name">Scarlett</div>
      </div>

      <div className="guide-speech" onClick={skip}>
        <div className="guide-text">
          {node!.text.slice(0, charIndex)}
          {isTyping && <span className="guide-cursor">{"\u258C"}</span>}
        </div>
        {isTyping && <div className="guide-skip-hint">click to skip</div>}
      </div>

      {!isTyping && (
        <div className="guide-responses">
          {node!.responses.map((r, i) => (
            <button
              key={`${animKey}-${i}`}
              className="guide-response-btn"
              style={{ animationDelay: `${i * 0.08}s` }}
              onClick={() => navigate(r.next)}
            >
              <span className="guide-response-num">{i + 1}</span>
              {r.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
