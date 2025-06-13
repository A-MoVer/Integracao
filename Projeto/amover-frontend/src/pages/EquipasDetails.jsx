// src/pages/EquipasDetails.jsx
import { useParams, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { useTeams } from '../context/TeamsContext'
import { getMembrosPorEquipa } from '../services/directus'
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Modal,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Snackbar,
  Alert,
  Chip,
  Avatar,
  Divider
} from '@mui/material'

const ordemNivel = {
  "Doutoramento": 1,
  "Mestrado": 2,
  "Licenciatura": 3,
  "Não definido": 4
}

const descricoesMock = {
  "Serviços de Baixo-Nível": "Desenvolve os sistemas eletrónicos e mecânicos essenciais da mota, como EFI, ABS, TCS e sensores. Inclui inovação com capacetes de condução óssea para comunicação eficiente.",
  "Front-End": "Cria as interfaces do display da mota e da app myFULGORA, com base em revisões sistemáticas, grupos focais e definição de personas e cenários.",
  "Plataforma de Gestão de Ciclo de Vida": "Desenvolve uma plataforma web e mobile para gerir todo o ciclo de vida do motociclo, focando-se em manutenção, sustentabilidade e comunicação entre utilizadores e fabricantes.",
  "Segurança e Apoio à Operação da Mota": "Foca-se em IA e simulações com sensores para criar sistemas de alerta de colisão, deteção de peões e assistência à condução usando o simulador CARLA.",
  "Gestão de Sistemas e Integração": "Responsável por gerir e integrar os servidores e serviços necessários ao ecossistema, com Git, armazenamento, serviços web e containers Docker.",
  "Serviços de Logística": "Explora modelos de previsão de consumo e otimização de rotas. Desenvolve uma plataforma de mobilidade e apoia projetos académicos em engenharia de software.",
  "Serviços Remotos e Cibersegurança": "Trabalha em funcionalidades remotas como monitorização, diagnóstico, rastreamento GPS e controlo via app, com foco em proteção de dados e segurança contra ataques."
}

export default function EquipasDetails() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { equipas } = useTeams()
  const [equipa, setEquipa] = useState(null)
  const [membros, setMembros] = useState([])

  const [editOpen, setEditOpen] = useState(false)
  const [membroEdicao, setMembroEdicao] = useState(null)
  const [snackbar, setSnackbar] = useState({ open: false, message: '' })

  useEffect(() => {
    const encontrada = equipas.find(e => e.id === id)
    setEquipa(encontrada || null)
  }, [equipas, id])

  useEffect(() => {
    if (equipa?.id) {
      getMembrosPorEquipa(equipa.id)
        .then(data => setMembros(data))
        .catch(error => console.error("💥 Erro ao buscar membros:", error))
    }
  }, [equipa])

  const handleOpenEdit = (member, idx) => {
    setMembroEdicao({ ...member, idx })
    setEditOpen(true)
  }

  const handleCloseEdit = () => setEditOpen(false)

  const handleSaveMember = () => {
    const novosMembros = membros.map((m, i) =>
      i === membroEdicao.idx
        ? {
            ...m,
            nome: membroEdicao.nome,
            email: membroEdicao.email,
            cargo: membroEdicao.cargo,
            foto: membroEdicao.foto,
            status: membroEdicao.status
          }
        : m
    )
    setMembros(novosMembros)
    setSnackbar({ open: true, message: 'Membro atualizado com sucesso!' })
    handleCloseEdit()
  }

if (!equipa) {
  return (
    <Box sx={{ pt: 12, textAlign: 'center' }}>
      <Typography color="error" variant="h5" gutterBottom>
        Equipa não encontrada!
      </Typography>

      <Button
        variant="outlined"
        color="success"
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/equipas')}
        sx={{ mt: 2 }}
      >
        VOLTAR
      </Button>
    </Box>
  );
}

  return (
    <Box sx={{ pt: 4, px: 2 }}>

      <Typography variant="h3" color="success.main" gutterBottom>
        {equipa.Nome || equipa.name}
      </Typography>

      <Typography variant="h6" color="text.secondary" gutterBottom>
        {equipa.Descricao || descricoesMock[equipa.Nome] || equipa.area || 'Sem descrição'}
        {/* Se o campo "Descricao" for preenchido no Directus, será usado automaticamente */}
      </Typography>

      <Typography variant="subtitle1" gutterBottom>
        Status: {equipa.Status === 'ativo' ? 'Ativo' : 'Desativado'}
      </Typography>

      <Typography variant="h5" color="success.main" gutterBottom>
        Membros
      </Typography>

      <Grid container spacing={4}>
        {Array.from(new Map(membros.map(m => [m.email, m])).values())
          .slice()
          .sort((a, b) => (ordemNivel[a.nivel] || 99) - (ordemNivel[b.nivel] || 99))
          .map((m, idx) => (
            <Grid item xs={12} sm={6} md={4} key={idx}>
              <Card
                onClick={() => handleOpenEdit(m, idx)}
                sx={{
                  cursor: 'pointer',
                  backgroundColor: m.status === 'ativo' ? '#81c784' : '#f5f5f5',
                  p: 3,
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  height: '100%',
                  transition: 'box-shadow 0.3s',
                  '&:hover': { boxShadow: 6 }
                }}
              >
                <Box sx={{ flex: 1 }}>
                  <Typography variant="h6" color="success.main" gutterBottom>
                    {m.nome}
                  </Typography>
                  <Divider sx={{ mb: 1 }} />
                  <Typography variant="body2"><strong>Cargo:</strong> {m.cargo}</Typography>
                  <Typography variant="body2"><strong>Grau Académico:</strong> {m.nivel}</Typography>
                  {m.inicio && (
                    <Typography variant="body2">
                      <strong>Início:</strong> {new Date(m.inicio).toLocaleDateString()}
                    </Typography>
                  )}
                  {m.fim && (
                    <Typography variant="body2">
                      <strong>Fim:</strong> {new Date(m.fim).toLocaleDateString()}
                    </Typography>
                  )}
                  <Typography variant="body2">
                    <strong>Email:</strong> <a href={`mailto:${m.email}`} style={{ textDecoration: 'none', color: 'inherit' }}>{m.email}</a>
                  </Typography>
                  <Box sx={{ mt: 1 }}>
                    <Chip
                      label={m.status === 'ativo' ? 'Ativo' : 'Inativo'}
                      color={m.status === 'ativo' ? 'success' : 'default'}
                      size="small"
                    />
                  </Box>
                </Box>
                <Avatar src={m.foto} alt={m.nome} sx={{ width: 64, height: 64, ml: 2 }} />
              </Card>
            </Grid>
          ))}
      </Grid>

      <Modal open={editOpen} onClose={handleCloseEdit}>
        <Box
          component="form"
          onSubmit={e => {
            e.preventDefault()
            handleSaveMember()
          }}
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            width: 360,
            bgcolor: 'background.paper',
            p: 4,
            borderRadius: 2,
            boxShadow: 24
          }}
        >
          <Typography variant="h6" gutterBottom>Editar Membro</Typography>
          <TextField label="Nome" fullWidth margin="normal" value={membroEdicao?.nome || ''} onChange={e => setMembroEdicao(prev => ({ ...prev, nome: e.target.value }))} />
          <TextField label="Email" fullWidth margin="normal" value={membroEdicao?.email || ''} onChange={e => setMembroEdicao(prev => ({ ...prev, email: e.target.value }))} />
          <TextField label="Cargo" fullWidth margin="normal" value={membroEdicao?.cargo || ''} onChange={e => setMembroEdicao(prev => ({ ...prev, cargo: e.target.value }))} />
          <TextField label="Foto (URL)" fullWidth margin="normal" value={membroEdicao?.foto || ''} onChange={e => setMembroEdicao(prev => ({ ...prev, foto: e.target.value }))} />
          <FormControl fullWidth margin="normal">
            <InputLabel>Status</InputLabel>
            <Select label="Status" value={membroEdicao?.status || 'ativo'} onChange={e => setMembroEdicao(prev => ({ ...prev, status: e.target.value }))}>
              <MenuItem value="ativo">Ativo</MenuItem>
              <MenuItem value="inativo">Inativo</MenuItem>
            </Select>
          </FormControl>
          <Box sx={{ textAlign: 'right', mt: 2 }}>
            <Button onClick={handleCloseEdit} sx={{ mr: 1 }}>Cancelar</Button>
            <Button type="submit" variant="contained" color="success">Guardar</Button>
          </Box>
        </Box>
      </Modal>

      <Snackbar open={snackbar.open} autoHideDuration={3000} onClose={() => setSnackbar(s => ({ ...s, open: false }))}>
        <Alert severity="success" onClose={() => setSnackbar(s => ({ ...s, open: false }))}>{snackbar.message}</Alert>
      </Snackbar>
    </Box>
  )
}
