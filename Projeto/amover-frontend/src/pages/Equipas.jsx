// src/pages/Equipas.jsx
import React, { useState } from 'react'
import { useTeams } from '../context/TeamsContext'
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Collapse,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
} from '@mui/material'

export default function Equipas() {
  const { equipas } = useTeams()
  const [openId, setOpenId] = useState(null)

  const handleToggle = (id) => setOpenId(openId === id ? null : id)

  return (
    <Box sx={{ pt: 12, px: 2 }}>
      <Typography variant="h3" align="center" color="success.main" gutterBottom>
        Equipas
      </Typography>

      <Grid container spacing={4} justifyContent="center">
        {equipas.map((equipa) => (
          <Grid item key={equipa.id} xs={12} sm={6} md={4}>
            <Card sx={{ height: '100%', boxShadow: 3 }}>
              <CardContent>
                <Typography variant="h6" color="success.main">
                  {equipa.nome}
                </Typography>

                <Typography variant="body2" color="text.secondary" paragraph>
                  {equipa.descricao}
                </Typography>

                <Typography variant="body2" color="text.secondary" paragraph>
                  Nº de membros: {equipa.membros.length}
                </Typography>

                <Button
                  variant="outlined"
                  color="success"
                  size="small"
                  onClick={() => handleToggle(equipa.id)}
                >
                  {openId === equipa.id ? 'Fechar Membros' : 'Ver Membros'}
                </Button>

                <Collapse in={openId === equipa.id} sx={{ mt: 2 }}>
                  <List>
                    {equipa.membros.map((m, idx) => (
                      <ListItem key={idx} alignItems="flex-start">
                        <ListItemAvatar>
                          <Avatar alt={m.nome} src={m.foto} />
                        </ListItemAvatar>
                        <ListItemText
                          primary={`${m.nome} (${m.cargo})`}
                          secondary={m.email}
                        />
                      </ListItem>
                    ))}
                  </List>
                </Collapse>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  )
}
