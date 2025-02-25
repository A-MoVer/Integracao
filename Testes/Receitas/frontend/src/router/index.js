import { createRouter, createWebHistory } from 'vue-router';
import Home from '@/components/HomePage.vue'; // Página inicial
import RecipeDetail from '@/components/RecipeDetail.vue'; // Página de detalhes da receita
import ChefDetail from '@/components/ChefDetail.vue'; // Página de detalhes do chefe
import ChefDetails from '@/components/ChefDetails.vue'; // Página de detalhes do chefe (nova página)
import HomePageSemLogin from '@/components/HomePageSemLogin.vue'; // Página para utilizadores não autenticados
import ProfilePage from '@/components/ProfilePage.vue';
import EditProfilePage from '@/components/EditProfilePage.vue';
import AddRecipe from '@/components/AddRecipe.vue'; // Adiciona o componente de adicionar receitas
import LoginPage from '@/components/LoginPage.vue'; // Página de login
import AdminLoginPage from '@/components/AdminLoginPage.vue'; // Página de login do administrador
import CreateAdminAccount from '@/components/CreateAdminAccount.vue'; // Criação de conta
import ReceitasDetalhe from '@/components/ReceitasDetalhe.vue';
import RedefinirSenha from "@/components/RedefinirSenha.vue";
import RedefinirSenha2 from '@/components/RedefinirSenha2.vue';


const routes = [
  { path: '/', name: 'HomePageSemLogin', component: HomePageSemLogin }, // Página inicial para utilizadores não autenticados
  { path: '/home', name: 'Home', component: Home }, // Página inicial para utilizadores autenticados
  { path: '/recipe/:id', name: 'RecipeDetail', component: RecipeDetail, props: true },
  { path: '/chefs/:id', name: 'ChefDetail', component: ChefDetail, props: true },
  { path: '/chef-details/:id', name: 'ChefDetails', component: ChefDetails, props: true }, // Nova rota para detalhes do chef
  { path: '/profile', name: 'ProfilePage', component: ProfilePage, props: true },
  { path: '/edit-profile', name: 'EditProfile', component: EditProfilePage, props: true },
  { path: '/add-recipe', name: 'AddRecipe', component: AddRecipe }, // Rota para adicionar receitas
  { path: '/login', name: 'LoginPage', component: LoginPage }, // Rota para a página de login
  { path: '/admin-login', name: 'AdminLogin', component: AdminLoginPage }, //Tela de fazer login
  { path: '/criar-conta', name: 'CriarConta', component: CreateAdminAccount}, //Tela de criar conta
  { path: '/recipe/:id', name: 'RecipeDetail', component: ReceitasDetalhe, props: true },  //Tela de detalhes de receita
  { path: "/redefinir-senha", name: "RedefinirSenha", component: RedefinirSenha },
  { path: "/redefinir-senha2", name: "RedefinirSenha2", component: RedefinirSenha2 },
  
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
