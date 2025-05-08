import { Link } from 'react-router-dom'
import { usePublications } from '../context/PublicationsContext'
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
} from '@mui/material'

export default function PublicationsFeed() {
  const { publications } = usePublications()

  if (publications.length === 0) {
    return (
      <Box sx={{ pt: 12, textAlign: 'center' }}>
        <Typography variant="h5">Não há publicações ainda.</Typography>
        <Button component={Link} to="/dashboard" sx={{ mt: 2 }}>
          Voltar
        </Button>
      </Box>
    )
  }

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Typography variant="h3" align="center" gutterBottom>
        Feed de Publicações
      </Typography>
      <Grid container spacing={4}>
        {publications.map(pub => (
          <Grid item xs={12} md={6} key={pub.id}>
            <Card sx={{ boxShadow: 3 }}>
              <CardContent>
                <Typography variant="h5">{pub.title}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  por {pub.author} em {new Date(pub.createdAt).toLocaleString()}
                </Typography>
                <Typography noWrap sx={{ mb: 2 }}>{pub.content}</Typography>
                <Button component={Link} to={`/publicacoes/${pub.id}`} size="small">
                  Ver detalhe
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  )
}
