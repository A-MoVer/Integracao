import fs from 'node:fs';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ command }) => {
  const cfg = { plugins: [react()] };

  if (command === 'serve') {
    const keyPath = './certs/localhost-key.pem';
    const crtPath = './certs/localhost.pem';

    if (fs.existsSync(keyPath) && fs.existsSync(crtPath)) {
      cfg.server = {
        https: {
          key:  fs.readFileSync(keyPath),
          cert: fs.readFileSync(crtPath),
        },
        port: 5173,
        proxy: { /* … */ },
      };
    } else {
      console.warn('⚠️  Certs not found; starting dev server over HTTP.');
    }
  }

  return cfg;
});
