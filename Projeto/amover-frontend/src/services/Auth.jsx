// AuthContext.js
import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loginDirectus, getUserInfo, logoutDirectus } from './directus';   
import Cookies from 'js-cookie';

export const AuthContext = createContext();
export const useAuth = () => useContext(AuthContext);      // <‑‑ helper

export const AuthProvider = ({ children }) => {
  const navigate = useNavigate();
  const [user, setUser]   = useState(null);
  const [token, setToken] = useState(Cookies.get('token') || '');
  // ➊ nova função de login
  const login = async (email, password) => {
    try {
      const newToken = await loginDirectus(email, password);
      Cookies.set('token', newToken, {
        expires: 7,  
        secure: true,  
        sameSite: 'Strict',
      });
      setToken(newToken);          // dispara o useEffect que obtém o utilizador
      return true;
    } catch (err) {
      console.error('Falha no login:', err);
      return false;
    }
  };

  // ➋ logout simples (opcional mas útil)
  const logout = () => {
    logoutDirectus(token);
    Cookies.remove('token');
    setToken('');
    setUser(null);
    navigate('/', { replace: true });
  };

  // Já existente – obtém info do utilizador sempre que o token muda
  useEffect(() => {
    async function fetchUser() {
      if (!token) return;
      try {
        const u = await getUserInfo(token);
        setUser(u);
      } catch (err) {
        logout();                 // token expirado → faz logout
      }
    }
    fetchUser();
  }, [token]);

  return (
    <AuthContext.Provider value={{ user, token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
