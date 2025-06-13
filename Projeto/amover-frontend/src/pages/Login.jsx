import { useState } from 'react'
import { useAuth } from '../services/Auth'
import { useNavigate } from 'react-router-dom'

import {
  Box,
  Button,
  TextField,
  Typography,
  Paper
} from '@mui/material'

export default function Login() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e) => {
    e.preventDefault()
    console.log('[Login] Submitting →', { email, senha })

    const ok = await login(email, senha)
    console.log('[Login] Resultado de login →', ok)

    if (ok) navigate('/dashboard')
    else setErro('Credenciais inválidas')
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        background: 'linear-gradient(to bottom, #cbeae7, #d9f2da)',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        px: 2,
        pt: '80px' // Compensar navbar fixa
      }}
    >
      <Paper
        elevation={3}
        sx={{
          p: 4,
          width: '100%',
          maxWidth: 400,
          textAlign: 'center',
          borderRadius: 3,
        }}
      >
        <Typography variant="h5" gutterBottom>
          Iniciar Sessão
        </Typography>

        <form onSubmit={handleSubmit}>
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
            <Typography color="error" sx={{ mt: 1 }}>
              {erro}
            </Typography>
          )}

          <Button
            variant="contained"
            color="success"
            type="submit"
            fullWidth
            sx={{ mt: 3 }}
          >
            Entrar
          </Button>
        </form>
      </Paper>
    </Box>
  )
}
