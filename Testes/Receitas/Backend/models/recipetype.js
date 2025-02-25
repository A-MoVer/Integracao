const mongoose = require('mongoose');

const recipeTypeSchema = new mongoose.Schema({
    type_name: {
        type: String,
        required: true
    }
});

module.exports = mongoose.model('RecipeType', recipeTypeSchema);