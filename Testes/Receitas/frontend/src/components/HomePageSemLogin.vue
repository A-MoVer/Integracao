<template>
  <div>
    <header class="header">
      <div class="logo">
        <img src="@/assets/icon.png" alt="Logo" class="logo-img" />
        <h1>EuCozinho</h1>
      </div>
      <nav class="nav">
        <ul>
          <nav class="nav">
        <ul>
          <li><router-link to="/login">Receitas</router-link></li>
          <li><router-link to="/login">Chefes</router-link></li>
          <li><router-link to="/login">Entrar</router-link></li>
        </ul>
</nav>

        </ul>
      </nav>
    </header>

    <main>
      <h2>Receitas</h2>
      <div class="receitas">
        <div class="card" v-for="receita in receitas" :key="receita.id">
          <img :src="receita.imagem" :alt="receita.nome" class="card-img" />
          <h3>{{ receita.nome }}</h3>
          <p>{{ receita.descricao }}</p>
        </div>
      </div>
    </main>
  </div>
</template>

<script>
export default {
  name: "HomePageSemLogin",
  data() {
    return {
      receitas: [
        {
          id: 1,
          nome: "Cozido à Portuguesa",
          descricao: "Cozido à portuguesa é um cozido tradicional português.",
          imagem: require('@/assets/cozidoaportuguesa.jpeg'),
        },
        {
          id: 2,
          nome: "Paella",
          descricao: "Um dos pratos mais famosos da Espanha, originário da região de Valência.",
          imagem: require('@/assets/Paella.jpg'),
        },
        {
          id: 3,
          nome: "Risotto",
          descricao: "O risotto é um prato típico italiano.",
          imagem: require('@/assets/Risotto.jpg'),
        },
      ],
    };
  },
};
</script>

<style>
body {
  font-family: Arial, sans-serif;
  margin: 0;
  padding: 0;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background-color: #f8f9fa;
  border-bottom: 1px solid #ddd;
}

.logo {
  display: flex;
  align-items: center;
}

.logo-img {
  width: 50px;
  height: 50px;
  margin-right: 10px;
}

.nav ul {
  display: flex;
  list-style: none;
  margin: 0;
  padding: 0;
}

.nav li {
  margin-right: 20px;
}

.nav a {
  text-decoration: none;
  color: #007bff;
}

main {
  padding: 20px;
  text-align: center;
}

.receitas {
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
}

.card {
  width: 300px;
  margin: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
  overflow: hidden;
  background-color: #fff;
  text-align: center;
}

.card-img {
  width: 100%;
  height: auto;
}

.card h3 {
  margin: 10px 0;
  font-size: 1.2em;
}

.card p {
  padding: 0 10px 10px;
  font-size: 0.9em;
  color: #666;
}
</style>
