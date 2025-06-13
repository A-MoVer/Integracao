import { useState } from 'react';
import { useAuth } from '../services/Auth';
import {
  Box,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Stack,
  Paper,
  IconButton,
  Snackbar,
  Alert
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { createPublicacao, uploadFile } from '../services/directus';

/**
 * Formulário de criação de publicação
 * - Autor  : o próprio utilizador (user.person_id)
 * - Equipa : a equipa do utilizador (user.team_id)
 *   → se o utilizador não pertencer a nenhuma equipa, não é permitido publicar.
 */
export default function NewPublication() {
  const { user, token } = useAuth();

  /* ---------- estados ---------- */
  const [title, setTitle] = useState('');
  const [abstract, setAbstract] = useState('');
  const [status, setStatus] = useState('Rascunho');
  const [links, setLinks] = useState(['']);
  const [files, setFiles] = useState([null]);

  /* Snackbar */
  const [snackbar, setSnackbar] = useState({
    open: false,
    severity: 'success', // 'success' | 'error' | 'info' | 'warning'
    message: ''
  });

  const handleCloseSnackbar = () => {
    setSnackbar(prev => ({ ...prev, open: false }));
  };

  const camposOk = title.trim() && abstract.trim();

  /* ---------- handlers dinâmicos ---------- */
  const handleLinkChange = (i, v) =>
    setLinks(links.map((l, idx) => (idx === i ? v : l)));
  const addLink = () => setLinks([...links, '']);
  const removeLink = i => setLinks(links.filter((_, idx) => idx !== i));

  const handleFileFieldChange = (i, file) => {
    setFiles(files.map((f, idx) => (idx === i ? file : f)));
  };
  const addFileField = () => setFiles([...files, null]);
  const removeFileField = i => setFiles(files.filter((_, idx) => idx !== i));

  /* ---------- submissão ---------- */
  const handleSubmit = async e => {
    e.preventDefault();

    if (!user?.person_id) {
      setSnackbar({ open: true, severity: 'error', message: 'Erro: não foi possível identificar o autor.' });
      return;
    }
    if (!user?.team_id) {
      setSnackbar({ open: true, severity: 'error', message: 'Erro: tem de pertencer a uma equipa para poder publicar.' });
      return;
    }

    // 1) subir ficheiros e recolher UUIDs
    let filesPayload = [];
    const toUpload = files.filter(Boolean);
    if (toUpload.length) {
      try {
        const uuids = await Promise.all(toUpload.map(f => uploadFile(f, token)));
        filesPayload = uuids.map(id => ({ directus_files_id: id }));
      } catch (err) {
        console.error('Erro a fazer upload dos ficheiros:', err);
        setSnackbar({ open: true, severity: 'error', message: `Falha ao enviar anexos: ${err.message}` });
        return;
      }
    }

    // 2) construir objeto de envio
    const novaPub = {
      title: title.trim(),
      abstract: abstract.trim(),
      status,
      equipa_id: user.team_id,
      authors: [user.person_id],
      doi_or_url: links.filter(l => l.trim()),
      published_on: new Date().toISOString(),
      ...(filesPayload.length && { files: filesPayload })
    };

    // 3) enviar ao Directus
    try {
      await createPublicacao(novaPub, token);
      setSnackbar({ open: true, severity: 'success', message: '✅ Publicação criada com sucesso!' });

      // reset
      setTitle('');
      setAbstract('');
      setStatus('Rascunho');
      setLinks(['']);
      setFiles([null]);
    } catch (err) {
      console.error('❌ Erro ao criar publicação:', err);
      setSnackbar({ open: true, severity: 'error', message: `Erro ao criar publicação: ${err.message}` });
    }
  };

  /* ---------- UI ---------- */
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Nova Publicação
      </Typography>

      <Paper elevation={3} sx={{ p: 4, maxWidth: 700 }}>
        <form onSubmit={handleSubmit}>
          <Stack spacing={3}>

            {/* Título */}
            <TextField
              label="Título"
              fullWidth
              required
              value={title}
              onChange={e => setTitle(e.target.value)}
            />

            {/* Resumo */}
            <TextField
              label="Resumo"
              fullWidth
              required
              multiline
              rows={6}
              value={abstract}
              onChange={e => setAbstract(e.target.value)}
            />

            {/* Status */}
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select label="Status" value={status} onChange={e => setStatus(e.target.value)}>
                <MenuItem value="Publicado">Publicar</MenuItem>
                <MenuItem value="Rascunho">Rascunho</MenuItem>
                <MenuItem value="Oculto">Esconder</MenuItem>
              </Select>
            </FormControl>

            {/* Links */}
            <div>
              <Typography variant="subtitle1">Links (opcional)</Typography>
              {links.map((link, i) => (
                <Stack direction="row" spacing={1} key={i} sx={{ my: 1 }}>
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
              <Button onClick={addLink}>Adicionar link</Button>
            </div>

            {/* Anexos */}
            <div>
              <Typography variant="subtitle1">Anexos (opcional)</Typography>
              {files.map((fileObj, i) => (
                <Stack key={i} direction="row" alignItems="center" spacing={1} sx={{ my: 1 }}>
                  <input
                    type="file"
                    onChange={e => handleFileFieldChange(i, e.target.files[0] || null)}
                  />
                  {fileObj && (
                    <Typography variant="body2" sx={{ flexGrow: 1, ml: 1 }}>
                      {fileObj.name}
                    </Typography>
                  )}
                  {i > 0 && (
                    <IconButton onClick={() => removeFileField(i)}>
                      <DeleteIcon />
                    </IconButton>
                  )}
                </Stack>
              ))}
              <Button onClick={addFileField}>Adicionar anexo</Button>
            </div>

            <Box textAlign="right">
              {!user?.team_id && (
                <Typography color="error" sx={{ mb: 1 }}>
                  ⚠️ Tem de pertencer a uma equipa para publicar.
                </Typography>
              )}
              <Button type="submit" variant="contained" color="success" disabled={!camposOk}>
                Guardar
              </Button>
            </Box>
          </Stack>
        </form>
      </Paper>

      {/* Snackbar de feedback */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
