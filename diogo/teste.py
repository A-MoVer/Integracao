def calcular_soma(a, b):
    resultado = a + b
    return resultado

def funcao_com_erro():
    y = "Erro"

    if y == "Erro":
        print("Isto é um erro!")  # Erro de linting: print sem uso de logging

        for i in range(5):
            print(i) # Indentação inconsistente
        
    return y

# Definir a variável global corretamente antes da função
var_global = ""

def altera_global():
    global var_global
    var_global = "Modificado"


funcao_com_erro()
