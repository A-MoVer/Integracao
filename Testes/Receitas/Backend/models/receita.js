const mongoose = require('mongoose');

const commentSchema = new mongoose.Schema({
    user_id: { type: mongoose.Schema.Types.ObjectId, ref: 'User', required: true }, // ID do utilizador que fez o comentário
    text: { type: String, required: true }, // Texto do comentário
    date: { type: Date, default: Date.now }, // Data do comentário
});

const receitaSchema = new mongoose.Schema({
    title: { type: String, required: true },
    description: { type: String, required: true },
    chef_id: { type: String, required: true },
    prep_time: { type: Number, required: true },
    difficulty: { type: String, required: true },
    image_url: { type: String },
    video_url: { type: String },
    comments: [commentSchema], // Campo para armazenar comentários
});

module.exports = mongoose.model('Receita', receitaSchema);
