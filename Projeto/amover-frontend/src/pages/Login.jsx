import { useState } from 'react';
import { useAuth } from '../services/Auth';
import { useNavigate } from 'react-router-dom';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';

function Login() {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro]  = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

const handleSubmit = async (e) => {
  e.preventDefault();
  console.log('[Login] Submitting →', { email, senha });      // ① dispara?

  const ok = await login(email, senha);
  console.log('[Login] Resultado de login →', ok);            // ② chega cá?

  if (ok) navigate('/dashboard');
  else     setErro('Credenciais inválidas');
};

  return (
    <div
      style={{
        height: '100vh',
        width: '100vw',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#f2f2f2',
        paddingTop: '80px', // compensar navbar
      }}
    >
      <form
        onSubmit={handleSubmit}
        style={{
          backgroundColor: 'white',
          padding: '3rem',
          borderRadius: '10px',
          boxShadow: '0 0 10px rgba(0,0,0,0.1)',
          maxWidth: '400px',
          width: '90%',
          textAlign: 'center',
        }}
      >
        <h2 className="mb-4">Iniciar Sessão</h2>

        <TextField
          label="Email"
          type="email"
          variant="outlined"
          fullWidth
          required
          margin="normal"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />

        <TextField
          label="Senha"
          type="password"
          variant="outlined"
          fullWidth
          required
          margin="normal"
          value={senha}
          onChange={(e) => setSenha(e.target.value)}
        />

        {erro && (
          <p style={{ color: 'red', marginTop: '0.5rem' }}>{erro}</p>
        )}

        <Button
          variant="contained"
          color="success"
          type="submit"
          fullWidth
          className="mt-3"
        >
          Entrar
        </Button>
      </form>
    </div>
  );
}

export default Login;
