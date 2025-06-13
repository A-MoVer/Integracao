import { Box, Toolbar } from '@mui/material';
import SidebarUtilizador from './SidebarUtilizador';
import { useAuth } from '../services/Auth';

export default function Layout({ children }) {
  const { user } = useAuth();
  const roleName = user?.perfil?.name || user?.role?.name || 'sem papel';

  const handleEditProfile = () => alert('Editar perfil ainda não implementado');
  const handleNewPublication = () => alert('Nova publicação');

  return (
    <Box sx={{ display: 'flex' }}>
      <SidebarUtilizador
        user={user}
        roleName={roleName}
        onNew={handleNewPublication}
        onEditProfile={handleEditProfile}
      />

      <Box component="main" sx={{ flexGrow: 1, p: 3, ml: '250px' }}>
        <Box sx={{ height: '0px' }} /> {/* Espaço para a navbar fixa */}
        {children}
      </Box>
    </Box>
  );
}
