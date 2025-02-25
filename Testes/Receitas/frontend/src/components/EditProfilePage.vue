<template>
  <div>
    <header class="header">
      <!-- Conteúdo do header permanece o mesmo -->
    </header>

    <main class="edit-profile">
      <h2>Edit Profile</h2>
      <div class="profile-img">
        <img src="https://static.vecteezy.com/ti/vetor-gratis/p1/2387693-icone-do-perfil-do-usuario-vetor.jpg" alt="Perfil" />
      </div>
      <form class="edit-form" @submit.prevent="saveProfile">
        <div class="form-group">
          <label for="username">Nome de Utilizador</label>
          <input
            type="text"
            id="username"
            v-model="username"
            placeholder="utilizador"
          />
        </div>
        <div class="form-actions">
          <button type="button" class="btn cancel" @click="cancelEdit">
            Cancelar
          </button>
          <button type="submit" class="btn save">Guardar</button>
        </div>
      </form>
    </main>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "EditProfilePage",
  data() {
    return {
      username: "", // Campo para o novo nome
    };
  },
  methods: {
    cancelEdit() {
      this.$router.push({ name: "ProfilePage" }); // Redireciona para a página de perfil
    },
    async saveProfile() {
      try {
        const response = await axios.patch('http://localhost:3000/users/profile', {
            userId: 'id_do_user',
            name: 'Novo Nome'
        });
        console.log('Perfil atualizado:', response.data);
    } catch (error) {
        console.error('Erro ao atualizar perfil:', error);
    }
    },
  },
};
</script>


<style>
body {
  font-family: Arial, sans-serif;
  margin: 0;
  padding: 0;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background-color: #f8f9fa;
  border-bottom: 1px solid #ddd;
}

.logo {
  display: flex;
  align-items: center;
}

.logo-img {
  width: 50px;
  height: 50px;
  margin-right: 10px;
}

.nav ul {
  display: flex;
  list-style: none;
  margin: 0;
  padding: 0;
}

.nav li {
  margin-right: 20px;
}

.nav a {
  text-decoration: none;
  color: #007bff;
}

.edit-profile {
  text-align: center;
  padding: 20px;
}

.edit-profile h2 {
  margin-bottom: 20px;
}

.profile-img img {
  border-radius: 50%;
  width: 100px;
  height: 100px;
}

.edit-form {
  max-width: 400px;
  margin: 20px auto;
  text-align: left;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
}

.form-group input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
}

.form-actions {
  display: flex;
  justify-content: space-between;
}

.btn {
  padding: 10px 15px;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.btn.cancel {
  background-color: #e74c3c;
  color: #fff;
}

.btn.cancel:hover {
  background-color: #c0392b;
}

.btn.save {
  background-color: #2ecc71;
  color: #fff;
}

.btn.save:hover {
  background-color: #27ae60;
}
</style>
