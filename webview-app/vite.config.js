import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: '/fragments/info-panel/',
  build: {
    outDir: '../fragments/info-panel',
    emptyOutDir: true,
  },
})
