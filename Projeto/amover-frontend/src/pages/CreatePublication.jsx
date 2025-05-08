import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useTeams } from '../context/TeamsContext'
import { usePublications } from '../context/PublicationsContext'
import {
  Box,
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
} from '@mui/material'

export default function CreatePublication() {
  const { user } = useAuth()
  const { equipas } = useTeams()
  const { publications, setPublications } = usePublications()
  const navigate = useNavigate()

  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [teamId, setTeamId] = useState('')
  const [tags, setTags] = useState('')

  const handleSubmit = e => {
    e.preventDefault()
    const nova = {
      id: Date.now(),
      title,
      content,
      teamId: parseInt(teamId, 10),
      tags: tags.split(',').map(t => t.trim()).filter(Boolean),
      author: user.email,
      createdAt: new Date().toISOString(),
    }
    setPublications([...publications, nova])
    navigate('/publicacoes')
  }

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ pt: 12, px: 2, maxWidth: 600, mx: 'auto' }}>
      <Typography variant="h4" gutterBottom>Nova Publicação</Typography>

      <TextField
        label="Título"
        fullWidth margin="normal"
        value={title}
        onChange={e => setTitle(e.target.value)}
        required
      />

      <TextField
        label="Conteúdo"
        fullWidth multiline rows={6} margin="normal"
        value={content}
        onChange={e => setContent(e.target.value)}
        required
      />

      <FormControl fullWidth margin="normal">
        <InputLabel>Equipa</InputLabel>
        <Select
          value={teamId}
          label="Equipa"
          onChange={e => setTeamId(e.target.value)}
          required
        >
          {equipas.map(eq => (
            <MenuItem key={eq.id} value={eq.id}>{eq.nome}</MenuItem>
          ))}
        </Select>
      </FormControl>

      <TextField
        label="Etiquetas (separadas por vírgula)"
        fullWidth margin="normal"
        value={tags}
        onChange={e => setTags(e.target.value)}
      />

      <Box sx={{ textAlign: 'right', mt: 2 }}>
        <Button variant="contained" color="success" type="submit">
          Publicar
        </Button>
      </Box>
    </Box>
  )
}
