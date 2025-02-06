function exemploComErro() {
    let a = 5;
    let b = 1;  // Alterei b de 0 para 1 para ver o comportamento

    let resultado = a / b;

    console.log("Resultado: " + resultado);  // Verificando se o GitHub Actions está capturando as alterações
}

exemploComErro();
