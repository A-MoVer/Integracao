<template>
  <div class="app-container">
    <header class="navbar">
      <div class="logo-container">
        <img src="@/assets/icon.png" alt="Logo EuCozinho" class="logo" />
        <h1>EuCozinho</h1>
      </div>
      <nav>
        <router-link to="/">Receitas</router-link>
        <router-link :to="{ name: 'ChefDetail', params: { id: 1 } }">Chefes</router-link>
        <a href="#" class="profile-icon">
          <img src="@/assets/perfil.png" alt="Perfil" />
        </a>
        <router-link to="/">Logout</router-link>
      </nav>
    </header>

    <div class="content">
      <div class="add-recipe">
        <h1>Adicionar Receita</h1>
        <form @submit.prevent="submeterReceita">
          <div>
            <label>Título</label>
            <input v-model="novaReceita.title" type="text" placeholder="Título da receita" required />
          </div>
          <div>
            <label>Descrição</label>
            <textarea v-model="novaReceita.description" placeholder="Descrição" required></textarea>
          </div>
          <div>
            <label>ID do Chef</label>
            <input v-model="novaReceita.chef_id" type="text" placeholder="ID do Chef" required />
          </div>
          <div>
            <label>Tempo de Preparo (minutos)</label>
            <input v-model="novaReceita.prep_time" type="number" placeholder="Tempo em minutos" required />
          </div>
          <div>
            <label>Dificuldade</label>
            <select v-model="novaReceita.difficulty" required>
              <option value="">Selecionar</option>
              <option value="easy">Fácil</option>
              <option value="medium">Médio</option>
              <option value="hard">Difícil</option>
            </select>
          </div>
          <div>
            <label>URL da Imagem</label>
            <input v-model="novaReceita.image_url" type="text" placeholder="URL da Imagem" />
          </div>
          <div>
            <label>URL do Vídeo</label>
            <input v-model="novaReceita.video_url" type="text" placeholder="URL do Vídeo" />
          </div>
          <div class="button-group">
            <button type="button" @click="voltarPagina" class="back-button">Cancelar</button>
            <button type="submit">Submeter Receita</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script>
import { adicionarReceita } from "../services/receitas";

export default {
  data() {
    return {
      novaReceita: {
        title: "",
        description: "",
        chef_id: "",
        prep_time: null,
        difficulty: "",
        image_url: "",
        video_url: "",
      },
    };
  },
  methods: {
    async submeterReceita() {

      // Define URLs e mensagens padrão
      const defaultImageUrl = "https://cdn-icons-png.flaticon.com/512/857/857681.png";
      const defaultVideoMessage = "Não há vídeo disponível para esta receita";

      // Verifica se os campos estão vazios e define os valores padrão se necessário
      if (!this.novaReceita.image_url) {
        this.novaReceita.image_url = defaultImageUrl;
      }
      if (!this.novaReceita.video_url) {
        this.novaReceita.video_url = defaultVideoMessage;
      }

      try {
        console.log("Dados enviados:", JSON.parse(JSON.stringify(this.novaReceita)));
        await adicionarReceita(this.novaReceita); // Chama a função de envio
        this.$router.push("/home"); // Redireciona para a página inicial
      } catch (error) {
        console.error("Erro ao adicionar receita:", error.response?.data || error.message);
        alert("Erro ao adicionar a receita.");
      }
    },
    voltarPagina() {
      this.$router.go(-1); // Volta para a página anterior
  },
  },
};
</script>

<style scoped>
/* Estilo do background geral */
.app-container {
  background: url('@/assets/background.png') no-repeat center center fixed;
  background-size: cover;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Estilo da barra de navegação */
.navbar {
  background-color: rgba(255, 255, 255, 0.9);
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

/* Estilo do conteúdo */
.content {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-grow: 1;
  padding: 2rem;
}

.add-recipe {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
  width: 100%;
  max-width: 600px;
}

form div {
  margin-bottom: 15px;
}

form label {
  font-weight: bold;
  display: block;
  margin-bottom: 5px;
}

form input,
form textarea,
form select {
  width: 100%;
  padding: 8px;
  font-size: 16px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.button-group {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.back-button {
  border: none;
  padding: 10px 15px;
  font-size: 16px;
  border-radius: 4px;
  cursor: pointer;
}

.back-button:hover {
  background-color: #c0392b;
}

button {
  background-color: #e74c3c;
  color: white;
  border: none;
  padding: 10px 15px;
  font-size: 16px;
  border-radius: 4px;
  cursor: pointer;
}

button:hover {
  background-color: #c0392b;
}
</style>
