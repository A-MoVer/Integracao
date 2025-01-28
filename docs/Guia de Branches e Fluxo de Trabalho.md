# Guia de Utilização de Branches

Este guia explica como utilizar as branches corretamente no projeto. Segue estas instruções para garantir que o trabalho em equipa é eficiente e organizado.

---

## **Branches Principais**

### 1. `main`
- **O que é?**
  - Contém o código estável e pronto para produção.
- **Regras:**
  - Não podes fazer commits diretos nesta branch.
  - Todas as alterações devem ser feitas por **Pull Requests** aprovados e passar pelos testes automáticos.

### 2. `develop`
- **O que é?**
  - Contém o código em desenvolvimento.
  - Todas as **features** e **correções** são integradas aqui antes de serem lançadas.
- **Regras:**
  - Também não podes fazer commits diretos nesta branch.
  - Usa **Pull Requests** para integrar o teu código.

---

## **Branches Temporárias**

### 1. Feature Branches
- **Para quê?**
  - Desenvolver novas funcionalidades.
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
  4. Quando terminares, faz merge para a `develop`:
     ```bash
     git checkout develop
     git merge feature/<nome-da-funcionalidade>
     ```
  5. Apaga a branch (opcional):
     ```bash
     git branch -d feature/<nome-da-funcionalidade>
     ```

---

### 2. Bugfix Branches
- **Para quê?**
  - Corrigir erros encontrados durante o desenvolvimento.
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
     git checkout -b bugfix/corrigir-botao-login
     ```
  3. Trabalha na correção e faz commits regulares:
     ```bash
     git add .
     git commit -m "Descrição da correção"
     ```
  4. Quando terminares, faz merge para a `develop`:
     ```bash
     git checkout develop
     git merge bugfix/<descricao-do-bug>
     ```
  5. Apaga a branch (opcional):
     ```bash
     git branch -d bugfix/<descricao-do-bug>
     ```

---

### 3. Release Branches
- **Para quê?**
  - Preparar uma nova versão para produção.
- **Como usar?**
  1. Certifica-te que estás na branch `develop`:
     ```bash
     git checkout develop
     ```
  2. Cria a branch:
     ```bash
     git checkout -b release/<versao>
     ```
     **Exemplo:**  
     ```bash
     git checkout -b release/1.0.0
     ```
  3. Trabalha nos ajustes finais e faz commits regulares:
     ```bash
     git add .
     git commit -m "Preparar versão 1.0.0"
     ```
  4. Faz merge para a `main`:
     ```bash
     git checkout main
     git merge release/<versao>
     git tag -a v<versao> -m "Lançamento da versão <versao>"
     git push origin v<versao>
     ```
  5. Faz merge para a `develop`:
     ```bash
     git checkout develop
     git merge release/<versao>
     ```
  6. Apaga a branch:
     ```bash
     git branch -d release/<versao>
     ```

---

### 4. Hotfix Branches
- **Para quê?**
  - Corrigir problemas críticos encontrados em produção.
- **Como usar?**
  1. Certifica-te que estás na branch `main`:
     ```bash
     git checkout main
     ```
  2. Cria a branch:
     ```bash
     git checkout -b hotfix/<descricao-do-hotfix>
     ```
     **Exemplo:**  
     ```bash
     git checkout -b hotfix/corrigir-erro-login
     ```
  3. Trabalha na correção e faz commits regulares:
     ```bash
     git add .
     git commit -m "Correção crítica para o erro de login"
     ```
  4. Faz merge para a `main`:
     ```bash
     git checkout main
     git merge hotfix/<descricao-do-hotfix>
     git tag -a v<versao> -m "Hotfix <descricao-do-hotfix>"
     git push origin v<versao>
     ```
  5. Faz merge para a `develop`:
     ```bash
     git checkout develop
     git merge hotfix/<descricao-do-hotfix>
     ```
  6. Apaga a branch:
     ```bash
     git branch -d hotfix/<descricao-do-hotfix>
     ```

---

## **Regras Gerais**
1. **Nomes das Branches**
   - Usa nomes descritivos e padronizados:
     - **Feature**: `feature/adicionar-login`
     - **Bugfix**: `bugfix/corrigir-botao`
     - **Release**: `release/1.0.0`
     - **Hotfix**: `hotfix/corrigir-erro-login`

2. **Commits**
   - Faz commits pequenos e com mensagens claras.
   - Exemplo:
     ```bash
     git commit -m "Adicionar funcionalidade de login"
     ```

3. **Pull Requests**
   - Todo o código deve ser integrado via Pull Request.
   - Pede sempre revisão de pelo menos 1 colega.

4. **Proteção das Branches**
   - Não podes fazer commits diretos nas branches `main` ou `develop`.

---

Segue este guia e mantém o repositório organizado!
