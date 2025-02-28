const admin = require("firebase-admin");
const path = require("path");
//teste
if (!admin.apps.length) { // Verifica se já foi inicializado
  const serviceAccount = require(path.resolve(__dirname, "middleware/pwa-mei2-firebase-adminsdk-fbsvc-abf45ce5b6.json"));

  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
  });
} else {
  console.log("Firebase já está inicializado!");
}

module.exports = admin;
