<template>
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
  <div class="chefs">
    <h1>Chefes</h1>
    <div class="search-bar">
      <input
        type="text"
        placeholder="Procurar chefes..."
        v-model="searchQuery"
        @input="filterChefs"
      />
    </div>
    <div class="chefs-list">
      <div class="chef-card" v-for="chef in filteredChefs" :key="chef._id">
        <img :src="chef.profile_picture || 'default-profile.png'" :alt="chef.name" class="chef-image" />
          <h2 class="chef-name">{{ chef.name }}</h2>
            <div class="chef-rating">
              <span v-for="n in 5" :key="n" class="star">
                <i
                  :class="n <= 4 ? 'fa-solid fa-star' : 'fa-regular fa-star'" >
                </i>
        </span>
    </div>
    <button class="details-button" @click="goToDetails(chef._id)">Detalhes</button>
</div>

    </div>
    <button class="back-button" @click="goBack">Voltar</button>
  </div>
</template>



<script>
import axios from "axios";

export default {
    data() {
        return {
            searchQuery: "",
            chefs: [], 
            filteredChefs: [],
        };
    },
    created() {
        this.fetchChefs();
    },
    methods: {
  async fetchChefs() {
    try {
      const response = await axios.get('http://localhost:3000/users');
      this.chefs = response.data;
      this.filteredChefs = this.chefs;
    } catch (error) {
      console.error("Erro no GET de users:", error.message);
    }
  },
  filterChefs() {
    const query = this.searchQuery.toLowerCase();
    this.filteredChefs = this.chefs.filter((chef) =>
      chef.name.toLowerCase().includes(query)
    );
  },
  goToDetails(chefId) {
    this.$router.push({ name: 'ChefDetails', params: { id: chefId } });
  },
  goBack() {
      // Navega para a página "HomePage"
      this.$router.push({ name: 'Home' });
    }
},

};
</script>

<style scoped>
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

.chefs {
  text-align: center;
  font-family: "Avenir", Helvetica, Arial, sans-serif;
}

h1 {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.search-bar {
  margin: 1rem 0;
}

.search-bar input {
  width: 30%;
  padding: 0.5rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 5px;
}

.chefs-list {
  display: flex;
  justify-content: center;
  flex-wrap: wrap;
  gap: 1.5rem;
}

.chef-card {
  width: 200px;
  border: 1px solid #ddd;
  border-radius: 10px;
  padding: 1rem;
  text-align: center;
  box-shadow: 0px 4px 10px rgba(0, 0, 0, 0.1);
}

.chef-image {
  width: 100%;
  height: auto;
  border-radius: 10px;
}

.chef-name {
  margin: 0.5rem 0;
  font-size: 1.2rem;
}

.chef-rating {
  margin: 2.5rem 0;
}

.star {
  color: #ffcc00;
  font-size: 1.2rem;
}

.details-button {
  background-color: #ff4500;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  font-size: 1rem;
  cursor: pointer;
}

.details-button:hover {
  background-color: #e63900;
}

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
</style>