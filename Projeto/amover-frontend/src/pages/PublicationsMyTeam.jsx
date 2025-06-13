import { useState } from 'react';
import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import Publication from '../components/Publication';
import {
  Box,
  Typography,
  Stack,
  Button,
  Tabs,
  Tab,
  CircularProgress,
  Skeleton
} from '@mui/material';

export default function PublicationsMyTeam() {
  const { user } = useAuth();
  const { publications, loading } = usePublications();

  const [tab, setTab] = useState('minhas');
  const [page, setPage] = useState(1);
  const pubsPerPage = 10;

  if (!user) return null;

  /* ---------------- LOADING STATE ---------------- */
  if (loading) {
    // Spinner centrado (pode trocar por skeletons)
    return (
      <Box mt={6} display="flex" justifyContent="center">
        <CircularProgress size={40} />
      </Box>
    );

    /* Exemplo de skeletons
    return (
      <Stack spacing={2} sx={{ mt: 2 }}>
        {[...Array(3)].map((_, i) => (
          <Skeleton key={i} variant="rectangular" height={140} animation="wave" />
        ))}
      </Stack>
    );
    --------------------------------*/
  }

  /* ---------------- FILTRAGEM ---------------- */
  const handleTabChange = (_, newValue) => {
    setTab(newValue);
    setPage(1);
  };

  const filtradas = publications.filter((p) => {
    if (p.status !== 'publicar') return false;
    if (tab === 'minhas') return p.autor?.id === user.id;
    if (tab === 'equipa')
      return p.equipa.id === user.team_id && p.autor?.id !== user.id;
    return false;
  });

  const paginated = filtradas.slice(
    (page - 1) * pubsPerPage,
    page * pubsPerPage
  );
  const totalPages = Math.ceil(filtradas.length / pubsPerPage);

  /* ---------------- RENDER ---------------- */
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Minhas Publicações / da Equipa
      </Typography>

      <Tabs
        value={tab}
        onChange={handleTabChange}
        textColor="primary"
        indicatorColor="primary"
        sx={{ mb: 2 }}
      >
        <Tab label="Minhas" value="minhas" />
        <Tab label="Da Equipa" value="equipa" />
      </Tabs>

      {filtradas.length === 0 ? (
        <Typography>Nenhuma publicação encontrada.</Typography>
      ) : (
        <>
          {paginated.map((pub) => (
            <Publication key={pub.id} pub={pub} />
          ))}

          <Stack
            direction="row"
            spacing={2}
            justifyContent="center"
            sx={{ mt: 4 }}
          >
            <Button
              variant="outlined"
              disabled={page === 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Anterior
            </Button>

            <Typography sx={{ alignSelf: 'center' }}>
              Página {page} de {totalPages}
            </Typography>

            <Button
              variant="outlined"
              disabled={page === totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Seguinte
            </Button>
          </Stack>
        </>
      )}
    </Box>
  );
}
