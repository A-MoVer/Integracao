// src/pages/Admin.jsx
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../services/Auth'
import { useTeams } from '../context/TeamsContext'
import { createEquipa, createMembro, getEquipasComMembros, getMembrosPorEquipa } from '../services/directus'

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

const descricoesMock = {
  "Serviços de Baixo-Nível": "Desenvolve os sistemas eletrónicos e mecânicos essenciais da mota, como EFI, ABS, TCS e sensores. Inclui inovação com capacetes de condução óssea para comunicação eficiente.",
  "Front-End": "Cria as interfaces do display da mota e da app myFULGORA, com base em revisões sistemáticas, grupos focais e definição de personas e cenários.",
  "Plataforma de Gestão de Ciclo de Vida": "Desenvolve uma plataforma web e mobile para gerir todo o ciclo de vida do motociclo, focando-se em manutenção, sustentabilidade e comunicação entre utilizadores e fabricantes.",
  "Segurança e Apoio à Operação da Mota": "Foca-se em IA e simulações com sensores para criar sistemas de alerta de colisão, deteção de peões e assistência à condução usando o simulador CARLA.",
  "Gestão de Sistemas e Integração": "Responsável por gerir e integrar os servidores e serviços necessários ao ecossistema, com Git, armazenamento, serviços web e containers Docker.",
  "Serviços de Logística": "Explora modelos de previsão de consumo e otimização de rotas. Desenvolve uma plataforma de mobilidade e apoia projetos académicos em engenharia de software.",
  "Serviços Remotos e Cibersegurança": "Trabalha em funcionalidades remotas como monitorização, diagnóstico, rastreamento GPS e controlo via app, com foco em proteção de dados e segurança contra ataques."
}

function Admin() {
  const { user, token } = useAuth()
  const navigate = useNavigate()
  const { equipas, setEquipas } = useTeams()

  const [open, setOpen] = useState(false)
  const [etapa, setEtapa] = useState(null)
  const [novaEquipa, setNovaEquipa] = useState({ nome: '', descricao: '' })
  const [membro, setMembro] = useState({ nome: '', email: '', foto: '', role: '', equipa: '' })
  const [message, setMessage] = useState('')
  const [openSnackbar, setOpenSnackbar] = useState(false)

  const roleName = user?.role?.name?.toLowerCase?.() || ''

  if (roleName !== 'presidente') {
    return (
      <Box sx={{ pt: 12, textAlign: 'center' }}>
        <Typography variant="h4">⚠️ Acesso restrito</Typography>
        <Typography>Esta área é exclusiva para o Presidente da Bolsa.</Typography>
      </Box>
    )
  }

  useEffect(() => {
    async function fetchEquipas() {
      try {
        const data = await getEquipasComMembros(token)
        const equipasComMembros = await Promise.all(
          data.map(async (equipa) => {
            const membros = await getMembrosPorEquipa(equipa.id).catch(() => [])
            return { ...equipa, membros }
          })
        )
        setEquipas(equipasComMembros)
      } catch (err) {
        console.error('Erro ao carregar equipas:', err)
      }
    }
    if (token) fetchEquipas()
  }, [token])

  const handleOpen = (step = 1) => {
    setEtapa(step)
    setOpen(true)
  }

  const handleClose = () => setOpen(false)

  const handleSubmitEquipa = async e => {
    e.preventDefault()
    try {
      const nova = await createEquipa(novaEquipa, token)
      setEquipas([...equipas, nova])
      setMessage('Equipa criada com sucesso!')
    } catch (err) {
      console.error(err)
      setMessage('Erro ao criar equipa!')
    }
    setOpenSnackbar(true)
    handleClose()
  }

  const handleAddMembro = async () => {
    try {
      const novoMembro = await createMembro(membro, token)
      setEquipas(
        equipas.map(eq =>
          eq.id === membro.equipa
            ? { ...eq, membros: [...(eq.membros || []), novoMembro] }
            : eq
        )
      )
      setMessage('Membro adicionado com sucesso!')
    } catch (err) {
      console.error(err)
      setMessage('Erro ao adicionar membro!')
    }
    setOpenSnackbar(true)
    setMembro({ nome: '', email: '', foto: '', role: '', equipa: '' })
    handleClose()
  }

  const handleToggleStatus = id => {
    const eq = equipas.find(e => e.id === id)
    if (!window.confirm(`Tem certeza que deseja ${eq.Status === 'ativo' ? 'desativar' : 'ativar'} esta equipa?`)) return
    setEquipas(
      equipas.map(e =>
        e.id === id ? { ...e, Status: e.Status === 'ativo' ? 'desativado' : 'ativo' } : e
      )
    )
    setMessage(`Equipa ${eq.Status === 'ativo' ? 'desativada' : 'ativada'} com sucesso!`)
    setOpenSnackbar(true)
  }

  const handleExcluirEquipa = id => {
    if (!window.confirm('Tem certeza que deseja excluir esta equipa?')) return
    setEquipas(equipas.filter(e => e.id !== id))
    setMessage('Equipa excluída com sucesso!')
    setOpenSnackbar(true)
  }

  const handleChangeMembro = e => {
    setMembro(prev => ({ ...prev, [e.target.name]: e.target.value }))
  }

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Typography variant="h3" align="center" color="success.main" gutterBottom>
        Área de Administração
      </Typography>

      <Grid container spacing={4} justifyContent="center">
        {equipas.map(equipa => (
          <Grid item xs={12} sm={6} md={4} key={equipa.id}>
            <Card
              onClick={() => navigate(`/equipas/${equipa.id}`)}
              sx={{
                maxWidth: 360,
                mx: 'auto',
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
                  {equipa.Nome || equipa.name}
                </Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  gutterBottom
                  sx={{
                    maxHeight: 60,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    display: '-webkit-box',
                    WebkitLineClamp: 3,
                    WebkitBoxOrient: 'vertical',
                  }}
                >
                  {equipa.Descricao || descricoesMock[equipa.Nome] || equipa.area || 'Sem descrição'}
                </Typography>

                <Typography variant="subtitle1">
                  Nº de Membros: {equipa.membros?.length || 0}
                </Typography>
                <Typography variant="subtitle2" color="text.secondary">
                  Status: {equipa.Status === 'ativo' ? 'Ativo' : 'Desativado'}
                </Typography>
              </CardContent>
            </Card>

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
                color={equipa.Status === 'ativo' ? 'warning' : 'success'}
                size="small"
                onClick={e => {
                  e.stopPropagation()
                  handleToggleStatus(equipa.id)
                }}
              >
                {equipa.Status === 'ativo' ? 'Desativar' : 'Ativar'}
              </Button>
            </Box>
          </Grid>
        ))}
      </Grid>

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
                      {eq.Nome || eq.name}
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

      <Snackbar open={openSnackbar} autoHideDuration={3000} onClose={() => setOpenSnackbar(false)}>
        <Alert severity="success" sx={{ width: '100%' }} onClose={() => setOpenSnackbar(false)}>
          {message}
        </Alert>
      </Snackbar>
    </Box>
  )
}

export default Admin
