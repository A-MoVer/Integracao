const express = require('express');
const router = express.Router();
const Rating = require('../models/rating');

router.get('/', async (req, res) => {
    try {
        const ratings = await Rating.find()
            .populate('user_id', 'name')
            .populate('recipe_id', 'name');
        res.json(ratings);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.get('/user/:userId', async (req, res) => {
    try {
        const ratings = await Rating.find({ user_id: req.params.userId })
            .populate('recipe_id', 'name');
        if (!ratings || ratings.length === 0) {
            return res.status(404).json({ message: 'No ratings found for this user' });
        }
        res.json(ratings);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/', async (req, res) => {
    const rating = new Rating({
        user_id: req.body.user_id,
        recipe_id: req.body.recipe_id,
        rating_value: req.body.rating_value,
    });
    try {
        const newRating = await rating.save();
        res.status(201).json(newRating);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.patch('/:id', async (req, res) => {
    try {
        const rating = await Rating.findById(req.params.id);
        if (!rating) return res.status(404).json({ message: 'Rating not found' });

        const { user_id, recipe_id, rating_value } = req.body;

        if (user_id != null) rating.user_id = user_id;
        if (recipe_id != null) rating.recipe_id = recipe_id;
        if (rating_value != null) rating.rating_value = rating_value;

        const updatedRating = await rating.save();
        res.json(updatedRating);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.delete('/:id', async (req, res) => {
    try {
        const rating = await Rating.findById(req.params.id);
        if (!rating) return res.status(404).json({ message: 'Rating not found' });
        await rating.deleteOne();
        res.json({ message: 'Rating deleted' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;