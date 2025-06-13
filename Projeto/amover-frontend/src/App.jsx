import { Routes, Route } from 'react-router-dom';
import { Box, Toolbar } from '@mui/material';

import Navbar from './components/Navbar';

import Home from './pages/Home';
import Login from './pages/Login';
import NewPublication from './pages/NewPublication'; // <- usar a nova versão
import Equipas from './pages/Equipas'

import PrivateLayout from './components/PrivateLayout';

import './App.css';
import './i18n';

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

          {/* Página pública de listagem de equipas */}
          <Route path="/equipas" element={<Equipas />} />

          {/* Página de criação de publicações */}
          <Route path="/publicacoes/novo" element={<NewPublication />} />

          {/* Páginas privadas com layout interno */}
          <Route path="/*" element={<PrivateLayout />} />
        </Routes>
      </Box>
    </>
  );
}
