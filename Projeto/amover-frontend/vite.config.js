// .../amover-frontend/vite.config.js
import { defineConfig } from 'vite';
import react          from '@vitejs/plugin-react';
import fs             from 'node:fs';

export default defineConfig(({ command }) => {
  const cfg = {
    plugins: [react()],
  };

  if (command === 'serve') {
    // só em `npm run dev` (vite serve) ele carrega os PEMs
    cfg.server = {
      https: {
        key:  fs.readFileSync('./certs/localhost-key.pem'),
        cert: fs.readFileSync('./certs/localhost.pem'),
      },
      port: 5173,
      proxy: {
        '/api': {
          target: 'http://directus:8055',
          changeOrigin: true,
          secure: false,
          rewrite: path => path.replace(/^\/api/, ''),
        },
      },
    };
  }

  return cfg;
});
