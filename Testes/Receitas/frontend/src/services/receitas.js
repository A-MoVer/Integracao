import axios from '../axios';

// Função para listar receitas
export const listarReceitas = async () => {
    try {
        const response = await axios.get('/receitas');
        return response.data; // Retorna a lista de receitas
    } catch (error) {
        console.error('Erro ao listar receitas:', error.response?.data || error.message);
        throw error;
    }
};

// Função para adicionar receita
export const adicionarReceita = async (novaReceita) => {
    try {
        const response = await axios.post('/receitas', novaReceita); // Envia os dados para o endpoint
        return response.data; // Retorna os dados da receita criada
    } catch (error) {
        console.error('Erro ao adicionar receita:', error.response?.data || error.message);
        throw error;
    }
};

//Fubção para obter uma receita específica
export const getReceitaById = async (id) => {
    try {
        const response = await axios.get(`/receitas/${id}`);
        return response.data;
    } catch (error) {
        console.error('Erro ao obter detalhes da receita:', error.response?.data || error.message);
        throw error;
    }
};

//Função para ir buscar um utilizador com base no seu id
export async function getUserById(id) {
    const response = await axios.get(`/users/${id}`);
    return response.data;
  }

//Dados do user atual
  export const getUserData = async () => {
    try {
      const token = localStorage.getItem('userToken');
      const response = await axios.get('/users/profile', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      return response.data;
    } catch (error) {
      console.error('Erro ao buscar dados do usuário:', error);
      throw error;
    }
  };
  
  export const updateUserProfile = async (userData) => {
    try {
      const token = localStorage.getItem('userToken');
      const response = await axios.patch('/users/profile', userData, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      return response.data;
    } catch (error) {
      console.error('Erro ao atualizar perfil:', error);
      throw error;
    }
  };

//Receitas do utilizador
export const getUserRecipes = async (chefId) => {
  try {
    const response = await axios.get(`/receitas/chef_id/${chefId}`);
    return response.data;
  } catch (error) {
    console.error('Erro ao buscar receitas:', error);
    throw error;
  }
};

