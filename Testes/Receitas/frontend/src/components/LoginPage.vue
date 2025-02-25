<template>
  <div class="login-container">
    <div class="login-card">
      <img src="@/assets/icon.png" alt="Logo" class="logo" />
      <h2>Entrar com serviços externos</h2>
      <button class="login-btn google-login" @click="loginWithGoogle">
        Entrar com Google
      </button>
      <p class="admin-link">
        É Administrador?
        <a @click="$router.push('/admin-login')" style="cursor: pointer;">
          Fazer login como Admin
        </a>
      </p>
    </div>
  </div>
</template>

<script>
import { auth } from "@/firebase";
import { GoogleAuthProvider, signInWithPopup } from "firebase/auth";
import axios from "axios";

export default {
  name: "LoginPage",
  methods: {
    async loginWithGoogle() {
  try {
    const provider = new GoogleAuthProvider();
    const result = await signInWithPopup(auth, provider);
    const user = result.user;

    // Obter o token do utilizador autenticado
    const token = await user.getIdToken();
    console.log("Token gerado durante o login:", token);

    // Guardar o token no localStorage para reutilização
    localStorage.setItem("userToken", token);

    // Redirecionar após login (ou outra lógica necessária)
    this.$router.push("/home");
  } catch (error) {
    console.error("Erro ao fazer login:", error);
    alert("Não foi possível efetuar o login. Tente novamente.");
  }
}

  },
};
</script>

<style>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-image: url("@/assets/background.jpg");
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
  height: 80px;
  margin-bottom: 20px;
}

.google-login {
  background: #4285f4;
  color: white;
  border: none;
  padding: 10px 20px;
  font-size: 1em;
  border-radius: 5px;
  cursor: pointer;
  margin-bottom: 20px;
}

.google-login:hover {
  background: #357ae8;
}

.admin-link {
  font-size: 0.9em;
}

.admin-link a {
  color: #007bff;
  text-decoration: none;
}

.admin-link a:hover {
  text-decoration: underline;
}
</style>
