// src/pages/Admin.jsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'    // ← só importar uma vez
import { useAuth } from '../context/AuthContext'
import { useTeams } from '../context/TeamsContext'
import {
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Modal,
  Box,
  TextField,
  Snackbar,
  Alert,
  FormControl,
  Select,
  InputLabel,
  MenuItem,
} from '@mui/material'

function Admin() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const { equipas, setEquipas } = useTeams()

  const [open, setOpen] = useState(false)
  const [etapa, setEtapa] = useState(null) // 1 = criar equipa, 2 = adicionar membro
  const [novaEquipa, setNovaEquipa] = useState({ nome: '', descricao: '' })
  const [membro, setMembro] = useState({ nome: '', email: '', foto: '', role: '', equipa: '' })

  const [message, setMessage] = useState('')
  const [openSnackbar, setOpenSnackbar] = useState(false)

  const handleOpen = (step = 1) => {
    setEtapa(step)
    setOpen(true)
  }
  const handleClose = () => setOpen(false)

  // 1) Criar equipa
  const handleSubmitEquipa = e => {
    e.preventDefault()
    const id = Date.now()
    const nova = { ...novaEquipa, id, membros: [], status: 'ativo' }
    setEquipas([...equipas, nova])
    setNovaEquipa({ nome: '', descricao: '' })
    setMessage('Equipa criada com sucesso!')
    setOpenSnackbar(true)
    handleClose()
  }

  // 2) Adicionar membro
  const handleAddMembro = () => {
    setEquipas(
      equipas.map(eq =>
        eq.id === membro.equipa
          ? {
              ...eq,
              membros: [
                ...eq.membros,
                { nome: membro.nome, cargo: membro.role, email: membro.email, foto: membro.foto },
              ],
            }
          : eq
      )
    )
    setMembro({ nome: '', email: '', foto: '', role: '', equipa: '' })
    setMessage('Membro adicionado com sucesso!')
    setOpenSnackbar(true)
    handleClose()
  }

  // 3) Ativar/Desativar
  const handleToggleStatus = id => {
    const eq = equipas.find(e => e.id === id)
    if (
      !window.confirm(
        `Tem certeza que deseja ${eq.status === 'ativo' ? 'desativar' : 'ativar'} esta equipa?`
      )
    )
      return
    setEquipas(
      equipas.map(e =>
        e.id === id ? { ...e, status: e.status === 'ativo' ? 'desativado' : 'ativo' } : e
      )
    )
    setMessage(`Equipa ${eq.status === 'ativo' ? 'desativada' : 'ativada'} com sucesso!`)
    setOpenSnackbar(true)
  }

  // 4) Excluir
  const handleExcluirEquipa = id => {
    if (!window.confirm('Tem certeza que deseja excluir esta equipa?')) return
    setEquipas(equipas.filter(e => e.id !== id))
    setMessage('Equipa excluída com sucesso!')
    setOpenSnackbar(true)
  }

  const handleChangeMembro = e => {
    setMembro(prev => ({ ...prev, [e.target.name]: e.target.value }))
  }

  if (user?.role !== 'presidente') {
    return (
      <Box sx={{ pt: 12, textAlign: 'center' }}>
        <Typography variant="h4">⚠️ Acesso restrito</Typography>
        <Typography>Esta área é exclusiva para o Presidente da Bolsa.</Typography>
      </Box>
    )
  }

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Typography variant="h3" align="center" color="success.main" gutterBottom>
        Área de Administração
      </Typography>

      {/* GRID DE CARTÕES */}
      <Grid container spacing={4} justifyContent="center">
        {equipas.map(equipa => (
          <Grid item xs={12} sm={6} md={4} key={equipa.id}>
            <Card
              onClick={() => navigate(`/equipas/${equipa.id}`)}
              sx={{
                boxShadow: 3,
                borderRadius: 2,
                p: 2,
                bgcolor: '#f1f8e9',
                textAlign: 'center',
                cursor: 'pointer',
              }}
            >
              <CardContent>
                <Typography variant="h5" color="success.main">
                  {equipa.nome}
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  {equipa.descricao}
                </Typography>
                <Typography variant="subtitle1">
                  Nº de Membros: {equipa.membros.length}
                </Typography>
                <Typography variant="subtitle2" color="text.secondary">
                  Status: {equipa.status === 'ativo' ? 'Ativo' : 'Desativado'}
                </Typography>
              </CardContent>
            </Card>

            {/* BOTÕES EXCLUIR / ATIVAR */}
            <Box sx={{ textAlign: 'center', mt: 1 }}>
              <Button
                variant="outlined"
                color="error"
                size="small"
                onClick={e => {
                  e.stopPropagation()
                  handleExcluirEquipa(equipa.id)
                }}
                sx={{ mr: 1 }}
              >
                Excluir
              </Button>
              <Button
                variant="outlined"
                color={equipa.status === 'ativo' ? 'warning' : 'success'}
                size="small"
                onClick={e => {
                  e.stopPropagation()
                  handleToggleStatus(equipa.id)
                }}
              >
                {equipa.status === 'ativo' ? 'Desativar' : 'Ativar'}
              </Button>
            </Box>
          </Grid>
        ))}
      </Grid>

      {/* BOTÕES CRIAR */}
      <Box sx={{ textAlign: 'center', mt: 4 }}>
        <Button variant="contained" color="success" onClick={() => handleOpen(1)}>
          Criar Nova Equipa
        </Button>
        <Button
          variant="contained"
          color="success"
          sx={{ ml: 2 }}
          onClick={() => handleOpen(2)}
        >
          Adicionar Membros
        </Button>
      </Box>

      {/* MODAL CRIAR / ADICIONAR */}
      <Modal open={open} onClose={handleClose}>
        <Box
          component={etapa === 1 ? 'form' : 'div'}
          onSubmit={etapa === 1 ? handleSubmitEquipa : undefined}
          sx={{
            position: 'absolute',
            top: '50%', left: '50%',
            transform: 'translate(-50%, -50%)',
            width: 400, bgcolor: 'background.paper',
            p: 4, borderRadius: 2, boxShadow: 24,
          }}
        >
          <Typography variant="h6" color="success.main" gutterBottom>
            {etapa === 1 ? 'Criar Nova Equipa' : 'Adicionar Membros à Equipa'}
          </Typography>

          {etapa === 1 ? (
            <>
              <TextField
                label="Nome da Equipa" fullWidth margin="normal"
                value={novaEquipa.nome}
                onChange={e => setNovaEquipa(prev => ({ ...prev, nome: e.target.value }))}
              />
              <TextField
                label="Descrição" fullWidth margin="normal"
                value={novaEquipa.descricao}
                onChange={e => setNovaEquipa(prev => ({ ...prev, descricao: e.target.value }))}
              />
              <Box sx={{ textAlign: 'center', mt: 2 }}>
                <Button type="submit" variant="contained" color="success">
                  Criar Equipa
                </Button>
              </Box>
            </>
          ) : (
            <>
              <TextField
                label="Nome" name="nome" fullWidth margin="normal"
                value={membro.nome} onChange={handleChangeMembro}
              />
              <TextField
                label="Email" name="email" fullWidth margin="normal"
                value={membro.email} onChange={handleChangeMembro}
              />
              <TextField
                label="Foto (URL)" name="foto" fullWidth margin="normal"
                value={membro.foto} onChange={handleChangeMembro}
              />
              <FormControl fullWidth margin="normal">
                <InputLabel>Papel</InputLabel>
                <Select
                  label="Papel" name="role"
                  value={membro.role} onChange={handleChangeMembro}
                >
                  <MenuItem value="bolseiro">Bolseiro</MenuItem>
                  <MenuItem value="tecnico">Técnico</MenuItem>
                  <MenuItem value="orientador">Orientador</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth margin="normal">
                <InputLabel>Equipa</InputLabel>
                <Select
                  label="Equipa" name="equipa"
                  value={membro.equipa} onChange={handleChangeMembro}
                >
                  {equipas.map(eq => (
                    <MenuItem key={eq.id} value={eq.id}>
                      {eq.nome}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Box sx={{ textAlign: 'center', mt: 2 }}>
                <Button variant="contained" color="success" onClick={handleAddMembro}>
                  Adicionar Membro
                </Button>
              </Box>
            </>
          )}
        </Box>
      </Modal>

      {/* SNACKBAR */}
      <Snackbar
        open={openSnackbar} autoHideDuration={3000}
        onClose={() => setOpenSnackbar(false)}
      >
        <Alert severity="success" sx={{ width: '100%' }} onClose={() => setOpenSnackbar(false)}>
          {message}
        </Alert>
      </Snackbar>
    </Box>
  )
}
export default Admin;