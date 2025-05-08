// AuthContext.js
import React, { createContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getUserInfo } from './directus';

export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  // Inicializa o token a partir do localStorage
  const [token, setToken] = useState(localStorage.getItem('token') || '');

  useEffect(() => {
    console.log('Token no contexto:', token);
    async function fetchUser() {
      if (token && token.trim() !== '') {
        try {
          const userData = await getUserInfo(token);
          console.log('Dados do usuário recebidos:', userData);
          setUser(userData);
        } catch (err) {
          console.error('Erro ao buscar dados do usuário:', err);
          // Se o token for inválido ou expirar, removemos-o
          localStorage.removeItem('token');
          setToken('');
          setUser(null);
          console.log('Token removido devido a erro:', err);
          navigate('/', { replace: true });
        }
      }
    }
    fetchUser();
  }, [token, navigate]);

  return (
    <AuthContext.Provider value={{ user, setUser, token, setToken }}>
      {children}
    </AuthContext.Provider>
  );
};
