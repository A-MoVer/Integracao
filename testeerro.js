// Código JavaScript com práticas que devem gerar Code Smells

// Usando == e != ao invés de === e !==
let a = 5;
let b = '5';
if (a == b) {  // Usando == em vez de ===
    console.log("São iguais");
}

// Acesso direto ao arguments
function exemplo() {
    console.log(arguments[0]);
}

// Uso de "arguments.callee"
function exemploComErro() {
    console.log(arguments.callee);
}

// Uso de "for" em vez de "for of" (em arrays)
let arr = [1, 2, 3];
for (let i = 0; i < arr.length; i++) {  // "for" ao invés de "for of"
    console.log(arr[i]);
}

// Definindo função dentro de loop
for (let i = 0; i < 5; i++) {
    function dentroDoLoop() { // Função definida dentro de loop
        console.log(i);
    }
    dentroDoLoop();
}

// Declaração de variável antes de seu uso
var x = 10;
console.log(x);  // x é declarado antes do seu uso

// Usando "with"
with (Math) {
    let resultado = sin(2) + cos(2);
    console.log(resultado);
}

// Declaração de variável "var" fora do escopo
var z = 5;
if (true) {
    var z = 10; // "z" é redeclarada dentro do mesmo escopo
}

// Uso do "eval" (prática ruim)
eval("console.log('Avaliado!')");

// Usando "continue" dentro de um loop sem necessidade
for (let i = 0; i < 5; i++) {
    if (i === 2) continue;  // "continue" sem razão clara
    console.log(i);
}

// Falta de uso de "default" no switch
let x = 'A';
switch (x) {
    case 'A':
        console.log("Caso A");
        break;
    case 'B':
        console.log("Caso B");
        break;
}

// Aninhamento de funções dentro de outras funções
function funcaoA() {
    function funcaoB() {
        console.log("Função B dentro de A");
    }
    funcaoB();  // Função dentro de outra
}

// Uso de múltiplos "return" no mesmo método
function minhaFuncao() {
    if (true) return "Sim";
    return "Não";  // Múltiplos returns, causando confusão
}

