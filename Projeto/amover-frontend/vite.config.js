// amover-frontend/vite.config.js
import { defineConfig } from 'vite';
import react          from '@vitejs/plugin-react';
import fs             from 'node:fs';

export default defineConfig(({ command }) => {
  const cfg = {
    plugins: [react()],
  };

  // só carrega HTTPS e proxy quando rodamos `vite serve`
  if (command === 'serve') {
    cfg.server = {
      https: {
        key:  fs.readFileSync('./localhost-key.pem'),
        cert: fs.readFileSync('./localhost.pem'),
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
