// src/services/Auth.jsx
import { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loginDirectus, getUserInfo, logoutDirectus } from './directus';
import Cookies from 'js-cookie';

const AuthContext = createContext();
export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(Cookies.get('token') || '');

  const login = async (email, password) => {
    try {
      const newToken = await loginDirectus(email, password);
      Cookies.set('token', newToken, { expires: 7, secure: true, sameSite: 'Strict' });
      setToken(newToken);
      return true;
    } catch (err) {
      console.error('❌ Falha no login:', err);
      return false;
    }
  };

  const logout = () => {
    logoutDirectus(token);
    Cookies.remove('token');
    setToken('');
    setUser(null);
    navigate('/', { replace: true });
  };

  useEffect(() => {
    const fetchUser = async () => {
      if (!token) return;
      try {
        const u = await getUserInfo(token);
        console.log('🧠 Utilizador autenticado:', u);
        setUser(u);
      } catch (err) {
        console.warn('⚠️ Token inválido ou erro ao obter utilizador:', err);
        logout();
      }
    };
    fetchUser();
  }, [token]);

  return (
    <AuthContext.Provider value={{ user, token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
