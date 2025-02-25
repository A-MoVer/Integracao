require('dotenv').config(); // Certifica-te que isto está no topo do ficheiro
const express = require('express');
const cors = require('cors');
const mongoose = require('mongoose');
const swaggerUi = require('swagger-ui-express');
const swaggerDocument = require('./swagger.json');

const app = express();

// Conectar à base de dadosteste
mongoose.connect(process.env.DB_URL, { 
  useNewUrlParser: true, 
  useUnifiedTopology: true 
}).then(() => {
  console.log('Conectado à Base de Dados!');
}).catch((error) => {
  console.error('Erro na conexão com a base de dados:', error);
});

// Configurações do express
app.use(cors());
app.use(express.json());

// Usar as rotas
const receitasRouter = require('./routes/receitas');
import usersRouter from './routes/users';
const interactionsRouter = require('./routes/interactions'); // Certifique-se de que está configurada corretamente

app.use('/receitas', receitasRouter);
app.use('/users', usersRouter);
app.use('/interactions', interactionsRouter); // Rota de interações (comentários)
app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerDocument));

// Rota 404 - Não encontrada
app.use((req, res, next) => {
  res.status(404).json({ message: 'Rota não encontrada' });
});

// Middleware de erro
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).json({ message: 'Erro interno do servidor' });
});

// Iniciar o servidor
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Servidor a rodar na porta ${PORT}!`));
