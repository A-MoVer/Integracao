// Teste para SonarQube

// Função que gera um erro de divisão por zero
function dividirPorZero() {
    let a = 5;
    let b = 0;  // Divisão por zero
    let resultado = a / b; // Aqui o SonarQube pode identificar um erro de "Code Smell"
    console.log("Resultado da divisão: " + resultado); 
}

// Função que usa uma variável não utilizada
function usarVariavelNaoUtilizada() {
    let y = 10;  // A variável x não é utilizada
}

// Função que usa um número mágico
function calcularImposto(valor) {
    let imposto = valor * 0.15; // 0.15 é um número mágico
    console.log("Imposto a pagar: " + imposto);
}

// Chamada das funções
dividirPorZero();
usarVariavelNaoUtilizada();
calcularImposto(100);
