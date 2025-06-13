import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import { Box, Typography, CircularProgress, Stack, Skeleton } from '@mui/material';
import Publication from '../components/Publication';

export default function Dashboard() {
  const { user } = useAuth();
  const { publications, loading } = usePublications();

  if (!user) return null;

  // Publicações criadas pelo utilizador
  const minhas = publications.filter(p => p.autor?.id === user.id);

  /* ---------------- LOADING STATE ---------------- */
  if (loading) {
    // Opção A: Spinner centrado
    return (
      <Box mt={6} display="flex" justifyContent="center">
        <CircularProgress size={40} />
      </Box>
    );


  }

  /* -------------- CONTEÚDO NORMAL -------------- */
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Bem-vindo, {user.first_name}
      </Typography>

      <Typography variant="body1" sx={{ mb: 3 }}>
        Aqui poderás acompanhar as tuas publicações e criar novas quando quiseres.
      </Typography>

      {minhas.length === 0 ? (
        <Typography>Nenhuma publicação tua encontrada.</Typography>
      ) : (
        minhas.map(pub => <Publication key={pub.id} pub={pub} />)
      )}
    </Box>
  );
}
