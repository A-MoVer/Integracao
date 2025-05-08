import React, { useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from '../../services/AuthContext';
import './HeaderLoggedIn.css';

function HeaderLoggedIn({ showTooltip, setShowTooltip }) {
  const navigate = useNavigate();
  const { user, setUser } = useContext(AuthContext);

  const handleLogout = () => {
    localStorage.removeItem('token');
    setUser(null);
    navigate('/login');
  };

  
  const roleName = user?.role?.name || user?.role;

  return (
    <header 
      className="header-logged-in"
      onMouseEnter={() => setShowTooltip(false)}
      onMouseLeave={() => setShowTooltip(true)}
    >
      <div className="header-content">
        <div className="logo-container" onClick={() => navigate('/dashboard')}>
          <img src="/imagens/logo.jpg" alt="Logo A-MoVeR" className="logo" />
        </div>
        <div className="user-info">
          <span className="user-name">
            Olá, {user?.first_name} {user?.last_name}
          </span>
          {roleName && (
            <span className="user-role">
              Role: {roleName}
            </span>
          )}
          <button onClick={handleLogout} className="btn logout-btn">
            Sair
          </button>
        </div>
      </div>
    </header>
  );
}

export default HeaderLoggedIn;