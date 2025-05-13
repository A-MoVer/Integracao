// vite.config.js
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import fs from 'node:fs';

export default defineConfig({
  plugins: [react()],
  server: {
    // certificados que já criaste
    https: {
      key:  fs.readFileSync('../localhost-key.pem'),
      cert: fs.readFileSync('../localhost.pem'),
    },
    port: 5173,
    proxy: {
      // qualquer request que comece por /api → Directus em HTTP
      '/api': {
        target: 'http://localhost:8055',
        changeOrigin: true,
        secure: false,      // diz ao Vite para ignorar HTTPS ausente no target
        rewrite: path => path.replace(/^\/api/, ''),
      },
    },
  },
});
