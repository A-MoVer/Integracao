const mongoose = require('mongoose');

const preferencesSchema = new mongoose.Schema({
    user_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User', 
        required: true
    },
    recipe_type_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'RecipeType', 
        required: true
    },
    theme: {
        type: String,
        required: true
    }
});

module.exports = mongoose.model('Preferences', preferencesSchema);