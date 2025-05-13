// src/pages/Dashboard.jsx
import { useState } from 'react';
import { useAuth } from '../services/Auth';
import { usePublications } from '../context/PublicationsContext';
import PublicationsFeed  from '../components/PublicationsFeed';
import DraftsFeed        from '../components/DraftsFeed';
import CreatePublication from '../components/CreatePublication';
import { Box, Typography, Tabs, Tab, Button } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';

export default function Dashboard() {
  /* 1️⃣  TODOS os hooks em topo de função, sem condições */
  const { user } = useAuth();
  const { publications, setPublications } = usePublications();


  const [tab, setTab]           = useState(0);
  const [modalPub, setModalPub] = useState(null);

  if (!user) return null;


  const handleTab  = (_, v) => setTab(v);

  /* 3️⃣  Processamento de dados */
  const published = publications.filter(p => p.status === 'publicar');
  const drafts    = publications.filter(p => p.status === 'rascunho');
  const hidden    = publications.filter(
                      p => p.status === 'esconder' && p.author === user.email);

  const openNew    = ()          => setModalPub({});
  const openEdit   = (pub)       => setModalPub(pub);
  const closeModal = ()          => setModalPub(null);
  const handleSave = (saved) => {
    setPublications(prev =>
      prev.some(p => p.id === saved.id)
        ? prev.map(p => p.id === saved.id ? saved : p)   // editar
        : [...prev, saved]                               // criar
    );
    closeModal();
  };

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Typography variant="h4" align="center" gutterBottom>
        {/* 4️⃣  Usa role.name ou outro campo string */}
        Bem‑vindo, {user.email} ({user.role?.name || user.role})
      </Typography>

      {['bolseiro','tecnico','orientador'].includes(user.role?.name) && (
        <Box textAlign="center" sx={{ mb: 3 }}>
          <Button
            variant="contained"
            color="success"
            startIcon={<AddIcon />}
            onClick={openNew}
          >
            Nova Publicação / Rascunho
          </Button>
        </Box>
      )}

      <Tabs value={tab} onChange={handleTab} centered textColor="success"
            indicatorColor="success" sx={{ mb: 4 }}>
        <Tab label={`Feed (${published.length})`} />
        <Tab label={`Rascunhos (${drafts.length})`} />
        {hidden.length > 0 && <Tab label={`Ocultas (${hidden.length})`} />}
      </Tabs>

      {tab === 0 && <PublicationsFeed data={published} onEdit={openEdit} />}
      {tab === 1 && <DraftsFeed       data={drafts}    onEdit={openEdit} />}
      {tab === 2 && hidden.length > 0 &&
        <PublicationsFeed data={hidden} onEdit={openEdit} />}

      <CreatePublication
        open={!!modalPub}
        publication={modalPub}
        onSave={handleSave}
        onClose={closeModal}
      />
    </Box>
  );
}
