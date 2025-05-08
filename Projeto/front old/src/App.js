import React from 'react';
import { Routes, Route } from 'react-router-dom';
import { AuthProvider } from './services/AuthContext';
import HomePage from './pages/HomePage';
import TeamsPage from './pages/TeamsPage';
import LoginPage from './pages/LoginPage';
import Dashboard from './pages/Dashboard/DashboardPage';
import EquipasPage from './pages/Dashboard/EquipasPage';
import EquipaDetalhesPage from './pages/Dashboard/EquipaDetalhesPage';

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/equipas" element={<TeamsPage />} />
        <Route path="/Login" element={<LoginPage />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/dashboard/equipas" element={<EquipasPage />} />
        <Route path="/dashboard/equipas/:id" element={<EquipaDetalhesPage />} />


      </Routes>
    </AuthProvider>
  );
}

export default App;
