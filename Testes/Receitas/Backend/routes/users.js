//Receitas\Backend\routes\users.js

const express = require('express');
const router = express.Router();
const User = require('../models/user');


router.get('/', async (req, res) => {
    try {
        const users = await User.find(); // Todos os users
        res.json(users); // Retorna como JSON
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

  
  // ROTA para atualizar o perfil do user atual
  router.patch('/users/profile', async (req, res) => {
      try {
          const { userId, name } = req.body; // Certifique-se que o body tem esses campos
          if (!userId || !name) {
              return res.status(400).json({ error: 'User ID and name are required' });
          }
  
          const updatedUser = await User.findByIdAndUpdate(
              userId, 
              { name }, 
              { new: true }
          );
  
          if (!updatedUser) {
              return res.status(404).json({ error: 'User not found' });
          }
  
          res.json({ message: 'Profile updated successfully', user: updatedUser });
      } catch (error) {
          console.error('Error updating user:', error);
          res.status(500).json({ error: 'Internal Server Error' });
      }
  });
  

router.get('/:id', getUser, (req, res) => {
    res.json(res.user);
});

router.post('/', async (req, res) => {
    const user = new User({
        name: req.body.name,
        email: req.body.email,
        password: req.body.password,
        user_type: req.body.user_type,
        profile_picture: req.body.profile_picture,
    });
    try {
        const newUser = await user.save();
        res.status(201).json(newUser);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.patch('/:id', getUser, async (req, res) => {
    if (req.body.name != null) res.user.name = req.body.name;
    if (req.body.email != null) res.user.email = req.body.email;
    if (req.body.password != null) res.user.password = req.body.password;
    if (req.body.user_type != null) res.user.user_type = req.body.user_type;
    if (req.body.profile_picture != null) res.user.profile_picture = req.body.profile_picture;

    try {
        const updatedUser = await res.user.save();
        res.json(updatedUser);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

router.delete('/:id', getUser, async (req, res) => {
    try {
        await res.user.deleteOne();
        res.json({ message: 'User deleted' });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Rota para enviar o link de redefinição de senha
router.post('/password-reset', async (req, res) => {
    const { email } = req.body;

    try {
        const user = await User.findOne({ email });
        if (!user) {
            return res.status(404).json({ message: 'Usuário não encontrado.' });
        }

        // Simule o envio de email (ou implemente nodemailer aqui)
        const resetLink = `http://localhost:8080/reset-password?email=${encodeURIComponent(email)}`;
        console.log(`Link para redefinir senha: ${resetLink}`);

        res.json({ message: 'Link enviado para o email associado.' });
    } catch (error) {
        console.error('Erro ao enviar link:', error);
        res.status(500).json({ message: 'Erro interno do servidor.' });
    }
});

// Rota para redefinir a senha diretamente com o email
router.post('/password-reset/submit', async (req, res) => {
    const { email, newPassword } = req.body;

    try {
        const user = await User.findOne({ email });
        if (!user) {
            return res.status(404).json({ message: 'Usuário não encontrado.' });
        }

        // Atualizar a senha diretamente
        user.password = newPassword;
        await user.save();

        res.json({ message: 'Senha redefinida com sucesso.' });
    } catch (error) {
        console.error('Erro ao redefinir senha:', error);
        res.status(500).json({ message: 'Erro interno do servidor.' });
    }
});


//POST PARA O FIREBASE PARA CRIAR UM USER NO CASO DE ELE NAO TER CONTA
router.post('/auth/google', async (req, res) => {
    const { name, email, profile_picture } = req.body;

    try {
        // Verifica se o user já existe na database
        let user = await User.findOne({ email });

        if (!user) {
            // Se não, cria um novo
            user = new User({
                name,
                email,
                password: "", // Não precisa de senha para login via Google
                user_type: "chef", // Tipo padrão
                profile_picture,
            });

            await user.save();
        }

        // Retorna os dados do user (incluindo o _id)
        res.status(200).json({
            _id: user._id,
            name: user.name,
            email: user.email,
            profile_picture: user.profile_picture,
            user_type: user.user_type,
        });
    } catch (error) {
        console.error('Erro ao autenticar com Google:', error);
        res.status(500).json({ message: 'Erro ao autenticar com Google', error });
    }
});


const authenticateToken = require('../middleware/auth');

// Rota para ir buscar o perfil do user autenticado
router.get('/profile', authenticateToken, async (req, res) => {
    try {
      const user = await User.findById(req.user.id).select('-password');
      if (!user) {
        return res.status(404).json({ message: 'Utilizador não encontrado' });
      }
      res.json(user);
    } catch (error) {
      res.status(500).json({ message: error.message });
    }
  });

//Login do user
router.post('/login', async (req, res) => {
    const { email, password } = req.body;

    try {
        // Verifica se já existe
        const user = await User.findOne({ email });
        if (!user || user.password !== password) {
            return res.status(401).json({ message: 'Email ou senha incorretos' });
        }

        // Retorna os dados do user (exceto a senha)
        res.status(200).json({
            id: user._id,
            name: user.name,
            email: user.email,
            user_type: user.user_type,
            profile_picture: user.profile_picture,
        });
    } catch (error) {
        console.error('Erro no login:', error);
        res.status(500).json({ message: 'Erro interno do servidor' });
    }
});


async function getUser(req, res, next) {
    let user;
    try {
        user = await User.findById(req.params.id);
        if (user == null) {
            return res.status(404).json({ message: 'User not found' });
        }
    } catch (error) {
        return res.status(500).json({ message: error.message });
    }
    res.user = user;
    next();
}


module.exports = router;