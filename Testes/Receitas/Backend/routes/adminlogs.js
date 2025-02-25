const express = require('express');
const router = express.Router();
const AdminLog = require('../models/adminlog');

router.get('/', async (req, res) => {
    try {
        const logs = await AdminLog.find();
        res.json(logs);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.get('/:id', async (req, res) => {
    try {
        const log = await AdminLog.findById(req.params.id);
        if (!log) return res.status(404).json({ message: 'Log not found' });
        res.json(log);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/', async (req, res) => {
    const { admin_id, action, target_type, target_id } = req.body;

    const log = new AdminLog({
        admin_id,
        action,
        target_type,
        target_id,
    });

    try {
        const newLog = await log.save();
        res.status(201).json(newLog);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.patch('/:id', async (req, res) => {
    try {
        const log = await AdminLog.findById(req.params.id);
        if (!log) return res.status(404).json({ message: 'Log not found' });

        const { admin_id, action, target_type, target_id } = req.body;

        if (admin_id != null) log.admin_id = admin_id;
        if (action != null) log.action = action;
        if (target_type != null) log.target_type = target_type;
        if (target_id != null) log.target_id = target_id;

        const updatedLog = await log.save();
        res.json(updatedLog);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.delete('/:id', async (req, res) => {
    try {
        const log = await AdminLog.findById(req.params.id);
        if (!log) return res.status(404).json({ message: 'Log not found' });
        await log.deleteOne();
        res.json({ message: 'Log deleted' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;