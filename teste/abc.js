var userInput = "'; DROP TABLE users; --"; // 🚨 Simulação de SQL Injection

// Função com risco de SQL Injection
function getUserData(username) {
    var query = "SELECT * FROM users WHERE username = '" + username + "'"; // ❌ Concatenar strings é inseguro
    console.log("Executando query: " + query);
    return query;
}

// Uso de eval (muito perigoso e não recomendado)
function executeUserCode(code) {
    eval(code); // ❌ Nunca usar eval com input não confiável
}

// Variáveis globais sem escopo adequado
for (i = 0; i < 10; i++) { // ❌ 'i' não foi declarado corretamente
    console.log(i);
}

getUserData(userInput);
executeUserCode("console.log('Código inseguro executado!');");
