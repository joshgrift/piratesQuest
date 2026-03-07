# PiratesQuest Admin Panel

This React + TypeScript app replaces the old `manage.sh` flow with a browser UI.

## Run locally

```bash
cd admin
npm install
npm run dev
```

Default API URL is the current page origin. You can switch it in the UI.

## Build

```bash
cd admin
npm run build
```

Build output goes to `../api/wwwroot/admin`, so the API can serve it at `/admin/`.

## Coverage vs manage.sh

- login
- users
- servers
- add-server
- rename-server
- set-server-description
- rm-server
- set-role
- status
- set-version
