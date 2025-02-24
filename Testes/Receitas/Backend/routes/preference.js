const express = require('express');
const router = express.Router();
const Preference = require('../models/preference');

router.get('/', async (req, res) => {
    try {
        const preferences = await Preference.find().populate('user_id', 'name');
        res.json(preferences);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.get('/user/:userId', async (req, res) => {
    try {
        const preference = await Preference.findOne({ user_id: req.params.userId });
        if (!preference) return res.status(404).json({ message: 'Preferences not found' });
        res.json(preference);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/', async (req, res) => {
    try {
        const preference = await Preference.findOneAndUpdate(
            { user_id: req.body.user_id },
            {
                theme: req.body.theme,
                notifications: req.body.notifications,
            },
            { new: true, upsert: true }
        );
        res.status(201).json(preference);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.patch('/:id', async (req, res) => {
    try {
        const preference = await Preference.findById(req.params.id);
        if (!preference) return res.status(404).json({ message: 'Preference not found' });

        const { user_id, recipe_type_id } = req.body;

        if (user_id != null) preference.user_id = user_id;
        if (recipe_type_id != null) preference.recipe_type_id = recipe_type_id;

        const updatedPreference = await preference.save();
        res.json(updatedPreference);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.delete('/:id', async (req, res) => {
    try {
        const preference = await Preference.findById(req.params.id);
        if (!preference) return res.status(404).json({ message: 'Preference not found' });
        await preference.deleteOne();
        res.json({ message: 'Preference deleted' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;