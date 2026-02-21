# Pirate's Quest API

REST API and static content server for Pirate's Quest. Manages player authentication, game server listings, persistent game state, and serves SPA fragments that can be loaded inside the game client.

## Running

```bash
docker compose up -d   # start PostgreSQL
dotnet run             # start API (auto-migrates the database)
```

The API starts at `http://localhost:5236` by default.

## Configuration

Set these values in `appsettings.json` (or `appsettings.Development.json` for local dev):

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Jwt:Key` | HMAC-SHA256 signing key for JWTs (min 32 bytes) |
| `ServerApiKey` | Shared secret that game servers use to call state endpoints |

## Auth

There are two auth mechanisms:

### Player auth (JWT)

- `POST /api/signup` creates a new account and returns a **permanent** JWT (no expiry).
- `POST /api/login` validates an existing account and returns a **permanent** JWT (no expiry).
- The client sends this token as `Authorization: Bearer <token>` on protected routes.

### Server auth (shared secret)

Game servers authenticate by sending the shared secret in the `X-Server-Key` header. This is used for the state read/write endpoints so only trusted game servers can access player data.

## Routes

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/signup` | Public | Register new account, returns JWT |
| `POST` | `/api/login` | Public | Login existing account, returns JWT |
| `GET` | `/api/servers` | JWT | List active game servers |
| `GET` | `/api/server/{id}/state/{user}` | Server key | Get a player's saved game state |
| `PUT` | `/api/server/{id}/state/{user}` | Server key | Save a player's game state (opaque JSON) |
| `GET` | `/fragments/{spaId}` | Public | Serve a SPA from `fragments/{spaId}/` |
| `GET` | `/` | Public | Landing page |

## Fragments

Drop a pre-built SPA into `fragments/{name}/` with an `index.html` entry point. It will be served at `/fragments/{name}`. Static assets (JS, CSS) in the same directory are served automatically.
