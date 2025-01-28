# Guia de Utilização de Branches

Este guia explica como utilizar as branches no projeto de forma simples e eficiente. Segue estas instruções para manter o fluxo de trabalho organizado.

---

## **Estrutura de Branches**

### **Branches Principais**
1. **`main`**
   - Contém o código **estável e em produção**.
   - **Regras:**
     - Não podes fazer commits diretos.
     - Apenas alterações aprovadas (via Pull Request) são mergeadas aqui.
     - As versões são marcadas com **tags** (ex.: `v1.0.0`).

2. **`develop`**
   - Contém o código **em desenvolvimento**.
   - **Regras:**
     - Todas as novas funcionalidades e correções são integradas aqui.
     - Não podes fazer commits diretos.

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
  4. Faz o merge para a `develop`:
     ```bash
     git checkout develop
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
  4. Faz o merge para a `develop`:
     ```bash
     git checkout develop
     git merge bugfix/<descricao-do-bug>
     git branch -d bugfix/<descricao-do-bug>
     ```

---

## **Gestão de Versões**

1. Quando o código em `develop` estiver pronto para produção:
   - Faz o merge para o `main`:
     ```bash
     git checkout main
     git merge develop
     ```
2. Marca a versão com uma **tag**:
   ```bash
   git tag -a v<versao> -m "Descrição da versão"
   git push origin v<versao>
