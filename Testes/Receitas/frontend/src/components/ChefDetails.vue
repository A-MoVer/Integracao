<template>
    <div v-if="chef" class="chef-details">
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
  
      <!-- Detalhes do chef -->
      <div class="chef-header">
        <img :src="chef.profile_picture || 'default-profile.png'" alt="Chef Image" class="chef-image" />
        <h1>{{ chef.name }}</h1>
        <p>{{ chef.bio || 'Sem bio disponível' }}</p>
  
        <!-- Rating do chef -->
        <div class="chef-rating">
          <span v-for="n in 5" :key="n" class="star">
            <i :class="n <= chef.rating ? 'fa-solid fa-star' : 'fa-regular fa-star'"></i>
          </span>
        </div>
      </div>
  
      <!-- Listagem de receitas publicadas pelo chef -->
      <div class="chef-recipes">
      <h2>Receitas Publicadas</h2>
      <div class="recipe-list">
        <div v-for="recipe in chef.recipes" :key="recipe._id" class="recipe-card">
          <img :src="recipe.image || 'default-recipe.png'" :alt="recipe.title" class="recipe-image" />
          <h3 class="recipe-title">{{ recipe.title }}</h3>
          <p class="recipe-description">
            {{ truncateDescription(recipe.description || 'Sem descrição') }}
          </p>
          <router-link :to="{ name: 'RecipeDetail', params: { id: recipe._id } }" class="recipe-button">Ver receita</router-link>
        </div>
      </div>
    </div>

  
      <button class="back-button" @click="oBack">Voltar</button>
    </div>
  </template>
  
  <script>
  import axios from "axios";
  
  export default {
    data() {
      return {
        chef: null, // Objeto que armazenará os detalhes do chef
      };
    },
    created() {
      this.fetchChefDetails(); // Chama a função para carregar os detalhes do chef
    },
    methods: {
    // Função para buscar os detalhes do chef baseado no ID passado pela rota
    async fetchChefDetails() {
      const chefId = this.$route.params.id;
      try {
        const chefResponse = await axios.get(`http://localhost:3000/users/${chefId}`);
        this.chef = chefResponse.data;

        const receitasResponse = await axios.get(`http://localhost:3000/receitas/chef_id/${chefId}`);
        this.chef.recipes = receitasResponse.data;
      } catch (error) {
        console.error("Erro ao buscar os dados do chef ou receitas:", error.message);
        this.chef = {};
      }
    },
    // Função para retornar à página de detalhes do chef atual
    oBack() {
      this.$router.push({ name: 'ChefDetail', params: { id: this.$route.params.id } });
    },
    // Função para truncar descrições longas
    truncateDescription(description) {
      const maxLength = 10;
      return description.length > maxLength ? description.substring(0, maxLength) + "..." : description;
    },
  },
  };
  </script>
  
  <style scoped>
  /* Estilo da Navbar */
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
    width: 40px;
    height: 40px;
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
  
  /* Estilos para a seção de detalhes do chef */
  .chef-header {
    text-align: center;
    margin-top: 2rem;
  }
  
  .chef-image {
    width: 150px;
    height: 150px;
    border-radius: 50%;
    object-fit: cover;
  }
  
  .chef-name {
    font-size: 2rem;
    margin: 1rem 0;
  }
  
  .chef-bio {
    font-style: italic;
    color: #777;
  }
  
  .chef-rating {
    margin-top: 1rem;
  }
  
  .star {
    color: #ffcc00;
    font-size: 1.5rem;
  }
  
  .chef-recipes {
    margin-top: 2rem;
  }
  
  .recipe-list {
    display: flex;
    justify-content: center;
    flex-wrap: wrap;
    gap: 1.5rem;
  }
  
  .recipe-card {
    width: 200px;
    border: 1px solid #ddd;
    border-radius: 10px;
    padding: 1rem;
    text-align: center;
    box-shadow: 0px 4px 10px rgba(0, 0, 0, 0.1);
  }
  
  .recipe-image {
    width: 100%;
    height: auto;
    border-radius: 10px;
  }
  
  .recipe-link {
    color: #ff4500;
    text-decoration: none;
    font-size: 1rem;
  }
  
  .recipe-link:hover {
    text-decoration: underline;
  }
  
  /* Estilo do botão voltar */
  .back-button {
    background-color: #ff4500;
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 5px;
    font-size: 1rem;
    cursor: pointer;
    margin-top: 2rem;
  }
  
  .back-button:hover {
    background-color: #e63900;
  }

.recipe-list {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 1.5rem;
}

.recipe-card {
  width: 250px; /* Largura ajustada para parecer com o exemplo */
  border: 1px solid #ddd;
  border-radius: 10px;
  box-shadow: 0px 4px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  text-align: center;
}

.recipe-image {
  width: 100%;
  height: 150px; /* Altura fixa para padronizar as imagens */
  object-fit: cover;
}

.recipe-title {
  font-size: 1.25rem;
  font-weight: bold;
  margin: 0.5rem 0;
}

.recipe-description {
  font-size: 0.9rem;
  color: #666;
  margin: 0 0 1rem 0;
}

.recipe-button {
  display: inline-block;
  background-color: #e74c3c;
  color: white;
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  font-size: 1rem;
  transition: background-color 0.3s;
}

.recipe-button:hover {
  background-color: #c0392b;
}
</style>