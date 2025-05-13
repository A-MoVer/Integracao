import { Link } from 'react-router-dom';
import Button from '@mui/material/Button';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../services/Auth';

function Home() {
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (user) navigate('/dashboard', { replace: true });
  }, [user, navigate]);

  if (user) return null;
  return (
    <div
      style={{
        height: '100vh',
        width: '100vw',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#f2f2f2', // fundo leve
      }}
    >
      <div
        style={{
          textAlign: 'center',
          backgroundColor: 'white',
          padding: '3rem',
          borderRadius: '10px',
          boxShadow: '0 0 10px rgba(0,0,0,0.1)',
          maxWidth: '700px',
          width: '90%',
        }}
      >
        <h1 style={{ fontSize: '3rem', color: '#2e7d32' }}>
          Plataforma A-MoVeR
        </h1>

        <p className="lead mb-5">
          A plataforma interna do projeto A-MoVeR permite a colaboração entre
          bolseiros, técnicos, orientadores e administradores.
        </p>

        <Link to="/equipas" style={{ textDecoration: 'none' }}>
          <Button variant="contained" color="success" size="large">
            Ver Equipas
          </Button>
        </Link>
      </div>
    </div>
  );
}

export default Home;
