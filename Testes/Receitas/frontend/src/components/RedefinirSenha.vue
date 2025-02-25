<template>
    <div class="redefinir-senha-container">
      <div class="redefinir-senha-card">
        <img src="@/assets/icon.png" alt="Logo" class="logo" />
        <h2>Repôr Password</h2>
        <form @submit.prevent="handlePasswordReset">
          <label for="email">Email associado:</label>
          <input type="email" id="email" v-model="email" required />
  
          <div class="button-group">
            <button type="button" class="cancel-btn" @click="$router.push({ name: 'AdminLogin' })">Cancelar</button>
            <button type="submit" class="send-link-btn">Enviar Link</button>
          </div>
        </form>
      </div>
    </div>
  </template>
  
  <script>
  export default {
    name: "RedefinirSenha",
    data() {
      return {
        email: "",
      };
    },
    methods: {
      async handlePasswordReset() {
        try {
          const response = await fetch("http://localhost:3000/users/password-reset", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: this.email }),
          });

          if (response.ok) {
            alert("Se o email estiver associado a uma conta, um link foi enviado.");
          } else {
            const data = await response.json();
            alert(data.message || "Erro ao enviar o link.");
          }
        } catch (error) {
          console.error("Erro:", error);
          alert("Erro interno. Tente novamente mais tarde.");
        }
      },
    },
  };
  </script>
  
  <style>
  .redefinir-senha-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    background-image: url('@/assets/background.jpg'); 
    background-size: cover;
    background-repeat: no-repeat;
    background-position: center;
  }
  
  .redefinir-senha-card {
    background: white;
    padding: 30px;
    border-radius: 8px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    text-align: center;
    width: 300px;
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    align-items: center;
  }
  
  .logo {
    width: 80px;
    margin-bottom: 20px;
    display: block;
  }
  
  h2 {
    font-size: 1.2em;
    color: #333;
    margin-bottom: 20px;
    text-align: center;
  }
  
  form {
    width: 100%;
  }
  
  label {
    font-size: 0.9em;
    color: #555;
    margin-bottom: 5px;
    display: block;
  }
  
  input {
    width: 100%;
    padding: 8px;
    border: 1px solid #ccc;
    border-radius: 5px;
    margin-bottom: 15px;
  }
  
  .button-group {
    display: flex;
    justify-content: space-between;
  }
  
  .cancel-btn,
  .send-link-btn {
    background: #e63946;
    color: white;
    border: none;
    padding: 10px 20px;
    font-size: 1em;
    border-radius: 5px;
    cursor: pointer;
  }
  
  .cancel-btn:hover,
  .send-link-btn:hover {
    background: #d62839; 
  }
  </style>
  