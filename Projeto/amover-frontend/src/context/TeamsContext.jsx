// src/context/TeamsContext.jsx
import React, { createContext, useContext, useEffect, useState } from 'react';
import { getEquipasComMembros } from '../services/directus';
import Cookies from 'js-cookie';

const TeamsContext = createContext();

export function TeamsProvider({ children }) {
  const [equipas, setEquipas] = useState([]);

  useEffect(() => {
  const token = Cookies.get('token');
  if (!token) return;

  getEquipasComMembros(token)
    .then(data => {
      console.log('🔍 EQUIPAS RECEBIDAS DO DIRECTUS:', data); // ← AQUI
      setEquipas(data);
    })
    .catch(err => {
      console.error('Erro ao carregar equipas:', err.message);
      Cookies.remove('token');
      window.location.reload();
    });
}, []);


  return (
    <TeamsContext.Provider value={{ equipas, setEquipas }}>
      {children}
    </TeamsContext.Provider>
  );
}

export function useTeams() {
  return useContext(TeamsContext);
}
