// src/main.jsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import App from './App.jsx';
import './index.css';

import { AuthProvider } from './services/Auth.jsx';          // confirma o caminho
import { TeamsProvider } from './context/TeamsContext';
import { PublicationsProvider } from './context/PublicationsContext';

import 'bootstrap/dist/css/bootstrap.min.css';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>            {/* ← aqui */}
      <AuthProvider>
        <TeamsProvider>
          <PublicationsProvider>
            <App />
          </PublicationsProvider>
        </TeamsProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
