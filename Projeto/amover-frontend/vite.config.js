// vite.config.js
import { defineConfig } from 'vite';
import react          from '@vitejs/plugin-react';
import fs             from 'node:fs';

export default defineConfig(({ command }) => {
  const base = {
    plugins: [react()],
  };

  if (command === 'serve') {
    base.server = {
      https: {
        key:  fs.readFileSync('./localhost-key.pem'),
        cert: fs.readFileSync('./localhost.pem'),
      },
      port: 5173,
      proxy: {
        '/api': {
          target: 'http://localhost:8055',
          changeOrigin: true,
          secure: false,
          rewrite: path => path.replace(/^\/api/, ''),
        },
      },
    };
  }

  return base;
});
