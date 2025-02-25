<template>
  <div>
    <header class="navbar">
      <div class="logo-container">
        <img src="@/assets/icon.png" alt="Logo EuCozinho" class="logo" />
        <h1>EuCozinho</h1>
      </div>
      <nav>
        <router-link to="/home">Receitas</router-link>
        <router-link :to="{ name: 'ChefDetail', params: { id: 1 } }">Chefes</router-link>

        <router-link to="/profile" class="profile-icon">
          <img src="@/assets/perfil.png" alt="Perfil" />
        </router-link>


        <router-link to="/">Logout</router-link>
      </nav>
    </header>

    <main class="profile">
      <div class="profile-info">
        <div class="profile-img">
          <img :src="userData.profile_picture" alt="Perfil" />
        </div>
        <div class="profile-details">
          <p><strong>Nome de utilizador:</strong> {{ userData.name }}</p>
          <p><strong>Email:</strong> {{ userData.email }}</p>
        </div>
        <div class="profile-actions">
          <button class="btn" @click="editProfile">Editar Perfil</button>
        </div>
      </div>

      <div class="profile-recipes">
        <h2>Receitas publicadas</h2>
        <div v-for="recipe in userRecipes" :key="recipe._id" class="recipe-card">
          <img :src="recipe.image_url" alt="Receita" />
          <h3>{{ recipe.title }}</h3>
          <p>{{ truncateText(recipe.description, 100) }}</p>
          <button class="btn">Ver receita</button>
        </div>
      </div>
    </main>
  </div>
</template>

<script>
import axios from "axios"; // Importação do Axios

export default {
  name: "ProfilePage",
  data() {
    return {
      userData: null,
      userRecipes: [],
      carregando: true,
      erro: null,
    };
  },
  async created() {
  try {
    const firebaseUser = JSON.parse(localStorage.getItem("user"));
    if (firebaseUser) {
      this.userData = {
        name: firebaseUser.name,
        email: firebaseUser.email,
        profile_picture: firebaseUser.profile_picture,
        id: firebaseUser._id,
      };

      if (!this.userData.id) {
        throw new Error("ID do user não encontrado no localStorage.");
      }
    }

    //GET das receitas do user atual
    this.userRecipes = await this.fetchUserRecipes();
    this.carregando = false;
  } catch (error) {
    this.erro = "Erro ao carregar perfil ou receitas";
    console.error(error);
  }
},
methods: {
  editProfile() {
    this.$router.push({ name: 'EditProfile' });
  },
  async fetchUserRecipes() {
    try {
      const response = await axios.get(
        `http://localhost:3000/receitas/chef_id/${this.userData.id}`
      );
      return response.data;
    } catch (error) {
      console.error("Erro ao ir buscar receitas:", error.response || error);
      this.erro = error.response?.data?.message || "Erro desconhecido";
      return [];
    }
  },
  truncateText(text, maxLength) {
    if (text.length > maxLength) {
      return text.substring(0, maxLength) + "...";
    }
    return text;
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

.profile {
  padding: 20px;
}

.profile-info {
  display: flex;
  align-items: center;
  margin-bottom: 30px;
}

.profile-img img {
  border-radius: 50%;
  width: 100px;
  height: 100px;
  margin-right: 20px;
}

.profile-details p {
  margin: 5px 0;
}

.profile-actions {
  margin-left: auto;
}

.profile-actions .btn {
  display: block;
  margin: 10px 0;
  padding: 10px 15px;
  background-color: #e74c3c;
  color: #fff;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.profile-actions .btn:hover {
  background-color: #c0392b;
}

.profile-recipes {
  margin-top: 20px;
}

.recipe-card {
  border: 1px solid #ddd;
  border-radius: 5px;
  overflow: hidden;
  text-align: center;
  padding: 10px;
  background-color: #fff;
  width: 250px;
  margin: 0 auto;
}

.recipe-card img {
  width: 100%; 
  height: auto;
  border-bottom: 1px solid #ddd;
}

.recipe-card h3 {
  margin: 10px 0;
  font-size: 1.2em;
  color: #333;
}

.recipe-card p {
  font-size: 0.9em;
  color: #666;
  margin: 0 10px 10px;
}

.recipe-card .btn {
  display: block;
  margin: 10px auto;
  padding: 10px 15px;
  background-color: #e74c3c;
  color: #fff;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  text-align: center;
}

.recipe-card .btn:hover {
  background-color: #c0392b;
}

.navbar {
  background-color: #fff;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 2rem;
  border-bottom: 1px solid #ddd;
}

.logo-container {
  display: flex;
  align-items: center;
}

.logo {
  width: 20px;
  height: 20px;
  margin-right: 0.5rem;
}

.navbar h1 {
  color: #e74c3c;
  margin: 0;
  font-size: 1.5rem;
}

.navbar nav {
  display: flex;
  align-items: center;
}

.navbar a {
  margin-left: 1rem;
  color: #333;
  text-decoration: none;
}

.profile-icon img {
  width: 30px;
  height: 30px;
  border-radius: 50%;
}

</style>
