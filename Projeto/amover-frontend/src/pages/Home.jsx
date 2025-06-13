import { useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../services/Auth'
import { Box, Button, Typography, Paper } from '@mui/material'

export default function Home() {
  const { user } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    if (user) navigate('/dashboard', { replace: true })
  }, [user, navigate])

  if (user) return null

  return (
    <Box
      sx={{
        minHeight: '100vh',
        background: 'linear-gradient(to bottom, #d6edea, #e6f5e8)',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        px: 2,
        pt: '80px',
      }}
    >
      <Paper
        elevation={3}
        sx={{
          p: 5,
          maxWidth: 900,
          width: '100%',
          textAlign: 'center',
          borderRadius: 3,
        }}
      >
        <Typography variant="h3" color="success.main" gutterBottom>
          Plataforma Interna A-MoVeR
        </Typography>

        <Typography variant="body1" sx={{ fontSize: '1.15rem', mb: 3 }}>
          A plataforma A-MoVeR foi criada para facilitar a colaboração entre bolseiros, técnicos,
          orientadores e administradores envolvidos no desenvolvimento da mota elétrica
          do projeto A-MoVeR — Agenda Mobilizadora para o Desenvolvimento de Produtos e Sistemas
          Inteligentes de Mobilidade Verde.
        </Typography>

        <Typography variant="body1" sx={{ fontSize: '1.15rem', mb: 3 }}>
          Aqui podes consultar e gerir as equipas técnicas e científicas, partilhar publicações,
          acompanhar o progresso do projeto e manter a comunicação entre os vários intervenientes.
        </Typography>

        <Typography variant="body1" sx={{ fontSize: '1.15rem', mb: 4 }}>
          O objetivo é garantir uma gestão eficaz e colaborativa de todo o ecossistema do projeto,
          desde a conceção até à implementação da mota elétrica.
        </Typography>

        <Link to="/equipas" style={{ textDecoration: 'none' }}>
          <Button variant="contained" color="success" size="large">
            Ver Equipas
          </Button>
        </Link>
      </Paper>
    </Box>
  )
}
