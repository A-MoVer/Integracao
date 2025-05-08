import { useParams, useNavigate } from 'react-router-dom'
import { usePublications } from '../context/PublicationsContext'
import {
  Box,
  Typography,
  Button,
  Chip,
  Link as MuiLink
} from '@mui/material'

export default function PublicationDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { publications } = usePublications()

  const pub = publications.find(p => p.id === parseInt(id, 10))

  if (!pub) {
    return (
      <Box sx={{ pt: 12, textAlign: 'center' }}>
        <Typography color="error">Publicação não encontrada!</Typography>
        <Button onClick={() => navigate('/dashboard')} sx={{ mt: 2 }}>
          ← Voltar
        </Button>
      </Box>
    )
  }

  return (
    <Box sx={{ pt: 12, px: 2, maxWidth: 700, mx: 'auto' }}>
      <Button onClick={() => navigate(-1)} sx={{ mb: 2 }}>
        ← Voltar
      </Button>
      <Typography variant="h4" gutterBottom>
        {pub.title}
      </Typography>
      <Typography variant="subtitle2" color="text.secondary" gutterBottom>
        {pub.status === 'publicado' ? 'Publicado' : pub.status === 'rascunho' ? 'Rascunho' : 'Escondido'} —{' '}
        {pub.author} em {new Date(pub.createdAt).toLocaleString()}
      </Typography>
      <Typography variant="body1" paragraph>
        {pub.description}
      </Typography>
      {pub.tags && pub.tags.map((t, i) => <Chip key={i} label={t} size="small" sx={{ mr:1 }} />)}
      {pub.links && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle1">Links:</Typography>
          {pub.links.map((link, i) => (
            <MuiLink href={link} target="_blank" rel="noopener" key={i} display="block">
              {link}
            </MuiLink>
          ))}
        </Box>
      )}
    </Box>
  )
}
