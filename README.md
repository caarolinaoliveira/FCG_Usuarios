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

| Variável | Obrigatória | Descrição | Padrão |
|----------|-------------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | não | Ambiente da aplicação | `Production` |
| `ConnectionStrings__DefaultConnection` | **sim** | Connection string do SQL Server | - |
| `JwtSettings__Segredo` | **sim** | Chave de assinatura do JWT (mínimo 32 caracteres) | - |
| `RabbitMQ__Usuario` | **sim** | Usuário do RabbitMQ | - |
| `RabbitMQ__Senha` | **sim** | Senha do RabbitMQ | - |
| `RabbitMQ__Host` | não | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | não | Porta do RabbitMQ | `5672` |
| `NEW_RELIC_LICENSE_KEY` | em produção | Chave de licença da New Relic | - |

Não há credenciais no código nem nos `appsettings.json`. Se uma variável obrigatória faltar, a API não inicia e informa qual chave está faltando. Em produção (AKS) os valores vêm do Azure Key Vault, injetados como variáveis de ambiente.

## Como rodar localmente

### 1. Infraestrutura (SQL Server, RabbitMQ, MongoDB e Redis)

```bash
cp .env.example .env     # edite o .env com os seus valores (o .env não é versionado)
docker compose up -d
```

> ⚠️ Este `docker-compose.yml` serve para desenvolver **somente o Usuários**. Ele **não deve rodar junto** com o compose do `FCG_Orquestracao`: os dois usam os mesmos nomes de container (`fcg-sqlserver`, `fcg-rabbitmq`, `fcg-mongodb`, `fcg-redis`) e as mesmas portas (1433, 5672, 15672, 27017, 6379). Rode um ou outro.

O MongoDB guarda o log de erros e o Redis guarda o cache de roles do login.

### 2. Segredos da API (dotnet user-secrets)

Os valores abaixo são **exemplos falsos**. Use os mesmos usuário/senha que você definiu no `.env`.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FCG_Usuarios;User Id=sa;Password=<SA_PASSWORD do .env>;TrustServerCertificate=True;" --project FCG.Presentation
dotnet user-secrets set "RabbitMQ:Usuario" "troque_este_usuario" --project FCG.Presentation
dotnet user-secrets set "RabbitMQ:Senha" "TroqueEstaSenha#2" --project FCG.Presentation
dotnet user-secrets set "JwtSettings:Segredo" "troque-esta-chave-com-no-minimo-32-caracteres" --project FCG.Presentation
```

### 3. Executar

```bash
dotnet run --project FCG.Presentation
```

## Como rodar com Docker

```bash
docker compose up -d     # sobe SQL Server, RabbitMQ, MongoDB e Redis usando as variáveis do .env
```

## Deploy no Kubernetes

```bash
kubectl apply -f k8s/
kubectl port-forward service/fcg-usuarios-api 5101:80
```

Swagger disponível em `http://localhost:5101/swagger`

> ⚠️ Este repositório não contém credenciais. Os exemplos acima usam valores falsos; nunca versione o `.env` nem os seus segredos reais.