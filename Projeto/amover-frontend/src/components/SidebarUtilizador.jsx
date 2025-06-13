import {
  Avatar,
  Box,
  Divider,
  List,
  ListItem,
  ListItemText,
  Typography,
  Toolbar,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import LogoutIcon from '@mui/icons-material/Logout';
import DescriptionIcon from '@mui/icons-material/Description';
import DraftsIcon from '@mui/icons-material/Drafts';
import PersonIcon from '@mui/icons-material/Person';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import GroupsIcon from '@mui/icons-material/Groups';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../services/Auth';

export default function SidebarUtilizador({
  user,
  roleName,
  onNew,
  onEditProfile,
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { logout } = useAuth();

  if (!user) return null;

  const isPresidente = roleName?.toLowerCase?.() === 'presidente';

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const itemStyle = {
    color: 'black',
    textDecoration: 'none',
  };

  const iconStyle = {
    mr: 1,
    color: 'black',
  };

  return (
    <Box
      sx={{
        width: 250,
        bgcolor: '#e0f2f1',
        height: '100vh',
        p: 3,
        position: 'fixed',
        top: 0,
        left: 0,
        overflowY: 'auto',
        boxShadow: 3,
        zIndex: 1300,
      }}
    >
      <Toolbar />

      <Avatar
        src={user.avatar || ''}
        alt={user.first_name || ''}
        sx={{ width: 72, height: 72, mb: 2 }}
      />
      <Typography variant="h6">{user.first_name || 'Utilizador'}</Typography>
      <Typography variant="body2" color="text.secondary">
        ({roleName || 'sem papel'})
      </Typography>

      <Divider sx={{ my: 2 }} />

      <List>
        <ListItem
          button
          component={Link}
          to="/newpublication"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <AddIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Nova Publicação" />
        </ListItem>

        <ListItem
          button
          component={Link}
          to="/publicacoes"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <DescriptionIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Publicações Gerais" />
        </ListItem>

        <ListItem
          button
          component={Link}
          to="/minhas"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <DescriptionIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Minhas / da Equipa" />
        </ListItem>

        <ListItem
          button
          component={Link}
          to="/rascunhos"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <DraftsIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Rascunhos" />
        </ListItem>

        {/* Novo item Equipas */}
        <ListItem
          button
          component={Link}
          to="/equipas"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <GroupsIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Equipas" />
        </ListItem>

        <ListItem
          button
          component={Link}
          to="/dadospessoais"
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <PersonIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Dados Pessoais" />
        </ListItem>

        {/* Só para presidentes */}
        {isPresidente && (
          <ListItem
            button
            component={Link}
            to="/admin"
            sx={{ '& .MuiListItemText-root': itemStyle }}
          >
            <AdminPanelSettingsIcon fontSize="small" sx={iconStyle} />
            <ListItemText primary="Administração" />
          </ListItem>
        )}

        <Divider sx={{ my: 2 }} />

        <ListItem
          button
          onClick={handleLogout}
          sx={{ '& .MuiListItemText-root': itemStyle }}
        >
          <LogoutIcon fontSize="small" sx={iconStyle} />
          <ListItemText primary="Logout" />
        </ListItem>
      </List>
    </Box>
  );
}
