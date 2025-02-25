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

  <div class="recipe-detail">
    <div v-if="receita">
      <img 
        v-if="receita.image_url" 
        :src="receita.image_url" 
        alt="Imagem da Receita" 
        class="recipe-main-image"
      />
      <div class="recipe-header">
        <h1 class="recipe-title">
          {{ receita.title }} - por {{ nomeCriador || 'Autor desconhecido' }}
        </h1>
        <div class="recipe-meta">
          <p><strong>Tempo de Preparo:</strong> {{ receita.prep_time || 'N/A' }} minutos</p>
          <p><strong>Dificuldade:</strong> {{ receita.difficulty || 'N/A' }}</p>
        </div>
      </div>
      <div class="recipe-body">
        <h2>Descrição</h2>
        <p>{{ receita.description || 'Descrição não disponível' }}</p>
      </div>
      <div class="recipe-video-container" v-if="receita.video_url">
        <h2>Vídeo</h2>
        <iframe
          v-if="isYouTubeVideo(receita.video_url)"
          :src="getEmbedYouTubeUrl(receita.video_url)"
          frameborder="0"
          allowfullscreen
          class="recipe-video"
        ></iframe>
        <video 
          v-else 
          :src="receita.video_url" 
          controls 
          autoplay 
          class="recipe-video"
        ></video>
      </div>
    </div>
    <div v-else-if="carregando">
      <p>A carregar...</p>
    </div>
    <div v-else>
      <p>Erro ao carregar a receita. Por favor, tente novamente mais tarde.</p>
    </div>
  </div>

  <div class="comments-section">
    <h2>Comentários</h2>
    <div v-if="comentarios.length > 0" class="comment-list">
      <div
        v-for="(comentario, index) in comentarios"
        :key="comentario.id || index"
        class="comment-item"
      >
        <div class="comment-avatar">
          <img
            :src="comentario.avatar || 'https://cdn-icons-png.flaticon.com/512/7414/7414124.png'"
            alt="Avatar"
          />
        </div>
        <div class="comment-content">
          <p class="comment-author">{{ comentario.author }}</p>
          <p class="comment-text">{{ comentario.text }}</p>
          <small class="comment-date">{{ comentario.date }}</small>
        </div>
      </div>
    </div>
    <div v-else>
      <p>Não há comentários para esta receita. Seja o primeiro a comentar!</p>
    </div>
    <div class="comment-form">
      <textarea
        v-model="novoComentario"
        placeholder="Adicione um comentário..."
        rows="3"
        required
      ></textarea>
      <button class="btn-submit" @click="adicionarComentario">Enviar Comentário</button>
    </div>
  </div>
</template>

<script>
import { getAuth } from "firebase/auth";
import axios from '../axios';  // Certifique-se de que este caminho está correto
import { getReceitaById, getUserById } from "@/services/receitas";

async function obterTokenFirebase() {
  const auth = getAuth();
  const currentUser = auth.currentUser;

  if (currentUser) {
    const token = await currentUser.getIdToken();
    console.log("Token diretamente do Firebase:", token);
    return token;
  } else {
    throw new Error("Utilizador não autenticado.");
  }
}
export default {
  props: ["id"],
  data() {
    return {
      receita: null,
      nomeCriador: null,
      carregando: true,
      erro: null,
      comentarios: [],
      novoComentario: "",
    };
  },
  methods: {
    async carregarNomeCriador(chefId) {
      try {
        const user = await getUserById(chefId);
        this.nomeCriador = user.name;
      } catch (error) {
        console.error("Erro ao buscar o nome do criador:", error);
        this.nomeCriador = "Autor desconhecido";
      }
    },
    async obterTokenFirebase() {
      const auth = getAuth();
      const currentUser = auth.currentUser;

      if (currentUser) {
        return await currentUser.getIdToken();
      } else {
        throw new Error("Utilizador não autenticado.");
      }
    },
    isYouTubeVideo(url) {
      return url.includes("youtube.com") || url.includes("youtu.be");
    },
    getEmbedYouTubeUrl(url) {
      const videoId = url.includes("youtu.be")
        ? url.split("/").pop()
        : new URL(url).searchParams.get("v");
      return `https://www.youtube.com/embed/${videoId}`;
    },
    async carregarInteracoes() {
      try {
        const response = await fetch(`/interactions/Recipe/${this.id}`);
        if (!response.ok) throw new Error("Erro ao carregar interações");

        const data = await response.json();
        this.comentarios = data
          .filter((interacao) => interacao.type === "comment")
          .map((comment) => ({
            id: comment._id,
            author: comment.user_id?.name || "Anônimo",
            avatar:
              comment.user_id?.avatar_url ||
              "https://cdn-icons-png.flaticon.com/512/7414/7414124.png",
            text: comment.comment_text,
            date: new Date(comment.interaction_date).toLocaleString(),
          }));
      } catch (error) {
        console.error("Erro ao carregar interações:", error);
        this.comentarios = [];
      }
    },
    async adicionarComentario() {
  if (!this.novoComentario.trim()) {
    alert("O comentário não pode estar vazio.");
    return;
  }

  try {
    const token = await this.obterTokenFirebase(); // Obter token do Firebase
    const response = await axios.post(
      "/interactions", // URL para o backend
      {
        recipe_id: this.id, // Certifica-te de que este valor existe
        type: "comment",
        comment_text: this.novoComentario,
      },
      {
        headers: {
          Authorization: `Bearer ${token}`, // Adicionar token no cabeçalho
          "Content-Type": "application/json", // Tipo de conteúdo JSON
        },
      }
    );

    if (response.status === 201) {
      const novoComentario = response.data;
      this.comentarios.push({
        id: novoComentario._id,
        author: "Usuário Atual", // Ajusta conforme necessário
        text: novoComentario.comment_text,
        date: new Date(novoComentario.interaction_date).toLocaleString(),
      });
      this.novoComentario = ""; // Limpa o campo de texto
    } else {
      alert("Erro ao adicionar o comentário.");
    }
  } catch (error) {
    console.error("Erro ao adicionar comentário:", error);
    alert("Erro ao adicionar o comentário. Tente novamente.");
  }
}

  },
  async created() {
    try {
      this.carregando = true;
      this.receita = await getReceitaById(this.id);
      if (this.receita.chef_id) {
        await this.carregarNomeCriador(this.receita.chef_id);
      }
      await this.carregarInteracoes();
    } catch (error) {
      this.erro = "Erro ao carregar os detalhes da receita.";
    } finally {
      this.carregando = false;
    }
  },
};
</script>

<style scoped>
.comments-section {
  margin-top: 30px;
  padding: 20px;
  background-color: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.comment-list {
  margin-bottom: 20px;
}

.comment-item {
  display: flex;
  align-items: flex-start;
  margin-bottom: 15px;
  padding: 10px;
  background: #fff;
  border: 1px solid #ddd;
  border-radius: 8px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.comment-avatar img {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  margin-right: 15px;
}

.comment-content {
  flex: 1;
}

.comment-author {
  font-weight: bold;
  margin-bottom: 5px;
  color: #333;
}

.comment-text {
  margin-bottom: 5px;
  line-height: 1.5;
}

.comment-date {
  font-size: 0.85rem;
  color: #999;
}

.comment-form {
  display: flex;
  flex-direction: column;
}

.comment-form textarea {
  resize: none;
  padding: 10px;
  border-radius: 8px;
  border: 1px solid #ddd;
  margin-bottom: 10px;
}

.btn-submit {
  background-color: #3498db;
  color: white;
  padding: 10px 15px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.btn-submit:hover {
  background-color: #2980b9;
}
</style>
