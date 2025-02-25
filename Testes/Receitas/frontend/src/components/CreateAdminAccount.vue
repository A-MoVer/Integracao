<template>
    <div class="admin-create-account-container">
      <div class="create-account-card">
        <h2>Criar uma conta de administrador</h2>
        <form @submit.prevent="handleCreateAccount">
          <label for="nome">Nome Completo:</label>
          <input type="text" id="nome" v-model="nome" required />
  
          <label for="email">Email:</label>
          <input type="email" id="email" v-model="email" required />
  
          <label for="password">Password:</label>
          <input type="password" id="password" v-model="password" required />
  
          <label for="confirm-password">Repetir Password:</label>
          <input type="password" id="confirm-password" v-model="confirmPassword" required />

          <label for="image">Foto de Perfil:</label>
          <input type="foto" id="foto-perfil" v-model="fotoPerfil"/>
  
          <button type="submit" class="create-account-btn">Criar Conta</button>
        </form>
  
        <p class="admin-link">
            Já tem uma conta? <a @click="$router.push({ name: 'AdminLogin' })" style="cursor: pointer;">Faça login</a>
        </p>
      </div>
    </div>
  </template>
  
  <script>
  export default {
  name: "CreateAdminAccount",
  data() {
    return {
      nome: "",
      email: "",
      password: "",
      confirmPassword: "",
      fotoPerfil: "",
    };
  },
  methods: {
    async handleCreateAccount() {
      if (this.password !== this.confirmPassword) {
        alert("As senhas não coincidem!");
        return;
      }

      // Dados a serem enviados para o backend
      const userData = {
        name: this.nome,
        email: this.email,
        password: this.password,
        user_type: "chef", // Define o tipo como 'admin'
        profile_picture: this.fotoPerfil || "https://cdn-icons-png.flaticon.com/512/7414/7414124.png", // Foto de perfil padrão se não for fornecida
      };

      try {
        // Faz a requisição ao backend
        const response = await fetch("http://localhost:3000/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(userData),
      });

      if (!response.ok) {
        const errorData = await response.json();
        alert(`Erro ao criar conta: ${errorData.message}`);
        return;
      }

      const newUser = await response.json(); // Aqui usamos newUser para salvar o retorno
      console.log("User criado:", newUser);
      this.$router.push({ name: "AdminLogin" });

      } catch (error) {
        console.error("Erro ao criar conta:", error);
        alert("Ocorreu um erro ao criar a conta.");
      }
    },
  },
};
  </script>
  
  <style>
  .admin-create-account-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    background-image: url('@/assets/background.jpg');
    background-size: cover;
    background-repeat: no-repeat;
    background-position: center;
  }
  
  .create-account-card {
    background: white;
    padding: 30px;
    border-radius: 8px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    text-align: center;
    width: 350px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
  }
  
  h2 {
    margin-bottom: 20px;
    font-size: 1.4em;
    color: #333;
  }
  
  form {
    width: 100%;
    display: flex;
    flex-direction: column;
  }
  
  label {
    font-size: 0.9em;
    margin-bottom: 5px;
    color: #555;
  }
  
  input {
    padding: 8px;
    margin-bottom: 15px;
    border: 1px solid #ccc;
    border-radius: 5px;
    font-size: 1em;
  }
  
  .create-account-btn {
    background: #e63946;
    color: white;
    border: none;
    padding: 10px 20px;
    font-size: 1em;
    border-radius: 5px;
    cursor: pointer;
  }
  
  .create-account-btn:hover {
    background: #d62839;
  }
  
  .back-to-login {
    font-size: 0.9em;
    margin-top: 10px;
  }
  
  .back-to-login a {
    color: #007bff;
    text-decoration: none;
  }
  
  .back-to-login a:hover {
    text-decoration: underline;
  }
  </style>  