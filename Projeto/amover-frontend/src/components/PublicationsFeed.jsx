// src/components/PublicationsFeed.jsx
import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import { Link } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid
} from '@mui/material';

export default function PublicationsFeed({ data, onEdit }) {
  const { user } = useAuth();
  const { publications, setPublications } = usePublications();

  // Só permite ação ao autor ou orientador
  const canManage = (pub) =>
    pub.author === user.email || user.role?.name === 'orientador';

  const toggleHide = (id) => {
    setPublications(
      publications.map(p =>
        p.id === id
          ? {
              ...p,
              status: p.status === 'publicar' ? 'esconder' : 'publicar'
            }
          : p
      )
    );
  };

  if (!data.length) {
    return <Typography variant="body2">Nenhuma publicação.</Typography>;
  }

  return (
    <Grid container spacing={3}>
      {data.map(pub => (
        <Grid item xs={12} md={6} key={pub.id}>
          <Card sx={{ boxShadow: 3 }}>
            <CardContent>
              <Typography variant="h6">{pub.title}</Typography>
              <Typography variant="caption" color="text.secondary">
                {new Date(pub.createdAt).toLocaleString()}
              </Typography>
              <Typography paragraph mt={1}>{pub.description}</Typography>

              <Box mt={1}>
                {/* Editar só se autorizado */}
                {onEdit && canManage(pub) && (
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => onEdit(pub)}
                    sx={{ mr: 1 }}
                  >
                    Editar
                  </Button>
                )}

                {/* Esconder ou Reexibir só se autorizado */}
                {canManage(pub) && pub.status !== 'rascunho' && (
                  <Button
                    size="small"
                    variant="contained"
                    color={pub.status === 'publicar' ? 'warning' : 'success'}
                    onClick={() => toggleHide(pub.id)}
                    sx={{ mr: 1 }}
                  >
                    {pub.status === 'publicar' ? 'Esconder' : 'Reexibir'}
                  </Button>
                )}

                <Button
                  size="small"
                  component={Link}
                  to={`/publicacoes/${pub.id}`}
                >
                  Ver Detalhe
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
