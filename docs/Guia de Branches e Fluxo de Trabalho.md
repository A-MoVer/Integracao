# Fluxo de Trabalho com Branches no Git

Este documento explica como usar as branches no projeto. Lê com atenção para seguires corretamente as práticas do fluxo de trabalho.

---

## Branches Principais

1. **`main`**
   - Contém o código **estável e pronto para produção**.
   - Não podes fazer commits diretos nesta branch.
   - Tudo o que vai para aqui deve ser aprovado por Pull Request e passar nos testes automáticos.

2. **`develop`**
   - Contém o código em desenvolvimento.
   - Todas as **features** e **correções** são integradas aqui antes de serem lançadas.
   - Também não podes fazer commits diretos nesta branch.

---

## Branches Temporárias

Estas são criadas para tarefas específicas e depois apagadas.

### **1. Feature Branches**
- Para **novas funcionalidades**.
- Nome: `feature/<nome-da-funcionalidade>`.
- Criar a partir de `develop`:
  ```bash
  git checkout develop
  git checkout -b feature/<nome-da-funcionalidade>
