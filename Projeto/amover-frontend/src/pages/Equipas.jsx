import React, { useState, useEffect } from 'react';
import { useTeams } from '../context/TeamsContext';
import { useAuth } from '../services/Auth';
import { getEquipasPublicas, getMembrosPorEquipa } from '../services/directus';
import {
  Box,
  Typography,
  Grid,
  Avatar,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
} from '@mui/material';
import { useLocation } from 'react-router-dom';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Mousewheel } from 'swiper/modules';
import 'swiper/css';
import 'swiper/css/mousewheel';

const descricoesMock = {
  "Serviços de Baixo-Nível": "Desenvolve os sistemas eletrónicos e mecânicos essenciais da mota, como EFI, ABS, TCS e sensores. Inclui inovação com capacetes de condução óssea para comunicação eficiente.",
  "Front-End": "Cria as interfaces do display da mota e da app myFULGORA, com base em revisões sistemáticas, grupos focais e definição de personas e cenários.",
  "Plataforma de Gestão de Ciclo de Vida": "Desenvolve uma plataforma web e mobile para gerir todo o ciclo de vida do motociclo, focando-se em manutenção, sustentabilidade e comunicação entre utilizadores e fabricantes.",
  "Segurança e Apoio à Operação da Mota": "Foca-se em IA e simulações com sensores para criar sistemas de alerta de colisão, deteção de peões e assistência à condução usando o simulador CARLA.",
  "Gestão de Sistemas e Integração": "Responsável por gerir e integrar os servidores e serviços necessários ao ecossistema, com Git, armazenamento, serviços web e containers Docker.",
  "Serviços de Logística": "Explora modelos de previsão de consumo e otimização de rotas. Desenvolve uma plataforma de mobilidade e apoia projetos académicos em engenharia de software.",
  "Serviços Remotos e Cibersegurança": "Trabalha em funcionalidades remotas como monitorização, diagnóstico, rastreamento GPS e controlo via app, com foco em proteção de dados e segurança contra ataques."
};

const ordemNivel = {
  Doutoramento: 1,
  Mestrado: 2,
  Licenciatura: 3,
  "Não definido": 4,
};

export default function Equipas() {
  const { equipas, setEquipas } = useTeams();
  const { user } = useAuth();

  const [loading, setLoading] = useState(true);
  const [openModal, setOpenModal] = useState(false);
  const [selectedPerson, setSelectedPerson] = useState(null);
  const location = useLocation();


  useEffect(() => {
    if (location.pathname === '/equipas') {
      setLoading(true);
      getEquipasPublicas()
        .then(async (equipasBase) => {
          const equipasComMembros = await Promise.all(
            equipasBase.map(async (e) => {
              const membros = await getMembrosPorEquipa(e.id);
              return { ...e, membros, leader: e.leader };
            })
          );
          setEquipas(equipasComMembros);
          setLoading(false);
        })
        .catch((err) => {
          console.error('Erro ao buscar equipas públicas:', err);
          setLoading(false);
        });
    }
  }, [location.pathname, setEquipas]);

  const handleOpen = (person) => {
    if (!user) return; // Só permite abrir se houver sessão iniciada
    setSelectedPerson(person);
    setOpenModal(true);
  };

  const handleClose = () => setOpenModal(false);

  const formatLeader = (leaderRaw) => {
    if (!leaderRaw) return null;
    return {
      id: leaderRaw.id,
      nome: `${leaderRaw.first_name || ''} ${leaderRaw.last_name || ''}`.trim() || 'Líder',
      email: leaderRaw.email || 'Sem email',
      foto: leaderRaw.avatar || '/default-orientador.jpg',
      cargo: 'Líder',
    };
  };

  return (
    <Box sx={{ pt: 0, px: 0, width: '100vw', height: '100vh' }}>
      {loading ? (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            height: '100vh',
          }}
        >
          <CircularProgress sx={{ mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            A carregar equipas... Por favor, aguarde.
          </Typography>
        </Box>
      ) : (
        <Swiper
          direction="vertical"
          slidesPerView={1}
          spaceBetween={50}
          mousewheel
          modules={[Mousewheel]}
          style={{ width: '100%', height: '100%' }}
        >
          {equipas.map((equipa) => {
            const leaderObj = formatLeader(equipa.leader);
            return (
              <SwiperSlide key={equipa.id}>
                <Box
                  sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', md: 'row' },
                    height: '100vh',
                  }}
                >
                  {/* Lado esquerdo: info da equipa */}
                  <Box
                    sx={{
                      bgcolor: '#a7d3ce',
                      color: '#004d40',
                      p: 4,
                      width: { xs: '100%', md: 400 },
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'center',
                      alignItems: 'center',
                      textAlign: 'center',
                    }}
                  >
                    <Typography variant="h4" fontWeight="bold" gutterBottom>
                      {equipa.Nome || equipa.name}
                    </Typography>
                    <Typography variant="body1" sx={{ mb: 3 }}>
                      {descricoesMock[equipa.Nome] || 'Sem descrição disponível.'}
                    </Typography>

                    <Box
                      onClick={() => handleOpen(leaderObj)}
                      sx={{
                        cursor: user ? 'pointer' : 'default',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                      }}
                    >
                      <Avatar
                        src={leaderObj?.foto}
                        alt={leaderObj?.nome}
                        sx={{ width: 120, height: 120, mb: 2 }}
                      />
                      <Typography variant="subtitle1" fontWeight="bold">
                        {leaderObj?.nome}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {leaderObj?.email}
                      </Typography>
                    </Box>
                  </Box>

                  {/* Lado direito: membros */}
                  <Box
                    sx={{
                      flex: 1,
                      px: 4,
                      py: 6,
                      background: 'linear-gradient(to bottom, #a7d3ce, #c8e6c9)',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'center',
                      minHeight: '100%',
                    }}
                  >
                    <Grid container spacing={3} justifyContent="center">
                      {(equipa.membros || [])
                        .slice()
                        .sort(
                          (a, b) =>
                            (ordemNivel[a.nivel] || 99) -
                            (ordemNivel[b.nivel] || 99)
                        )
                        .map((membro, idx) => (
                          <Grid item key={idx} xs={12} sm={6} md={4} lg={3} xl={2}>
                            <Box
                              onClick={() => handleOpen(membro)}
                              sx={{
                                p: 3,
                                bgcolor: '#ffffff',
                                borderRadius: 2,
                                boxShadow: 2,
                                textAlign: 'center',
                                width: '100%',
                                height: '100%',
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'center',
                                justifyContent: 'center',
                                cursor: user ? 'pointer' : 'default',
                              }}
                            >
                              <Avatar
                                src={membro.foto}
                                alt={membro.nome}
                                sx={{ width: 80, height: 80, mb: 2 }}
                              />
                              <Typography fontWeight="bold" textAlign="center">
                                {membro.nome}
                              </Typography>
                              <Typography
                                variant="body2"
                                color="text.secondary"
                                textAlign="center"
                              >
                                {membro.cargo} |{' '}
                                {membro.nivel || 'Sem grau definido'}
                              </Typography>
                              <Typography
                                variant="body2"
                                color="text.secondary"
                                textAlign="center"
                              >
                                {membro.email}
                              </Typography>
                            </Box>
                          </Grid>
                        ))}
                    </Grid>
                  </Box>
                </Box>
              </SwiperSlide>
            );
          })}
        </Swiper>
      )}

      {/* Modal de detalhes de pessoa */}
      <Dialog open={openModal} onClose={handleClose} fullWidth maxWidth="sm">
        <DialogTitle>{selectedPerson?.nome}</DialogTitle>
        <DialogContent dividers>
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: 2,
            }}
          >
            <Avatar
              src={selectedPerson?.foto}
              alt={selectedPerson?.nome}
              sx={{ width: 150, height: 150 }}
            />
            <DialogContentText component="div" sx={{ textAlign: 'center' }}>
              <Typography variant="body1" gutterBottom>
                <strong>Email:</strong> {selectedPerson?.email}
              </Typography>
              <Typography variant="body1" gutterBottom>
                <strong>Cargo:</strong> {selectedPerson?.cargo}
              </Typography>
              {selectedPerson?.nivel && (
                <Typography variant="body1">
                  <strong>Nível:</strong> {selectedPerson.nivel}
                </Typography>
              )}
            </DialogContentText>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>Fechar</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}