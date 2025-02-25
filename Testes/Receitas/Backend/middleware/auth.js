// backend/middleware/auth.js
const admin = require('../'); // Importa o Firebase já inicializado

const verifyToken = async (req, res, next) => {
  const token = req.headers.authorization?.split(" ")[1]; // Obtém o token do cabeçalho
s
  if (!token) {
    return res.status(401).json({ message: "Token não fornecido." });
  }

  try {
    const decodedToken = await admin.auth().verifyIdToken(token); // Valida o token
    req.user = decodedToken; // Guarda os dados do utilizador no request
    next();
  } catch (error) {
    console.error("Erro ao validar o token:", error);
    return res.status(403).json({ message: "Token inválido." });
  }
};

module.exports = verifyToken;
