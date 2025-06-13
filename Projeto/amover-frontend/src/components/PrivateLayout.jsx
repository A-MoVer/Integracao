import { Routes, Route } from 'react-router-dom';
import Layout from '../components/Layout';
import Dashboard from '../pages/Dashboard';
import Admin from '../pages/Admin';
import PublicationsGeral from '../pages/PublicationsGeral';
import PublicationsMyTeam from '../pages/PublicationsMyTeam';
import Drafts from '../pages/Drafts';
import DadosPessoais from '../pages/DadosPessoais';
import NewPublication from '../pages/NewPublication'
import EquipaDetails from '../pages/EquipasDetails'; 




export default function PrivateLayout() {
  return (
    <Layout>
      <Routes>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/publicacoes" element={<PublicationsGeral />} />
        <Route path="/minhas" element={<PublicationsMyTeam />} />
        <Route path="/rascunhos" element={<Drafts />} />
        <Route path="/dadospessoais" element={<DadosPessoais />} />
        <Route path="/newpublication" element={<NewPublication/>}/>
        <Route path="/equipas/:id" element={<EquipaDetails />} />
        <Route path="/admin" element={<Admin />} />
      </Routes>
    </Layout>
  );
}
