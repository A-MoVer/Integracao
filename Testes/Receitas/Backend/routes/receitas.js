const express = require('express');
const router = express.Router();
const Receita = require('../models/receita');

// Rota para adicionar comentários a uma receita
router.post('/:id/comments', async (req, res) => {
    const { text, user_id } = req.body;

    // Validar os dados recebidos
    if (!text || !user_id) {
        return res.status(400).json({ message: 'Texto e utilizador são obrigatórios.' });
    }

    try {
        // Encontrar a receita pelo ID
        const receita = await Receita.findById(req.params.id);
        if (!receita) {
            return res.status(404).json({ message: 'Receita não encontrada.' });
        }

        // Adicionar o comentário
        const comment = { user_id, text };
        receita.comments.push(comment);

        // Guardar a receita atualizada
        await receita.save();

        res.status(201).json({ message: 'Comentário adicionado com sucesso.', comment });
    } catch (error) {
        res.status(500).json({ message: 'Erro ao adicionar o comentário.', error: error.message });
    }
});

// Rota para listar os comentários de uma receita
router.get('/:id/comments', async (req, res) => {
    console.log("ID recebido:", req.params.id);
    try {
        const receita = await Receita.findById(req.params.id).select('comments').populate('comments.user_id', 'name');
        if (!receita) {
            return res.status(404).json({ message: 'Receita não encontrada.' });
        }
        res.json(receita.comments);
    } catch (error) {
        console.error("Erro ao buscar comentários:", error.message);
        res.status(500).json({ message: 'Erro ao obter os comentários.', error: error.message });
    }
});

// Rota para obter os detalhes de uma receita
router.get('/:id', async (req, res) => {
    try {
        const receita = await Receita.findById(req.params.id).populate('chef_id', 'name');
        if (!receita) {
            return res.status(404).json({ message: 'Receita não encontrada.' });
        }
        res.json(receita);
    } catch (error) {
        res.status(500).json({ message: 'Erro ao obter a receita.', error: error.message });
    }
});

// Rota para listar todas as receitas
router.get('/', async (req, res) => {
    try {
        const receitas = await Receita.find().populate('chef_id', 'name');
        res.json(receitas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Rota para listar receitas por chef_id
router.get('/chef_id/:chefe_id', async (req, res) => {
    try {
        const receitas = await Receita.find({ chef_id: req.params.chefe_id });
        if (!receitas.length) {
            return res.status(404).json({ message: 'Nenhuma receita encontrada para este chefe.' });
        }
        res.json(receitas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Rota para criar uma receita
router.post('/', async (req, res) => {
    const { title, description, chef_id, prep_time, recipetype_id, difficulty, image_url, video_url } = req.body;

    if (!title || !description || !chef_id || !prep_time || !difficulty) {
        return res.status(400).json({ message: 'Todos os campos obrigatórios devem ser fornecidos.' });
    }

    try {
        const receita = new Receita({
            title,
            description,
            chef_id,
            prep_time,
            difficulty,
            image_url,
            video_url,
        });

        const newReceita = await receita.save();
        res.status(201).json(newReceita);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// Rota para atualizar uma receita
router.patch('/:id', async (req, res) => {
    try {
        const receita = await Receita.findById(req.params.id);
        if (!receita) {
            return res.status(404).json({ message: 'Receita não encontrada.' });
        }

        const updates = req.body;
        Object.keys(updates).forEach((key) => {
            receita[key] = updates[key];
        });

        const updatedReceita = await receita.save();
        res.json(updatedReceita);
    } catch (error) {
        res.status(500).json({ message: 'Erro ao atualizar a receita.', error: error.message });
    }
});

// Rota para apagar uma receita
router.delete('/:id', async (req, res) => {
    try {
        const receita = await Receita.findById(req.params.id);
        if (!receita) {
            return res.status(404).json({ message: 'Receita não encontrada.' });
        }

        await receita.deleteOne();
        res.json({ message: 'Receita apagada com sucesso.' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
