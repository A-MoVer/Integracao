import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './Header.css'; // Import do CSS específico

function Header({ showTooltip, setShowTooltip }) {
  const navigate = useNavigate();
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      navigate('/dashboard', { replace: true });
    }
  }, [navigate]);
  return (
    <header
      className="header"
      onMouseEnter={() => setShowTooltip(false)}
      onMouseLeave={() => setShowTooltip(true)}
    >
      <div className="header-content">
        <div className="logo-container" onClick={() => navigate('/')}>
          <img src="imagens/logo.jpg" alt="Logo A-MoVeR" className="logo" />
        </div>
        <nav className="nav-buttons">
          <button className="btn login-btn" onClick={() => navigate('/login')}>
            Login
          </button>
        </nav>
      </div>
    </header>
  );
}

export default Header;
