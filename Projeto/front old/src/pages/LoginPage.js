// src/pages/LoginPage.js
import React, { useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { loginDirectus } from '../services/directus'; // Função de serviço para login
import { AuthContext } from '../services/AuthContext';
import Header from '../components/Header';
import Footer from '../components/Footer';
import './LoginPage.css';

function LoginPage() {
  const navigate = useNavigate();
  const { setToken } = useContext(AuthContext); // Obtemos setToken do contexto
  const [showTooltip, setShowTooltip] = useState(false); 
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleLogin = async (e) => {
    e.preventDefault();
    try {
      const token = await loginDirectus(email, password);
      console.log('Token recebido:', token); 
      localStorage.setItem('token', token);
      setToken(token); // Atualiza o token no contexto para disparar o fetchUser
      navigate('/dashboard');
    } catch (err) {
      setError(err.message);
      console.error('Erro:', err);
    }
  };

  return (
    <div className="login-container">
      {/* Cabeçalho */}
      <Header showTooltip={showTooltip} setShowTooltip={setShowTooltip} />
      
      {/* Formulário de Login */}
      <form onSubmit={handleLogin} className="login-form">
        <h2>Login</h2>
        {error && <div className="error-container">{error}</div>}

        <input 
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <input 
          type="password"
          placeholder="Senha"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        <button type="submit">Entrar</button>
      </form>

      {/* Rodapé */}
      <Footer />
    </div>
  );
}

export default LoginPage;
