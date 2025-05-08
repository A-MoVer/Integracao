// src/components/Navbar.jsx
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import Button from '@mui/material/Button';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();       // ← import e hook para navegação
  const location = useLocation();       // sabemos em que rota estamos

  const handleLogout = () => {
    logout();                           // limpa sessão/token
    navigate('/');                      // redireciona para a home pública
  };

  return (
    <nav className="navbar navbar-expand-lg bg-white shadow-sm fixed-top">
      <div className="container-fluid">
        {/* Marca da plataforma */}
        <Link
          to={user ? "/dashboard" : "/"}
          className="navbar-brand fw-bold text-success fs-4 text-decoration-none"
        >
          Plataforma A-MoVeR
        </Link>

        {/* Botões da navbar */}
        <div className="ms-auto d-flex align-items-center">
          {user ? (
            <>
              <span className="me-3">Bem-vinda, {user.email}</span>
              <Button variant="outlined" color="success" onClick={handleLogout}>
                Logout
              </Button>

              {/* Botão Administração aparece apenas se for presidente */}
              {user.role === 'presidente' && (
                <Link to="/admin" className="ms-3 text-decoration-none">
                  <Button variant="outlined" color="success">
                    Administração
                  </Button>
                </Link>
              )}
            </>
          ) : (
            // Se não estiver no /login, mostra Login
            location.pathname !== '/login' && (
              <Link to="/login" style={{ textDecoration: 'none' }}>
                <Button variant="contained" color="success">
                  Login
                </Button>
              </Link>
            )
          )}
        </div>
      </div>
    </nav>
  );
}
