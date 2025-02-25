<template>
  <div class="home">
    <!-- Navbar -->
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

    <!-- Filtros e Pesquisa -->
    <section class="filters">
      <div class="filters-container">
        <label>Receitas Cozinha
          <select>
            <option value="">Selecionar</option>
          </select>
        </label>
        <label>Tipo de Comida
          <input type="text" placeholder="Ex: Italiana" />
        </label>
        <label>Preferências Alimentares
          <select>
            <option value="">Selecionar</option>
          </select>
        </label>
      </div>
      <div class="search-container">
  <input 
    type="text" 
    placeholder="Procure por receitas..." 
    class="search-input" 
    v-model="searchQuery"
    @input="filterReceitas" 
  />
  <router-link to="/add-recipe" class="btn-add">Adicionar Receita</router-link>
</div>

    </section>

    <!-- Cards de Receitas -->
    <section class="recipes"> 
      <div
        v-for="recipe in filteredReceitas"
        :key="recipe._id"
        class="recipe-card"
      >
        <img :src="recipe.image_url" alt="Recipe Image" class="recipe-image" />
        <div class="recipe-info">
          <h3 class="recipe-title">{{ recipe.title }}</h3>
          <p class="recipe-description">{{ truncateText(recipe.description, 10) }}</p>
          <div class="likes">
            <span class="like-icon">&#9829;</span>
            <span>{{ recipe.likes }}</span>
          </div>
          <router-link
            :to="{ name: 'RecipeDetail', params: { id: recipe._id } }"
            class="btn-view-recipe"
            style="color: white; text-decoration: none;"
            >
            Ver Receita
          </router-link>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
import { listarReceitas } from '../services/receitas'; // Importa o serviço para listar receitas do backend

export default {
  data() {
    return {
      searchQuery: "",
      recipes: [], // Primeiro está vazio
      filteredReceitas: [],
    };
  },
  async created() {
    try {
      this.recipes = await listarReceitas();
      console.log("Receitas carregadas no frontend:", this.recipes);
      this.filteredReceitas = this.recipes; // Inicializa as receitas filtradas
    } catch (error) {
      console.error("Erro ao carregar receitas:", error);
    }
  },
  methods: {
    filterReceitas() {
      const query = this.searchQuery.toLowerCase();
      this.filteredReceitas = this.recipes.filter((receita) =>
        receita.title.toLowerCase().includes(query)
      );
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


<style scoped>
.home {
  font-family: Arial, sans-serif;
  margin: 0;
  padding: 0;
  background-color: #fff;
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

.filters {
  background-color: #f9f9f9;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #ddd;
}

.filters-container {
  display: flex;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.filters-container label {
  font-size: 1rem;
  display: flex;
  flex-direction: column;
}

.search-container {
  display: flex;
  justify-content: space-between;
}

.search-input {
  width: 80%;
  padding: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 5px;
}

.btn-add {
  background-color: #e74c3c;
  color: #fff;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  cursor: pointer;
}

.recipes {
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
  padding: 2rem;
}

.recipe-card {
  width: 300px;
  margin: 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
}

.recipe-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}

.recipe-info {
  padding: 1rem;
}

.recipe-title {
  font-size: 1.2rem;
  font-weight: bold;
  margin: 0;
}

.recipe-description {
  font-size: 1rem;
  color: #555;
  margin: 0.5rem 0;
}

.likes {
  display: flex;
  align-items: center;
  font-size: 1rem;
  color: #555;
}

.like-icon {
  color: #e74c3c;
  margin-right: 0.3rem;
  font-size: 1.2rem;
}

.btn-view-recipe {
  background-color: #e74c3c;
  color: #fff;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  cursor: pointer;
  margin-top: 1rem;
  width: 100%;
}
</style>
