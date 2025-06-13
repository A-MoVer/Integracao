import { useState } from 'react';
import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import Publication from '../components/Publication';
import {
  Box,
  Typography,
  Stack,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
  Snackbar,
  Alert as MuiAlert
} from '@mui/material';
import Cookies from 'js-cookie';
import { editPublicacao, deletePublicacao } from '../services/directus';
import DeleteIcon from '@mui/icons-material/Delete';
import { IconButton } from '@mui/material';

export default function Drafts() {
  const { user } = useAuth();
  const { publications, setPublications, loading } = usePublications();

  const [editingPub, setEditingPub] = useState(null);
  const [page, setPage] = useState(1);
  const [openEditModal, setOpenEditModal] = useState(false);
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const [deletePub, setDeletePub] = useState(null);
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
  const pubsPerPage = 10;

  const showSnackbar = (message, severity = 'success') => {
    setSnackbar({ open: true, message, severity });
  };

  const handleCloseSnackbar = () => {
    setSnackbar(prev => ({ ...prev, open: false }));
  };

  if (!user) return null;

  if (loading) {
    return (
      <Box mt={6} display="flex" justifyContent="center">
        <CircularProgress size={40} />
      </Box>
    );
  }

  const drafts = publications.filter(
    (p) => p.status === 'rascunho' && p.autor?.id === user.id
  );

  const paginated = drafts.slice((page - 1) * pubsPerPage, page * pubsPerPage);
  const totalPages = Math.ceil(drafts.length / pubsPerPage);

  const openEdit = p => {
    setEditingPub({
      ...p,
      doi_or_url: p.links?.length ? [...p.links] : ['']
    });
    setOpenEditModal(true);
  };

  const closeEdit = () => setOpenEditModal(false);

  const openDelete = (p) => {
    setDeletePub(p);
    setOpenDeleteModal(true);
  };

  const closeDelete = () => setOpenDeleteModal(false);

  const handleLinkChange = (idx, value) => {
    setEditingPub(prev => ({
      ...prev,
      doi_or_url: prev.doi_or_url.map((l, i) => (i === idx ? value : l))
    }));
  };
  const addLink = () =>
    setEditingPub(prev => ({
      ...prev,
      doi_or_url: [...(prev.doi_or_url || ['']), '']
    }));
  const removeLink = idx =>
    setEditingPub(prev => ({
      ...prev,
      doi_or_url: prev.doi_or_url.filter((_, i) => i !== idx)
    }));

  const handleSave = async (pub) => {
    try {
      const token = Cookies.get('token');
      if (!pub.titulo || !pub.conteudo) {
        showSnackbar('Título e conteúdo são obrigatórios.', 'warning');
        return;
      }

      const payload = {
        title:    pub.titulo.trim(),
        abstract: pub.conteudo.trim(),
        status:   pub.status,
        equipa_id: pub.equipa_id,
        Autores:  [ user.id ],
        doi_or_url: editingPub.doi_or_url.filter(Boolean)
      };
      const savedPub = await editPublicacao(
        { id: pub.id, ...payload },
        token
      );

      setPublications(prev =>
        prev.map(p =>
          p.id !== savedPub.id ? p : {
            ...p,
            titulo: savedPub.title,
            conteudo: savedPub.abstract,
            status: savedPub.status === 'published' ? 'publicar' : savedPub.status,
            createdAt: savedPub.published_on,
            links: Array.isArray(savedPub.doi_or_url)
              ? savedPub.doi_or_url
              : savedPub.doi_or_url?.split('; ').filter(Boolean) || []
          }
        )
      );
      closeEdit();
      showSnackbar('Publicação salva com sucesso!', 'success');
    } catch (err) {
      console.error('❌ Erro ao salvar publicação:', err.message);
      showSnackbar(`Erro ao salvar publicação: ${err.message}`, 'error');
    }
  };

  const handleDelete = async () => {
    try {
      const token = Cookies.get('token');
      await deletePublicacao(deletePub.id, token);

      setPublications((prev) =>
        prev.filter((pub) => pub.id !== deletePub.id)
      );
      closeDelete();
      showSnackbar('Rascunho excluído com sucesso!', 'success');
    } catch (err) {
      console.error('❌ Erro ao excluir publicação:', err.message);
      showSnackbar(`Erro ao excluir publicação: ${err.message}`, 'error');
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Rascunhos
      </Typography>

      {drafts.length === 0 ? (
        <Typography>Não tens nenhum rascunho guardado.</Typography>
      ) : (
        <>
          <Stack spacing={2} sx={{ mt: 2 }}>
            {paginated.map((pub) => (
              <Publication
                key={pub.id}
                pub={pub}
                isEditing={editingPub?.id === pub.id}
                onEdit={openEdit}
                onDelete={openDelete}
              />
            ))}
          </Stack>

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

      <Dialog open={openEditModal} onClose={closeEdit} maxWidth="sm" fullWidth>
        <DialogTitle>Editar Rascunho</DialogTitle>
        <DialogContent>
          <TextField
            label="Título"
            fullWidth
            value={editingPub?.titulo || ''}
            onChange={(e) =>
              setEditingPub((prev) => ({ ...prev, titulo: e.target.value }))
            }
            sx={{ mb: 2 }}
          />
          <TextField
            label="Conteúdo"
            fullWidth
            multiline
            rows={4}
            value={editingPub?.conteudo || ''}
            onChange={(e) =>
              setEditingPub((prev) => ({ ...prev, conteudo: e.target.value }))
            }
          />
          <FormControl fullWidth sx={{ mt: 2 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={editingPub?.status || 'rascunho'}
              onChange={(e) =>
                setEditingPub((prev) => ({ ...prev, status: e.target.value }))
              }
            >
              <MenuItem value="publicar">Publicar</MenuItem>
              <MenuItem value="rascunho">Rascunho</MenuItem>
              <MenuItem value="oculto">Esconder</MenuItem>
            </Select>
          </FormControl>
              <Typography variant="subtitle1" sx={{ mt: 2 }}>Links (opcional)</Typography>

              {editingPub?.doi_or_url.map((link, i) => (
                <Stack key={i} direction="row" spacing={1} alignItems="center" sx={{ my: 1 }}>
                  <TextField
                    fullWidth
                    label={`Link ${i + 1}`}
                    value={link}
                    onChange={e => handleLinkChange(i, e.target.value)}
                  />
                  {i > 0 && (
                    <IconButton onClick={() => removeLink(i)}>
                      <DeleteIcon />
                    </IconButton>
                  )}
                </Stack>
              ))}

              <Button onClick={addLink} sx={{ mt: 1 }}>Adicionar link</Button>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} color="secondary">
            Cancelar
          </Button>
          <Button onClick={() => handleSave(editingPub)} color="primary">
            Salvar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={openDeleteModal} onClose={closeDelete} maxWidth="xs" fullWidth>
        <DialogTitle>Confirmar Deleção</DialogTitle>
        <DialogContent>
          <Typography>
            Tem certeza que deseja excluir o rascunho "{deletePub?.titulo}"?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDelete} color="secondary">
            Cancelar
          </Button>
          <Button onClick={handleDelete} color="primary">
            Excluir
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <MuiAlert elevation={6} variant="filled" onClose={handleCloseSnackbar} severity={snackbar.severity}>
          {snackbar.message}
        </MuiAlert>
      </Snackbar>
    </Box>
  );
}
