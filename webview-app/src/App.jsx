import { useState, useEffect } from 'react'
import './App.css'

const SHOP_ITEMS = [
  { type: 'CannonBall', label: 'Cannon Ball', price: 2 },
  { type: 'Wood', label: 'Wood', price: 4 },
  { type: 'Fish', label: 'Fish', price: 5 },
]

function App() {
  const [inventory, setInventory] = useState(null)
  const [status, setStatus] = useState(null)

  useEffect(() => {
    // Called by C# via eval("window.updateInventory({...})")
    // whenever the player's inventory changes.
    window.updateInventory = (data) => {
      setInventory(data)
    }

    fetch('/api/status')
      .then(res => res.json())
      .then(data => setStatus(data))
      .catch(() => {})
  }, [])

  // Send a purchase request to C# via godot_wry's IPC channel.
  // C# receives this as the ipc_message signal on the WebView node.
  const handlePurchase = (itemType, quantity) => {
    if (!window.ipc) return
    window.ipc.postMessage(JSON.stringify({
      action: 'purchase',
      itemType,
      quantity,
    }))
  }

  return (
    <div className="panel">
      <h1>Pirate's Quest</h1>

      {status && (
        <div className="card version-bar">
          <span className="version">v{status.version}</span>
          {inventory && <span className="coins">{inventory.Coin ?? 0} coin</span>}
        </div>
      )}

      <div className="card">
        <h2>Inventory</h2>
        {!inventory ? (
          <p className="loading">Waiting for game data...</p>
        ) : (
          <ul className="inventory-list">
            {Object.entries(inventory)
              .filter(([, count]) => count > 0)
              .map(([item, count]) => (
                <li key={item}>
                  <span className="item-name">{item}</span>
                  <span className="item-count">{count}</span>
                </li>
              ))}
            {Object.values(inventory).every(c => c === 0) && (
              <li className="empty">No items yet</li>
            )}
          </ul>
        )}
      </div>

      <div className="card">
        <h2>Quick Buy</h2>
        <div className="shop-list">
          {SHOP_ITEMS.map(item => (
            <button
              key={item.type}
              className="buy-btn"
              onClick={() => handlePurchase(item.type, 1)}
            >
              Buy {item.label}
              <span className="price">{item.price} coin</span>
            </button>
          ))}
        </div>
      </div>

      <div className="card">
        <h2>Controls</h2>
        <ul>
          <li><kbd>W A S D</kbd> Move</li>
          <li><kbd>Q</kbd> / <kbd>E</kbd> Fire cannons</li>
          <li><kbd>Space</kbd> Fire broadside</li>
          <li><kbd>X</kbd> Toggle this panel</li>
        </ul>
      </div>
    </div>
  )
}

export default App
