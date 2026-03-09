/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  // build-game.sh passes /fragments/webview/ so the API can host this UI
  // under a stable fragment URL.
  base: '/',
  build: {
    outDir: '../api/fragments/webview',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    css: true,
  },
})
