// src/components/DraftsFeed.jsx
import { usePublications } from '../context/PublicationsContext';
import { Link } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Stack
} from '@mui/material';

export default function DraftsFeed({ data, onEdit }) {
  const { publications, setPublications } = usePublications();

  const handlePublish = (id) => {
    setPublications(publications.map(p =>
      p.id === id
        ? { ...p, status: 'publicar', publishedAt: new Date().toISOString() }
        : p
    ));
  };

  if (data.length === 0) {
    return <Typography variant="body2">Sem rascunhos.</Typography>;
  }

  return (
    <Stack spacing={2}>
      {data.map(pub => (
        <Card key={pub.id} variant="outlined">
          <CardContent>
            <Typography variant="h6">
              {pub.title}
            </Typography>
            <Typography
              variant="body2"
              color="text.secondary"
              gutterBottom
              noWrap
            >
              {pub.description.substring(0, 100)}…
            </Typography>

            <Box mt={1}>
              <Button
                size="small"
                variant="outlined"
                onClick={() => onEdit(pub)}
                sx={{ mr: 1 }}
              >
                Editar
              </Button>

              <Button
                size="small"
                variant="contained"
                color="success"
                onClick={() => handlePublish(pub.id)}
                sx={{ mr: 1 }}
              >
                Publicar
              </Button>

              <Button
                size="small"
                component={Link}
                to={`/publicacoes/${pub.id}`}
              >
                Ver Detalhes
              </Button>
            </Box>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}
