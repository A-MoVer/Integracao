// src/pages/EquipasDetails.jsx
import { useParams, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { useTeams } from '../context/TeamsContext'
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
  Alert
} from '@mui/material'

export default function EquipasDetails() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { equipas, setEquipas } = useTeams()
  const [equipa, setEquipa] = useState(null)

  // states para edição de membro
  const [editOpen, setEditOpen] = useState(false)
  const [membroEdicao, setMembroEdicao] = useState(null)
  const [snackbar, setSnackbar] = useState({ open: false, message: '' })

  // Carrega a equipa pelo id
  useEffect(() => {
    const encontrada = equipas.find(e => e.id === parseInt(id, 10))
    setEquipa(encontrada || null)
  }, [equipas, id])

  if (!equipa) {
    return (
      <Box sx={{ pt: 12, textAlign: 'center' }}>
        <Typography color="error" variant="h5">
          Equipa não encontrada!
        </Typography>
        <Button onClick={() => navigate('/equipas')} sx={{ mt: 2 }}>
          ← Voltar
        </Button>
      </Box>
    )
  }

  const handleOpenEdit = (member, idx) => {
    setMembroEdicao({ ...member, idx })
    setEditOpen(true)
  }
  const handleCloseEdit = () => setEditOpen(false)

  const handleSaveMember = () => {
    const novosMembros = equipa.membros.map((m, i) =>
      i === membroEdicao.idx
        ? {
            nome: membroEdicao.nome,
            email: membroEdicao.email,
            cargo: membroEdicao.cargo,
            foto: membroEdicao.foto,
            status: membroEdicao.status
          }
        : m
    )
    const novaEquipa = { ...equipa, membros: novosMembros }
    setEquipas(equipas.map(e => (e.id === novaEquipa.id ? novaEquipa : e)))
    setSnackbar({ open: true, message: 'Membro atualizado com sucesso!' })
    handleCloseEdit()
  }

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Button onClick={() => navigate('/admin')} sx={{ mb: 2 }}>
        ← Voltar ao Admin
      </Button>
      <Typography variant="h3" color="success.main" gutterBottom>
        {equipa.nome}
      </Typography>
      <Typography variant="h6" color="text.secondary" gutterBottom>
        {equipa.descricao}
      </Typography>
      <Typography variant="subtitle1" gutterBottom>
        Status: {equipa.status === 'ativo' ? 'Ativo' : 'Desativado'}
      </Typography>

      <Typography variant="h5" color="success.main" gutterBottom>
        Membros
      </Typography>
      <Grid container spacing={4}>
        {equipa.membros.map((m, idx) => (
          <Grid item xs={12} sm={6} md={4} key={idx}>
            <Card
              onClick={() => handleOpenEdit(m, idx)}
              sx={{
                cursor: 'pointer',
                backgroundColor: m.status === 'ativo' ? '#81c784' : '#f5f5f5',
                transition: 'background-color 0.3s, box-shadow 0.3s',
                '&:hover': { boxShadow: 6 }
              }}
            >
              <CardContent>
                <Typography variant="h6" color="success.main">
                  {m.nome}
                </Typography>
                <Typography variant="body2">Cargo: {m.cargo}</Typography>
                <Typography variant="body2">Email: {m.email}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {m.status === 'ativo' ? 'Ativo' : 'Inativo'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      {/* Modal de edição */}
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
          <Typography variant="h6" gutterBottom>
            Editar Membro
          </Typography>
          <TextField
            label="Nome"
            fullWidth
            margin="normal"
            value={membroEdicao?.nome || ''}
            onChange={e => setMembroEdicao(prev => ({ ...prev, nome: e.target.value }))}
          />
          <TextField
            label="Email"
            fullWidth
            margin="normal"
            value={membroEdicao?.email || ''}
            onChange={e => setMembroEdicao(prev => ({ ...prev, email: e.target.value }))}
          />
          <TextField
            label="Cargo"
            fullWidth
            margin="normal"
            value={membroEdicao?.cargo || ''}
            onChange={e => setMembroEdicao(prev => ({ ...prev, cargo: e.target.value }))}
          />
          <TextField
            label="Foto (URL)"
            fullWidth
            margin="normal"
            value={membroEdicao?.foto || ''}
            onChange={e => setMembroEdicao(prev => ({ ...prev, foto: e.target.value }))}
          />
          <FormControl fullWidth margin="normal">
            <InputLabel>Status</InputLabel>
            <Select
              label="Status"
              value={membroEdicao?.status || 'ativo'}
              onChange={e => setMembroEdicao(prev => ({ ...prev, status: e.target.value }))}
            >
              <MenuItem value="ativo">Ativo</MenuItem>
              <MenuItem value="inativo">Inativo</MenuItem>
            </Select>
          </FormControl>
          <Box sx={{ textAlign: 'right', mt: 2 }}>
            <Button onClick={handleCloseEdit} sx={{ mr: 1 }}>
              Cancelar
            </Button>
            <Button type="submit" variant="contained" color="success">
              Guardar
            </Button>
          </Box>
        </Box>
      </Modal>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={3000}
        onClose={() => setSnackbar(s => ({ ...s, open: false }))}
      >
        <Alert
          severity="success"
          onClose={() => setSnackbar(s => ({ ...s, open: false }))}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  )
}
