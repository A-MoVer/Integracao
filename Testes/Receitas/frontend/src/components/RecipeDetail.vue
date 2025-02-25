<template>
  <div class="recipe-detail">
    <h2>{{ recipe.title }}</h2>
    <img :src="recipe.image" alt="Recipe Image" class="recipe-image" />
    <p>{{ recipe.description }}</p>
    <p><strong>Likes:</strong> {{ recipe.likes }}</p>
  </div>
</template>

<script>
export default {
  props: ['id'],
  data() {
    return {
      recipe: null,
    };
  },
  mounted() {
    this.fetchRecipeDetail();
  },
  methods: {
    fetchRecipeDetail() {
      const recipes = [
        {
          id: 1,
          title: 'Tagliatelle',
          description: 'Uma versão autêntica da famosa receita italiana, com uma mistura cremosa de queijo e ovo.',
          image: require('@/assets/taglia.png'),
          likes: 2000,
        },
        {
          id: 2,
          title: 'Tarte de Maçã',
          description: 'Uma tarte caseira deliciosa com maçãs caramelizadas e canela.',
          image: require('@/assets/tarte.png'),
          likes: 1000,
        },
        {
          id: 3,
          title: 'Brownies',
          description: 'Brownies densos e ricos, com pedaços crocantes de nozes.',
          image: require('@/assets/brownies.png'),
          likes: 157,
        },
      ];

      this.recipe = recipes.find((recipe) => recipe.id === parseInt(this.id));
    },
  },
};
</script>

<style scoped>
.recipe-detail {
  padding: 2rem;
}

.recipe-image {
  width: 100%;
  height: auto;
  margin-top: 1rem;
}

h2 {
  color: #e74c3c;
}

p {
  font-size: 1.2rem;
  color: #555;
}
</style>
