# Guia de Utilização de Branches

Este guia explica como utilizar as branches no projeto de forma simples e eficiente. Segue estas instruções para manter o fluxo de trabalho organizado.

---

## **Estrutura de Branches**

### **Branches Principais**
1. **`main`**
   - Contém o código **estável e em produção**.
   - **Regras:**
     - Não podes fazer commits diretos.
     - Apenas código aprovado e testado chega aqui.
     - As versões são marcadas com **tags** (ex.: `v1.0.0`).

2. **`develop`**
   - Contém o código **em desenvolvimento contínuo**.
   - **Regras:**
     - Apenas funcionalidades e correções aprovadas na branch `testes` podem ser integradas.
     - Não podes fazer commits diretos.

3. **`testes`**
   - Contém o código **em validação**.
   - **Regras:**
     - Apenas funcionalidades e correções validadas passam para a branch `develop`.

---

## **Branches Temporárias**

### **Feature Branches**
- **Para quê?**  
  Desenvolver novas funcionalidades.
- **Como usar?**
  1. Certifica-te que estás na branch `develop`:
     ```bash
     git checkout develop
     ```
  2. Cria a branch:
     ```bash
     git checkout -b feature/<nome-da-funcionalidade>
     ```
     **Exemplo:**  
     ```bash
     git checkout -b feature/adicionar-login
     ```
  3. Trabalha na funcionalidade e faz commits regulares:
     ```bash
     git add .
     git commit -m "Descrição da funcionalidade"
     ```
  4. Faz o merge para a `testes`:
     ```bash
     git checkout testes
     git merge feature/<nome-da-funcionalidade>
     git branch -d feature/<nome-da-funcionalidade>
     ```

---

### **Bugfix Branches**
- **Para quê?**  
  Corrigir erros encontrados durante o desenvolvimento.
- **Como usar?**
  1. Certifica-te que estás na branch `develop`:
     ```bash
     git checkout develop
     ```
  2. Cria a branch:
     ```bash
     git checkout -b bugfix/<descricao-do-bug>
     ```
     **Exemplo:**  
     ```bash
     git checkout -b bugfix/corrigir-botao
     ```
  3. Trabalha na correção e faz commits regulares:
     ```bash
     git add .
     git commit -m "Descrição da correção"
     ```
  4. Faz o merge para a `testes`:
     ```bash
     git checkout testes
     git merge bugfix/<descricao-do-bug>
     git branch -d bugfix/<descricao-do-bug>
     ```
---

## **Regras Importantes**

1. **Nomes das Branches**:
   - Usa nomes claros e descritivos:
     - **Feature**: `feature/adicionar-login`
     - **Bugfix**: `bugfix/corrigir-botao`

2. **Commits**:
   - Faz commits pequenos e claros.
   - Exemplo:
     ```bash
     git commit -m "Adicionar funcionalidade de login"
     ```

---

Segue este guia para garantir um fluxo de trabalho organizado e eficiente.
