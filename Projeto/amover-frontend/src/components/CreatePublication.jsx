// src/components/CreatePublication.jsx
import { useState, useEffect } from 'react';
import { useAuth } from '../services/Auth';
import { useTeams } from '../context/TeamsContext';
import { TAGS } from '../constants/tags';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Box,
  Button,
  TextField,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  OutlinedInput,
  Chip,
  Stack,
  IconButton,
} from '@mui/material';
import AddCircleOutlineIcon   from '@mui/icons-material/AddCircleOutline';
import RemoveCircleOutlineIcon from '@mui/icons-material/RemoveCircleOutline';

export default function CreatePublication({ open, publication, onSave, onClose }) {
  const { user } = useAuth();
  const { equipas } = useTeams();

  // estados do formulário
  const [title, setTitle]         = useState('');
  const [description, setDescription] = useState('');
  const [tags, setTags]           = useState([]);
  const [links, setLinks]         = useState(['']);
  const [status, setStatus]       = useState('rascunho');
  const [teamId, setTeamId]       = useState('');

  // inicializar quando abrir (novo ou edição)
  useEffect(() => {
    if (!open) return;
    if (publication && publication.id) {
      // edição: carrega valores
      setTitle(publication.title || '');
      setDescription(publication.description || '');
      setTags(publication.tags || []);
      setLinks(publication.links?.length ? publication.links : ['']);
      setStatus(publication.status || 'rascunho');
      setTeamId(publication.teamId || '');
    } else {
      // novo: limpa e associa equipa
      setTitle('');
      setDescription('');
      setTags([]);
      setLinks(['']);
      setStatus('rascunho');
      const minhaEq = equipas.find(e =>
        e.membros.some(m => m.email === user.email)
      );
      setTeamId(minhaEq?.id ?? equipas[0]?.id ?? '');
    }
  }, [open, publication, equipas, user.email]);

  const handleSubmit = (e) => {
    e.preventDefault();
    const saved = {
      id: publication?.id || Date.now(),
      title,
      description,
      tags,
      links: links.filter(l => l.trim()),
      author: user.email,
      teamId,
      status,
      createdAt: publication?.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    onSave(saved);
  };

  const handleLinkChange = (i, val) => {
    const next = [...links];
    next[i] = val;
    setLinks(next);
  };
  const addLinkField    = () => setLinks([...links, '']);
  const removeLinkField = i => setLinks(links.filter((_, idx) => idx !== i));

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      scroll="paper"
    >
      <DialogTitle>
        {publication?.id ? 'Editar Publicação' : 'Nova Publicação / Rascunho'}
      </DialogTitle>

      <DialogContent
        dividers
        sx={{ maxHeight: '70vh', overflowY: 'auto' }}
      >
        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            label="Título"
            fullWidth required
            margin="normal"
            value={title}
            onChange={e => setTitle(e.target.value)}
          />

          <TextField
            label="Descrição"
            fullWidth required multiline rows={4}
            margin="normal"
            value={description}
            onChange={e => setDescription(e.target.value)}
          />

          <FormControl fullWidth margin="normal">
            <InputLabel>Tags</InputLabel>
            <Select
              multiple
              value={tags}
              onChange={e => {
                const v = e.target.value;
                setTags(typeof v === 'string' ? v.split(',') : v);
              }}
              input={<OutlinedInput label="Tags" />}
              renderValue={selected => (
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: .5 }}>
                  {selected.map(t => <Chip key={t} label={t} />)}
                </Box>
              )}
            >
              {TAGS.map(t => (
                <MenuItem key={t} value={t}>{t}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <Stack spacing={1} mt={2} mb={2}>
            <Typography>Links</Typography>
            {links.map((link, i) => (
              <Stack key={i} direction="row" spacing={1}>
                <TextField
                  label={`Link ${i + 1}`}
                  fullWidth
                  value={link}
                  onChange={e => handleLinkChange(i, e.target.value)}
                />
                <IconButton size="small" onClick={() => removeLinkField(i)}>
                  <RemoveCircleOutlineIcon />
                </IconButton>
              </Stack>
            ))}
            <Button
              startIcon={<AddCircleOutlineIcon />}
              onClick={addLinkField}
            >
              Adicionar Link
            </Button>
          </Stack>

          <FormControl fullWidth margin="normal">
            <InputLabel>Status</InputLabel>
            <Select
              label="Status"
              value={status}
              onChange={e => setStatus(e.target.value)}
            >
              <MenuItem value="publicar">Publicar</MenuItem>
              <MenuItem value="rascunho">Rascunho</MenuItem>
              <MenuItem value="esconder">Esconder</MenuItem>
            </Select>
          </FormControl>

          <FormControl fullWidth margin="normal" disabled>
            <InputLabel>Equipa</InputLabel>
            <Select
              label="Equipa"
              value={teamId}
            >
              {equipas.map(eq => (
                <MenuItem key={eq.id} value={eq.id}>{eq.nome}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Cancelar</Button>
        <Button
          onClick={handleSubmit}
          type="submit"
          variant="contained"
          color="success"
          disabled={!title || !description}
        >
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
