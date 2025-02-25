const mongoose = require('mongoose');

const userSchema = new mongoose.Schema({
    name: { type: String, required: true },
    email: { type: String, required: true, unique: true },
    password: { type: String }, // Torna não obrigatório
    user_type: { type: String, enum: ['guest', 'chef', 'admin'], required: true, default: 'guest' }, // Define o padrão como 'chef'
    profile_picture: { type: String },
});

module.exports = mongoose.model('User', userSchema);
