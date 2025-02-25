import axios from 'axios';

const instance = axios.create({
    baseURL: 'http://localhost:3000', // URL base do backend
});

export default instance;
