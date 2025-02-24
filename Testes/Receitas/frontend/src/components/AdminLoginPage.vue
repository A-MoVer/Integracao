<template>
    <div class="admin-login-container">
      <div class="login-card">
        <img src="@/assets/icon.png" alt="Logo" class="logo" />
        <h2>Fazer Login</h2>
        <form @submit.prevent="handleLogin">
          <label for="email">Email:</label>
          <input type="email" id="email" v-model="email" required />

          <label for="password">Password:</label>
          <input type="password" id="password" v-model="password" required />

          <!-- Quando o usuário clica aqui, o método handleLogin é chamado -->
          <button type="submit" class="login-btn">Login</button>
        </form>
        <p class="forgot-password">
          Esqueceu-se da password? <router-link to="/redefinir-senha">Repôr password</router-link>
        </p>
        <p class="create-account">
          Ainda não tem conta? <router-link to="/criar-conta">Criar Conta</router-link>
        </p>
      </div>
    </div>
  </template>
  
  <script>
  export default {
    name: "AdminLoginPage",
    data() {
      return {
        email: "",
        password: "",
      };
    },
      methods: {
    async handleLogin() {
      try {
        const response = await fetch('http://localhost:3000/users/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email: this.email, password: this.password }),
        });

        if (!response.ok) {
          const errorData = await response.json();
          alert(errorData.message); // Mensagem de erro
          return;
        }

        const userData = await response.json();
        console.log('Login bem-sucedido:', userData);

        // Redireciona para outra home
        this.$router.push('/home');
      } catch (error) {
        console.error('Erro ao fazer login:', error);
        alert('Erro ao tentar fazer login. Tente novamente mais tarde.');
      }
    },
  },
  };
  </script>
  
  <style>
  .admin-login-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    background-image: url('@/assets/background.jpg');
    background-size: cover;
    background-repeat: no-repeat;
    background-position: center;
  }
  
  .login-card {
    background: white;
    padding: 30px;
    border-radius: 8px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    text-align: center;
    width: 300px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
  }
  
  .logo {
    width: 80px;
    margin-bottom: 20px;
  }
  
  h2 {
    margin-bottom: 20px;
    font-size: 1.2em;
    color: #333;
    text-align: center;
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
  
  .login-btn {
    background: #e63946;
    color: white;
    border: none;
    padding: 10px 20px;
    font-size: 1em;
    border-radius: 5px;
    cursor: pointer;
  }
  
  .login-btn:hover {
    background: #d62839;
  }
  
  .forgot-password,
  .create-account {
    font-size: 0.9em;
    margin-top: 10px;
  }
  
  .forgot-password a,
  .create-account a {
    color: #007bff;
    text-decoration: none;
  }
  
  .forgot-password a:hover,
  .create-account a:hover {
    text-decoration: underline;
  }
  </style>
  