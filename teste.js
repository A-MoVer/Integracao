// Teste de erro para o SonarQube

function exemploComErro() {
    let a = 5;
    let b = 0;

    // Divisão por zero (erro intencional)
    let resultado = a / b;

    console.log("Resultado: " + resultado);  // Isto deve gerar um "code smell" no SonarQube
}

exemploComErro();
