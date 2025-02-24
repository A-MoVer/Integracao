const express = require("express");
const router = express.Router();
const verifyToken = require("../middleware/auth");
const Interaction = require("../models/interaction");

router.post("/", verifyToken, async (req, res) => {
  try {
    const { recipe_id, comment_text, type } = req.body;

    if (!recipe_id || !comment_text || !type) {
      return res.status(400).json({ message: "Dados incompletos." });
    }

    const user_id = req.user.uid;
    const newComment = new Interaction({
      user_id,
      recipe_id,
      comment_text,
      type,
      interaction_date: new Date(),
    });

    await newComment.save();
    res.status(201).json(newComment);
  } catch (error) {
    console.error("Erro no backend ao adicionar comentário:", error);
    res.status(500).json({ message: "Erro no backend." });
  }
});

module.exports = router;
