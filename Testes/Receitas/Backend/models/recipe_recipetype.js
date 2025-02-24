const mongoose = require('mongoose');

const recipeRecipeTypeSchema = new mongoose.Schema({
    recipe_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'Receita', 
        required: true
    },
    recipe_type_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'RecipeType', 
        required: true
    }
});

module.exports = mongoose.model('Recipe_RecipeType', recipeRecipeTypeSchema);