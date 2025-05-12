// src/App.jsx
import { Routes, Route } from 'react-router-dom';
import { Box, Toolbar } from '@mui/material';

import Navbar from './components/Navbar';
import PublicationsFeed from './components/PublicationsFeed';
import DraftsFeed from './components/DraftsFeed';
import CreatePublication from './components/CreatePublication';
import PublicationDetail from './pages/PublicationDetail';

import Home from './pages/Home';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Equipas from './pages/Equipas';
import EquipasDetails from './pages/EquipasDetails';
import Admin from './pages/Admin';

export default function App() {
  return (
    <>
      <Navbar />
      <Toolbar />
      <Box component="main" sx={{ flexGrow: 1 }}>
        <Routes>
          {/* Páginas públicas */}
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />

          {/* Dashboard */}
          <Route path="/dashboard" element={<Dashboard />} />

          {/* Equipas */}
          <Route path="/equipas" element={<Equipas />} />
          <Route path="/equipas/:id" element={<EquipasDetails />} />

          {/* Admin */}
          <Route path="/admin" element={<Admin />} />

          {/* Publicações */}
          <Route path="/publicacoes" element={<PublicationsFeed />} />
          <Route path="/rascunhos" element={<DraftsFeed />} />
          <Route path="/publicacoes/novo" element={<CreatePublication />} />
          <Route path="/publicacoes/:id" element={<PublicationDetail />} />
        </Routes>
      </Box>
    </>
  );
}
