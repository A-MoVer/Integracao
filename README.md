## Fluxo de Trabalho Git

O projeto segue o fluxo **GitFlow** para garantir uma colaboração organizada. Aqui está a explicação de cada branch e como trabalhar nelas:

### Branches Principais
- **main**: Contém o código estável e pronto para produção.
- **develop**: É onde ocorre o desenvolvimento ativo. O código nesta branch está sujeito a testes antes de ser integrado na `main`.

### Branches de Funcionalidade
- **feature/nome-da-feature**: Usadas para desenvolver novas funcionalidades. São criadas a partir da branch `develop` e, ao finalizar, fazem merge de volta na `develop`.

### Passos para Contribuir
1. **Criar uma nova branch para uma funcionalidade:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/nova-funcionalidade
