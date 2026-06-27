# FCG Usuários

Microsserviço responsável pelo cadastro, autenticação e autorização de usuários da plataforma FIAP Cloud Games.

## Responsabilidades

- Cadastro de novos usuários
- Autenticação via JWT
- Autorização por roles (Usuario, Admin)
- Publicação do evento de usuário criado

## Eventos publicados

| Evento | Fila | Quando |
|--------|------|--------|
| `UserRegisteredEvent` | `user.created` | Novo usuário cadastrado |

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/usuarios/registrar` | Cadastra novo usuário |
| POST | `/api/usuarios/login` | Autentica e retorna JWT |
| GET | `/api/usuarios/perfil` | Retorna dados do usuário autenticado |
| PUT | `/api/usuarios/alterar-senha` | Altera senha do usuário |

## Variáveis de ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente da aplicação | `Production` |
| `ConnectionStrings__DefaultConnection` | Connection string do SQL Server | - |
| `JwtSettings__Secret` | Chave secreta para geração do JWT | - |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | Porta do RabbitMQ | `5672` |
| `RabbitMQ__Usuario` | Usuário do RabbitMQ | `guest` |
| `RabbitMQ__Senha` | Senha do RabbitMQ | `guest` |

## Como rodar localmente

```bash
dotnet run --project FCG.Presentation
```

## Como rodar com Docker

```bash
docker-compose up
```

## Deploy no Kubernetes

```bash
kubectl apply -f k8s/
kubectl port-forward service/fcg-usuarios-api 5101:80
```

Swagger disponível em `http://localhost:5101/swagger`

> ⚠️ As credenciais neste repositório são exclusivas para ambiente de desenvolvimento local.