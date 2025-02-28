const express = require('express');
const router = express.Router();
const RecipeType = require('../models/recipetype');

router.get('/', async (req, res) => {
    try {
        const types = await RecipeType.find();
        res.json(types);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.get('/:id', async (req, res) => {
    try {
        const type = await RecipeType.findById(req.params.id);
        if (!type) return res.status(404).json({ message: 'Recipe type not found' });
        res.json(type);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/', async (req, res) => {
    const type = new RecipeType({
        type_name: req.body.type_name,
    });
    try {
        const newType = await type.save();
        res.status(201).json(newType);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.patch('/:id', async (req, res) => {
    try {
        const type = await RecipeType.findById(req.params.id);
        if (!type) return res.status(404).json({ message: 'Recipe type not found' });

        if (req.body.type_name != null) {
            type.type_name = req.body.type_name;
        }

        const updatedType = await type.save();
        res.json(updatedType);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.delete('/:id', async (req, res) => {
    try {
        const type = await RecipeType.findById(req.params.id);
        if (!type) return res.status(404).json({ message: 'Recipe type not found' });
        await type.deleteOne();
        res.json({ message: 'Recipe type deleted' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;