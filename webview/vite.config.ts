import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: '/fragments/webview/',
  build: {
    outDir: '../server/fragments/webview',
    emptyOutDir: true,
  },
})
