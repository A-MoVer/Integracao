import { useState } from 'react';
import { useAuth } from '../services/Auth';
import { 
  Box, 
  Typography, 
  Avatar, 
  Paper, 
  Divider, 
  Stack, 
  Button, 
  Modal, 
  TextField, 
  Input,
  IconButton       
} from '@mui/material';
import { updateUserInfo } from '../services/directus';
import { Close as CloseIcon } from '@mui/icons-material'; 

export default function DadosPessoais() {
  const { user, token } = useAuth();
  const [openModal, setOpenModal] = useState(false);
  const [newName, setNewName] = useState(user?.first_name || '');
  const [newLastName, setNewLastName] = useState(user?.last_name || '');
  const [newEmail, setNewEmail] = useState(user?.email || '');
  const [newAvatar, setNewAvatar] = useState(user?.avatar || '');
  const [newAvatarFile, setNewAvatarFile] = useState(null);

  const tipo = user?.perfil?.tipo || 'Não definido';
  const equipa = user?.perfil?.equipa || 'Não atribuída';
  const nivel = user?.perfil?.nivel_academico || 'Não definido';

  const handleOpenModal = () => setOpenModal(true);
  const handleCloseModal = () => setOpenModal(false);

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setNewAvatarFile(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setNewAvatar(reader.result);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      await updateUserInfo({ newName, newLastName, newEmail, newAvatar }, token);
      alert('Perfil atualizado com sucesso!');
      handleCloseModal();
    } catch (error) {
      console.error('Erro ao atualizar perfil:', error);
      alert('Erro ao atualizar perfil');
    }
  };

  if (!user) return null;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dados Pessoais
      </Typography>

      <Paper elevation={3} sx={{ p: 4, maxWidth: 500 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Avatar
              src={newAvatar || user.avatar || ''}
              alt={user.first_name || ''}
              sx={{ width: 72, height: 72 }}
            />
            <Box>
              <Typography variant="h6">
                {user.first_name} {user.last_name}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {user.email}
              </Typography>
            </Box>
          </Stack>

          <Divider />

          <Typography>
            <strong>Equipa:</strong> {equipa}
          </Typography>

          {tipo === 'bolseiro' && (
            <Typography>
              <strong>Nível Académico:</strong> {nivel}
            </Typography>
          )}

          {['bolseiro', 'técnico'].includes(tipo) && user?.orientador && (
            <Stack direction="row" spacing={1} alignItems="center">
              <Avatar src={user.orientador.foto} alt={user.orientador.nome} sx={{ width: 32, height: 32 }} />
              <Typography>
                <strong>Orientador:</strong> {user.orientador.nome}
              </Typography>
            </Stack>
          )}

          {['bolseiro', 'técnico'].includes(tipo) && user?.coorientadores?.length > 0 && (
            <Box>
              <Typography><strong>Coorientadores:</strong></Typography>
              <Stack spacing={1} mt={1}>
                {user.coorientadores.map((co, idx) => (
                  <Stack direction="row" spacing={1} alignItems="center" key={idx}>
                    <Avatar src={co.foto} alt={co.nome} sx={{ width: 32, height: 32 }} />
                    <Typography>{co.nome}</Typography>
                  </Stack>
                ))}
              </Stack>
            </Box>
          )}

          <Typography>
            <strong>Tipo:</strong> {tipo}
          </Typography>

          <Box textAlign="right">
            <Button variant="outlined" onClick={handleOpenModal}>
              Editar Perfil
            </Button>
          </Box>
        </Stack>
      </Paper>

      <Modal open={openModal} onClose={handleCloseModal}>
        <Box sx={{
          position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
          backgroundColor: 'white', padding: 4, maxWidth: 400, boxShadow: 24
        }}>
          <IconButton aria-label="Fechar" onClick={handleCloseModal} sx={{ position: 'absolute', top: 8, right: 8 }}>
            <CloseIcon />
          </IconButton>
          <Typography variant="h6" gutterBottom>Editar Perfil</Typography>
          <form onSubmit={handleSubmit}>
            <TextField
              label="Nome"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              fullWidth
              margin="normal"
            />
            <TextField
              label="Sobrenome"
              value={newLastName}
              onChange={(e) => setNewLastName(e.target.value)}
              fullWidth
              margin="normal"
            />
            <TextField
              label="Email"
              value={newEmail}
              onChange={(e) => setNewEmail(e.target.value)}
              fullWidth
              margin="normal"
            />
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">Foto</Typography>
              <Input
                type="file"
                inputProps={{ accept: 'image/*' }}
                onChange={handleFileChange}
                fullWidth
                margin="normal"
              />
            </Box>
            <Box textAlign="right" sx={{ mt: 2 }}>
              <Button onClick={handleCloseModal} sx={{ mr: 1 }}>
                Cancelar
              </Button>
              <Button variant="contained" type="submit">
                Atualizar
              </Button>
            </Box>
          </form>
        </Box>
      </Modal>
    </Box>
  );
}
