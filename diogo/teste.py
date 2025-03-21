import os  # 🚨 Import não utilizado (vai gerar erro no Codacy)

def calcular_soma(a, b):
    resultado = a + b  # 🚨 Variável declarada mas nunca utilizada
    return resultadooo  # 🚨 Nome de variável incorreto (erro de sintaxe)

# 🚨 Função com más práticas de formatação e erros de linting
def funcao_com_erro():
 y = "Erro"  # 🚨 Má indentação (erro de estilo)

 if y == "Erro":
        print("Isto é um erro!")  # 🚨 Uso de print em vez de logging

        for i in range(5):
         print(i) # 🚨 Indentação inconsistente
        
 return y

# 🚨 Definir uma variável global incorretamente antes da função
var_global = ""


def altera_global():
    global var_global
    var_global = "Modificado"

# 🚨 Criar uma variável sem uso
valor_inutil = "Não estou a ser usado"

# 🚨 Código morto (nunca será executado)
if False:
    print("Isto nunca será impresso!")

# 🚨 Função recursiva sem condição de paragem (pode causar erro de desempenho)
def recursao_infinita():
    return recursao_infinita()


funcao_com_erro()
recursao_infinita()
