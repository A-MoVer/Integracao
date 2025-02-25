import { initializeApp } from "firebase/app";
import { getAuth } from "firebase/auth";

// Configuração do Firebase Client SDK
const firebaseConfig = {
  apiKey: "AIzaSyCaL2t5saaFbImgHpgHkdB9HmAw8Xypr5o",
  authDomain: "pwa-mei2.firebaseapp.com",
  projectId: "pwa-mei2",
  storageBucket: "pwa-mei2.appspot.com",
  messagingSenderId: "260331841641",
  appId: "1:260331841641:web:3904c49cc5a9f24b619af8",
  measurementId: "G-PXG6QS3WNL",
};

// Inicializa a aplicação Firebase
const app = initializeApp(firebaseConfig);

// Exporta o serviço de autenticação
const auth = getAuth(app);
export { auth };
