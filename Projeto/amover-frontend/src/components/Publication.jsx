import React from 'react';
import {
  Card,
  CardHeader,
  CardContent,
  CardActions,
  Avatar,
  Typography,
  Stack,
  Chip,
  IconButton,
  useTheme,
  Box, 
} from '@mui/material';
import { useAuth } from '../services/Auth';
import { Delete as DeleteIcon,MoreVert as MoreVertIcon } from '@mui/icons-material';

export default function Publication({ pub, onEdit, onToggle,onDelete  }) {
  const { user } = useAuth();
  const theme = useTheme();

  const isAutor = pub.autor?.id === user.id;
  const podeEditar = isAutor || user?.role?.name === 'orientador';

  // Format date
  const formattedDate = pub.createdAt
    ? new Date(pub.createdAt).toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      })
    : '';

  return (
    <Card
      elevation={3}
      sx={{
        borderRadius: 3,
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: theme.shadows[6]
        },
        mb: 2
      }}
    >
      <CardHeader
        avatar={
          <Avatar src={pub.autor?.avatar || undefined} sx={{ bgcolor: theme.palette.primary.main }}>
              { !pub.autor?.avatar && (pub.autor?.nome?.charAt(0) || 'A') }
          </Avatar>
        }
        title={
          <Typography variant="h6" component="div">
            {pub.titulo}
          </Typography>
        }
        subheader={
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="body2" color="text.secondary">
              {pub.autor?.nome}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              •
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {pub.equipa?.nome}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              • {formattedDate}
            </Typography>
          </Stack>
        }
        action={
          <IconButton aria-label="settings">
            <MoreVertIcon />
          </IconButton>
        }
      />

      <CardContent>
        <Typography variant="body1" color="text.primary">
          {pub.conteudo}
        </Typography>
          {pub.ficheiro && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2">
                Ficheiro: {' '}
                <a
                  href={pub.ficheiro.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  download={pub.ficheiro.nome}
                >
                  {pub.ficheiro.nome}
                </a>
              </Typography>
            </Box>
          )}
          {Array.isArray(pub.links) && pub.links.length > 0 && (
            <Stack direction="column" spacing={1} sx={{ mt: 2 }}>
              {pub.links.map((url, i) => (
                <Typography key={i} variant="body2">
                  <a href={url} target="_blank" rel="noopener noreferrer">
                    {url}
                  </a>
                </Typography>
              ))}
            </Stack>
          )}
      </CardContent>

 <CardActions sx={{ justifyContent: 'space-between', px: 2, pb: 2 }}>
        <Chip
          label={pub.status === 'publicar' ? 'Publicado' : 'Rascunho'}
          size="small"
          sx={{
            bgcolor: pub.status === 'publicar' ? theme.palette.success.light : theme.palette.warning.light,
            color : pub.status === 'publicar' ? theme.palette.success.dark  : theme.palette.warning.dark
          }}
        />

        <Stack direction="row" spacing={1}>
          {onEdit   && podeEditar && (
            <IconButton onClick={() => onEdit(pub)}>
              <Typography variant="button">Editar</Typography>
            </IconButton>
          )}

          {onToggle && podeEditar && (
            <IconButton onClick={() => onToggle(pub)}>
              <Typography variant="button">
                {pub.status === 'publicar' ? 'Esconder' : 'Reexibir'}
              </Typography>
            </IconButton>
          )}

          {onDelete && podeEditar && (
            <IconButton color="error" onClick={() => onDelete(pub)}>
              <DeleteIcon fontSize="small" />
              <Typography variant="button" sx={{ ml: 0.5 }}>Eliminar</Typography>
            </IconButton>
          )}
        </Stack>
      </CardActions>
    </Card>
  );
}
