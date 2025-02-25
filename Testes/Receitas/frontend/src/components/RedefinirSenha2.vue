<template>
    <div>
      <h1>Redefinir Senha</h1>
      <p>Digite sua nova senha abaixo:</p>
      <form @submit.prevent="updatePassword">
        <div>
          <label for="password">Nova Senha:</label>
          <input type="password" id="password" v-model="newPassword" required />
        </div>
        <button type="submit">Redefinir Senha</button>
      </form>
      <p v-if="message">{{ message }}</p>
    </div>
  </template>
  
  <script>
  import { ref } from 'vue';
  import axios from 'axios';
  
  export default {
    name: 'RedefinirSenha2',
    setup() {
      const newPassword = ref('');
      const message = ref('');
      const email = new URLSearchParams(window.location.search).get('email');
  
      const updatePassword = async () => {
        try {
          const response = await axios.post('http://localhost:3000/users/update-password', {
            email,
            newPassword: newPassword.value,
          });
          message.value = response.data.message;
        } catch (error) {
          message.value = 'Erro ao atualizar senha.';
          console.error(error);
        }
      };
  
      return {
        newPassword,
        message,
        updatePassword,
      };
    },
  };
  </script>
  