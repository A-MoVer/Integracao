import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../services/Auth';
import Button from '@mui/material/Button';
import logoAmover from '../assets/logo_amover.png';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <nav
      className="navbar navbar-expand-lg shadow-sm fixed-top"
      style={{
        backgroundColor: '#004d40',
        zIndex: 2000 // garante que fica por cima da sidebar
      }}
    >
      <div className="container-fluid d-flex justify-content-between align-items-center">
        {/* Logo clicável */}
        <Link
          to={user ? "/dashboard" : "/"}
          className="navbar-brand d-flex align-items-center text-decoration-none"
        >
          <img
            src={logoAmover}
            alt="Logo A-MoVeR"
            style={{ height: 48 }}
          />
        </Link>

        <div className="d-flex align-items-center ms-auto">
          {user ? null : location.pathname !== '/login' && (
            <Link to="/login" className="text-decoration-none">
              <Button
                variant="outlined"
                style={{ color: 'white', borderColor: 'white' }}
                sx={{
                  '&:hover': {
                    backgroundColor: '#00695c',
                    borderColor: 'white'
                  }
                }}
              >
                Login
              </Button>
            </Link>
          )}
        </div>
      </div>
    </nav>
  );
}
