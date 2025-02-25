const mongoose = require("mongoose");

// Definição do esquema de interações/comentários
const interactionSchema = new mongoose.Schema({
  user_id: { type: String, required: true }, // ID do utilizador
  recipe_id: { type: String, required: true }, // ID da receita
  comment_text: { type: String, required: true }, // Texto do comentário
  type: { type: String, required: true }, // Tipo de interação (e.g., comentário, avaliação, etc.)
  interaction_date: { type: Date, default: Date.now }, // Data da interação
});

// Exporta o modelo Interaction
module.exports = mongoose.model("Interaction", interactionSchema);
