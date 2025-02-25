const mongoose = require('mongoose');

const ratingSchema = new mongoose.Schema({
    user_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User', 
        required: true
    },
    recipe_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'Receita', 
        required: true
    },
    rating_value: {
        type: Number,
        min: 1,
        max: 5, 
        required: true
    }
});

module.exports = mongoose.model('Rating', ratingSchema);