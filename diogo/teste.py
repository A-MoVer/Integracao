import os

def calcular_soma(a, b):
    resultado = a + b
    return resultado

def funcao_com_erro():
    x = 10  # Variável nunca usada
    y = "Erro"

    if y == "Erro":
        print("Isto é um erro!")  # Erro de linting: print sem uso de logging

        for i in range(5):
            print(i) # Indentação inconsistente
        
    return y

# Uso incorreto de variáveis globais
def altera_global():
    global var_global  # Codacy pode detetar que isto não é uma boa prática
    var_global = "Modificado"

funcao_com_erro()
