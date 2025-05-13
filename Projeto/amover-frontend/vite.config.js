// .../amover-frontend/vite.config.js
import { defineConfig } from 'vite';
import react          from '@vitejs/plugin-react';
import fs             from 'node:fs';
import path           from 'path';

export default defineConfig(({ command }) => {
  const cfg = {
    plugins: [react()],
  };

  if (command === 'serve') {
    // só em `npm run dev` (vite serve) ele carrega os PEMs
    const certPath = './localhost.pem';
    const keyPath = './localhost-key.pem';
    
    if (fs.existsSync(certPath) && fs.existsSync(keyPath)) {
      cfg.server = {
        https: {
          key: fs.readFileSync(keyPath),
          cert: fs.readFileSync(certPath),
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
    } else {
      cfg.server = {
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
  }

  return cfg;
});
