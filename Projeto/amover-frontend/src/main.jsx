import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'
import { AuthProvider } from './context/AuthContext'
import { TeamsProvider } from './context/TeamsContext'
import { PublicationsProvider } from './context/PublicationsContext'  // ← importa o teu PublicationsProvider

import 'bootstrap/dist/css/bootstrap.min.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <AuthProvider>
      <TeamsProvider>         
        <PublicationsProvider>
          <App />
        </PublicationsProvider>
      </TeamsProvider>
    </AuthProvider>
  </React.StrictMode>
)