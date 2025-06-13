import { useState, useEffect } from 'react';
import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import Publication from '../components/Publication';
import {
  Box,
  Typography,
  Stack,
  Button,
  CircularProgress,
  Skeleton
} from '@mui/material';

export default function PublicationsGeral() {
  const { user } = useAuth();
  const { publications, loading } = usePublications();
  const [page, setPage] = useState(1);
  const pubsPerPage = 10;

  if (!user) return null;

  /* ---------------- LOADING ---------------- */
  if (loading) {
    // Spinner centrado (troque por skeletons se preferir)
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
    -----------------------------------------*/
  }

  /* ---------------- FILTRAGEM ---------------- */
  const publicadas = publications.filter((p) => p.status === 'publicar');
  const paginated = publicadas.slice(
    (page - 1) * pubsPerPage,
    page * pubsPerPage
  );
  const totalPages = Math.ceil(publicadas.length / pubsPerPage);

  /* ---------------- RENDER ---------------- */
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Publicações Gerais
      </Typography>

      {publicadas.length === 0 ? (
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
