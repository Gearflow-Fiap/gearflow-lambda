# gearflow-lambda

Repositório da **Fase 3 (Pós-Graduação FIAP / GearFlow)** responsável pelo fluxo de autenticação em **AWS Lambda + API Gateway**, provisionado com **Terraform**.

Este é o **Repo 1** da divisão da arquitetura. A lógica que hoje vive no monorepo legado ([gearflow-legado](https://github.com/Gearflow-Fiap/gearflow-legado)) — especialmente `AuthController` e os use cases de login — será extraída e redistribuída em **três funções serverless independentes**.

> **Status atual:** estrutura do projeto criada (3 Lambdas .NET 8, `shared/db`, Terraform e script de publish). A integração real com o banco (Repo 3) ainda usa stub.

---

## Objetivo

Quebrar o login monolítico do legado em steps pequenos, cada um deployável e escalável isoladamente:

1. **Validar CPF** (regra de domínio, sem banco)
2. **Checar cliente** (consulta readonly no banco)
3. **Gerar token JWT** (assinatura e retorno do access token)

Isso substitui, no novo desenho, a responsabilidade concentrada em:

- `GearFlow.Api/Controllers/AuthController.cs`
- `GearFlow.Application/UseCases/Auth/LoginUser/LoginUserUseCase.cs`

---

## Arquitetura das functions

Cada pasta em `functions/` é **uma Lambda própria**: projeto/handler próprio, pacote próprio, rota própria no API Gateway.

```
Cliente / Front / BFF
        │
        ▼
   API Gateway
        │
        ├── POST /auth/validate-cpf   → Lambda validate-cpf
        ├── POST /auth/check-client   → Lambda check-client
        └── POST /auth/generate-token → Lambda generate-token
```

### Por que não uma Lambda só?

O enunciado da Fase 3 pede **três functions**, não um único `FunctionHandler` chamando tudo. Cada step tem responsabilidade única:

| Lambda | Responsabilidade | Precisa de banco? |
|---|---|---|
| `validate-cpf` | Validar dígitos verificadores do CPF | Não |
| `check-client` | Consultar existência/status do cliente | Sim (readonly) |
| `generate-token` | Assinar e devolver JWT | Não (só config/secret) |

### O que é um handler?

Na AWS, “function” é a unidade deployável. Em **.NET**, o ponto de entrada costuma ser uma classe `Function` com um método `FunctionHandler` que recebe o evento (ex.: request do API Gateway) e devolve a resposta HTTP/JSON.

Há **um `Function` + `FunctionHandler` por Lambda** (três vezes no total), não um handler único orquestrando as três.

---

## Estrutura do repositório

```text
gearflow-lambda/
├── GearFlow.Lambda.slnx
├── functions/
│   ├── validate-cpf/          # Lambda 1 – validação de CPF (CpfValidator + Function)
│   ├── check-client/          # Lambda 2 – consulta de cliente (usa shared/db)
│   └── generate-token/        # Lambda 3 – geração de JWT
├── shared/
│   └── db/                    # IClientReadRepository + factory readonly (stub)
├── events/                    # Payloads de exemplo (API Gateway HTTP API v2)
├── scripts/
│   ├── publish-lambdas.ps1    # Gera artifacts/*.zip (local / Windows)
│   └── publish-lambdas.sh     # Gera artifacts/*.zip (CI / Linux)
├── terraform/
│   ├── functions.tf           # Lambdas, IAM, API Gateway, rotas
│   ├── versions.tf            # Backend Terraform Cloud (workspace gearflow-lambda)
│   ├── variables.tf
│   ├── outputs.tf
│   └── terraform.tfvars.example
├── .github/
│   ├── dependabot.yml
│   └── workflows/
│       ├── ci.yml                 # Build + security (.NET)
│       ├── codeql.yml             # SAST C#
│       ├── terraform-plan.yml     # Plan no PR
│       └── terraform-apply.yml    # Apply no merge em main
└── README.md
```

---

## O que migrar do monorepo legado

Fonte: [Gearflow-Fiap/gearflow-legado](https://github.com/Gearflow-Fiap/gearflow-legado) (branch `develop`).

| Arquivo atual (legado) | Destino no Repo 1 |
|---|---|
| `GearFlow.Domain/Entities/ClientAggregate/Cpf.cs` | Base da lógica da **validate-cpf** |
| `GearFlow.Domain/Utils/` (`IsValidCpf()`) | Lógica central da **validate-cpf** |
| `GearFlow.Application/UseCases/Auth/LoginUser/LoginUserUseCase.cs` | Dividida entre **check-client** e **generate-token** |
| `GearFlow.Application/Settings/JwtSettings.cs` | Configuração da **generate-token** |
| `GearFlow.Api/Configurations/ConfigureJwtExtensions.cs` | Parâmetros de assinatura do JWT (**generate-token**) |

### Detalhamento por function

#### 1. `validate-cpf`

**Origem**

- `Cpf.cs` — value object que rejeita CPF inválido
- `Utils.IsValidCpf()` — algoritmo (11 dígitos, rejeita sequência repetida, calcula dígitos)

**Comportamento esperado**

- Entrada: CPF (string, com ou sem máscara)
- Processamento: normalizar números + validar dígitos
- Saída: `{ "valid": true/false }` (ou erro 400 quando inválido, conforme contrato definido na implementação)

**Não faz:** consulta a banco, login, geração de token.

#### 2. `check-client`

**Origem (parte do `LoginUserUseCase`)**

- Buscar o registro (no legado: usuário por login normalizado)
- Verificar se pode autenticar (existe, ativo, não bloqueado, etc.)

**Comportamento esperado**

- Entrada: identificador do cliente (ex.: CPF já validado no step anterior)
- Processamento: consulta **readonly** via `shared/db` (banco do Repo 3)
- Saída: dados mínimos do cliente + status (encontrado / inativo / bloqueado / não encontrado)

**Não faz:** validar dígitos de CPF (isso é da Lambda 1) nem assinar JWT (Lambda 3).

#### 3. `generate-token`

**Origem**

- Trecho do login que chama `AuthTokenHelper.GenerateTokenBundle`
- `JwtSettings` — `Issuer`, `Audience`, `SigningKey`, tempos de expiração
- `ConfigureJwtExtensions` — parâmetros de assinatura (HMAC SHA-256, issuer, audience, key)

**Comportamento esperado**

- Entrada: dados do cliente/usuário já checado
- Processamento: montar claims + assinar JWT com a chave configurada
- Saída: access token (e, se o escopo da Fase 3 incluir, refresh token)

**Não faz:** validar CPF nem consultar cliente no banco.

---

## Fluxo de autenticação (visão de uso)

Fluxo típico orquestrado pelo cliente, BFF ou chamadas sequenciais:

```text
1) POST /auth/validate-cpf
      body: { "cpf": "123.456.789-09" }
      → 200 se válido / 400 se inválido

2) POST /auth/check-client
      body: { "cpf": "12345678909" }
      → 200 com dados/status do cliente
      → 404 se não existir

3) POST /auth/generate-token
      body: { "clientId": "...", ... }
      → 200 com { "accessToken": "...", "expiresAt": "..." }
```

As rotas exatas podem ser ajustadas no Terraform; o importante é manter **uma rota → uma Lambda**.

---

## Relação com os outros repositórios

| Repo | Papel |
|---|---|
| **Repo 1 – `gearflow-lambda` (este)** | Functions de auth + Terraform das Lambdas/API Gateway |
| **Repo 3 – banco / dados** | Fonte de verdade do cliente; `shared/db` só lê |
| **legado (`gearflow-legado`)** | Monólito de referência; código a migrar, não a executar aqui |

Este repositório **não** sobe a API ASP.NET completa do legado. Ele publica apenas as Lambdas de auth.

---

## Stack prevista

| Item | Tecnologia |
|---|---|
| Runtime das Lambdas | .NET (AWS Lambda) — alinhado ao legado C# |
| Entrada HTTP | Amazon API Gateway |
| IaC | Terraform (`terraform/functions.tf`) |
| Banco (check-client) | Conexão readonly (Repo 3) |
| Auth token | JWT (HMAC SHA-256), configs espelhando `JwtSettings` |

> Se o padrão oficial do curso/time definir outro runtime (Node/Python), a responsabilidade das três functions permanece a mesma; só muda a linguagem do handler.

---

## Pré-requisitos (quando for implementar / rodar)

- Conta AWS com permissões para Lambda, API Gateway, IAM (e VPC/Secrets se necessário)
- [AWS CLI](https://aws.amazon.com/cli/) configurado (`aws configure` ou variáveis de ambiente)
- [Terraform](https://developer.hashicorp.com/terraform) instalado
- [.NET SDK](https://dotnet.microsoft.com/) (versão alinhada ao template Lambda escolhido)
- Ferramentas úteis:
  - Amazon.Lambda.Tools / template AWS Lambda para .NET
  - (opcional) AWS SAM para invoke local

---

## Como desenvolver

```bash
# Restaurar e compilar
dotnet restore GearFlow.Lambda.slnx
dotnet build GearFlow.Lambda.slnx -c Release
```

Handlers:

| Projeto | Handler |
|---|---|
| `GearFlow.Lambda.ValidateCpf` | `GearFlow.Lambda.ValidateCpf::GearFlow.Lambda.ValidateCpf.Function::FunctionHandler` |
| `GearFlow.Lambda.CheckClient` | `GearFlow.Lambda.CheckClient::GearFlow.Lambda.CheckClient.Function::FunctionHandler` |
| `GearFlow.Lambda.GenerateToken` | `GearFlow.Lambda.GenerateToken::GearFlow.Lambda.GenerateToken.Function::FunctionHandler` |

Próximos passos de implementação:

- Trocar `StubClientReadRepository` pela query readonly real (Repo 3)
- Ajustar claims do JWT se o contrato do front exigir `security_stamp` / refresh token
- (Opcional) VPC/SG no Terraform se o banco não for acessível publicamente

---

## Como rodar localmente (quando o código existir)

### Opção A — invocar o handler unitariamente

Chamar o `FunctionHandler` com um evento JSON fake (útil em testes):

```json
{
  "body": "{\"cpf\":\"529.982.247-25\"}"
}
```

### Opção B — AWS SAM (simular API Gateway)

```bash
sam build
sam local invoke ValidateCpfFunction -e events/validate-cpf.json
sam local start-api
```

### Opção C — testes automatizados

Cobrir pelo menos:

- CPF válido / inválido / mascarado / só dígitos
- Cliente existente / inexistente / inativo
- Token gerado com issuer/audience/expiração corretos

---

## CI/CD

Esteira alinhada ao `gearflow-app` (CI .NET) e ao `gearflow-infra-k8s` (Terraform plan/apply).

| Workflow | Dispara em | O que faz |
|---|---|---|
| `ci.yml` | push/PR em `main`,`develop` | Build Release + `dotnet test` + gate de deps vulneráveis |
| `codeql.yml` | push/PR em `main`,`develop` + semanal | SAST C# (CodeQL) |
| `terraform-plan.yml` | PR em `main` (paths de código/infra) | Publica zips → init/validate/plan → comenta no PR |
| `terraform-apply.yml` | push em `main` (mesmos paths) | Publica zips → apply (`environment: production`) |
| `dependabot.yml` | agendado | PRs de NuGet + GitHub Actions |

```
PR → main
  ├─ CI (build + security)
  ├─ CodeQL
  └─ Terraform Plan  (publish zips → plan)

Merge → main
  ├─ CI + CodeQL
  └─ Terraform Apply (publish zips → apply)
```

### Secrets e variables (GitHub Actions)

Igual ao `gearflow-infra-k8s`: o **plan não configura AWS no runner** — só o **apply** usa `configure-aws-credentials`.

Configure em **Settings → Secrets and variables → Actions**. Crie também o environment **`production`** (usado pelo apply).

**Secrets**

| Nome | Uso |
|---|---|
| `TF_API_TOKEN` | Terraform Cloud (workspace `gearflow-lambda`) |
| `AWS_ACCESS_KEY_ID` | Credencial AWS no **apply** |
| `AWS_SECRET_ACCESS_KEY` | Credencial AWS no **apply** |
| `JWT_SIGNING_KEY` | `TF_VAR_jwt_signing_key` (Lambda generate-token) |
| `DB_CONNECTION_STRING` | `TF_VAR_db_connection_string` (Lambda check-client; pode vazio enquanto stub) |

**Variables**

| Nome | Exemplo |
|---|---|
| `AWS_REGION` | `us-east-1` |

> `The security token included in the request is invalid` = Access Key / Secret errados ou expirados. Se a conta for **AWS Academy**, a key é temporária: no HCP (e no `aws configure` local) use também `AWS_SESSION_TOKEN` renovado a cada lab.

Após o primeiro apply, copie os outputs `validate_cpf_url`, `check_client_url` e `generate_token_url` para as variables `LAMBDA_*_URL` do `gearflow-infra-k8s`.

### Pré-requisito Terraform Cloud

Workspace `gearflow-lambda` na organização de `terraform/versions.tf`.

Como o **plan** espelha o k8s (sem AWS no GitHub Actions), se o Execution Mode for **Remote** configure no workspace HCP (*Variables → Environment variables*, sensíveis):

| Env var no HCP | Valor |
|---|---|
| `AWS_ACCESS_KEY_ID` | mesma key AWS |
| `AWS_SECRET_ACCESS_KEY` | mesmo secret |
| `AWS_SESSION_TOKEN` | se a credencial for temporária |

Sem isso o plan remoto falha com `No valid credential sources found`.

---

## Como publicar na AWS

### Via CI/CD (fluxo normal)

1. Abrir PR para `main` → `ci.yml` + `codeql.yml` + `terraform-plan.yml`
2. Após aprovação e merge → `terraform-apply.yml` aplica as Lambdas/API Gateway

### Manual (local)

```powershell
# 1. Empacotar as 3 functions (gera artifacts/*.zip)
.\scripts\publish-lambdas.ps1
# Linux/CI: bash scripts/publish-lambdas.sh

# 2. Configurar variáveis
cd terraform
copy terraform.tfvars.example terraform.tfvars
# edite terraform.tfvars (jwt_signing_key, db_connection_string, etc.)

# 3. Infra
terraform init
terraform plan
terraform apply
```

Depois do apply:

1. Pegar a URL do API Gateway na output do Terraform
2. Testar com curl/Postman na ordem: validate-cpf → check-client → generate-token
3. Validar o JWT (claims, assinatura, expiração)

### Variáveis / secrets esperados

| Nome (exemplo) | Usado por | Descrição |
|---|---|---|
| `JWT_SIGNING_KEY` | `generate-token` | Chave HMAC (equivalente a `Jwt:SigningKey`) |
| `JWT_ISSUER` | `generate-token` | Issuer (default: `GearFlow.Api`) |
| `JWT_AUDIENCE` | `generate-token` | Audience (default: `GearFlow.Client`) |
| `JWT_ACCESS_TOKEN_MINUTES` | `generate-token` | Tempo de vida do access token |
| `DB_CONNECTION_STRING` | `check-client` | Connection string **readonly** (Repo 3) |

Secrets não devem ir commitados no repositório; usar GitHub Secrets, AWS Secrets Manager ou Terraform variables sensíveis.

---

## Contratos sugeridos (rascunho)

Serão fechados na implementação; abaixo um ponto de partida.

### `POST /auth/validate-cpf`

**Request**

```json
{ "cpf": "529.982.247-25" }
```

**Response 200**

```json
{ "valid": true, "cpf": "52998224725" }
```

**Response 400**

```json
{ "valid": false, "message": "CPF inválido." }
```

### `POST /auth/check-client`

**Request**

```json
{ "cpf": "52998224725" }
```

**Response 200**

```json
{
  "found": true,
  "clientId": "uuid-ou-id",
  "status": "active"
}
```

### `POST /auth/generate-token`

**Request**

```json
{
  "clientId": "uuid-ou-id",
  "email": "cliente@email.com",
  "userName": "cliente"
}
```

**Response 200**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAtUtc": "2026-08-02T23:00:00Z"
}
```

---

## Escopo fora deste repositório

- Register / refresh-token / revoke-token completos do `AuthController` (só entram se a Fase 3 pedir explicitamente)
- API ASP.NET + Docker/K8s do legado
- Migrações de banco e schema (Repo 3)
- Frontend

---

## Roadmap de implementação

- [x] Documentar objetivo, arquitetura e mapa de migração (este README)
- [x] Criar esqueleto `functions/*`, `shared/db`, `terraform/`
- [x] Implementar `validate-cpf` a partir de `Cpf` + `IsValidCpf`
- [x] Esqueleto `check-client` + `shared/db` (stub até Repo 3)
- [x] Implementar `generate-token` com `JwtSettings` / assinatura HMAC
- [x] Terraform: Lambdas + API Gateway + outputs
- [x] CI/CD (GitHub Actions: build, security, CodeQL, Terraform plan/apply)
- [ ] Integração real com banco readonly (Repo 3)
- [ ] Testes automatizados + smoke test na AWS

---

## Referências

- Legado: https://github.com/Gearflow-Fiap/gearflow-legado
- Arquivos-chave de migração:
  - [`Cpf.cs`](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Domain/Entities/ClientAggregate/Cpf.cs)
  - [`Utils.cs` (`IsValidCpf`)](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Domain/Utils/Utils.cs)
  - [`LoginUserUseCase.cs`](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Application/UseCases/Auth/LoginUser/LoginUserUseCase.cs)
  - [`JwtSettings.cs`](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Application/Settings/JwtSettings.cs)
  - [`ConfigureJwtExtensions.cs`](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Api/Configurations/ConfigureJwtExtensions.cs)
  - [`AuthTokenHelper.cs`](https://github.com/Gearflow-Fiap/gearflow-legado/blob/develop/GearFlow.Application/UseCases/Auth/AuthTokenHelper.cs)
- Documentação AWS:
  - [AWS Lambda](https://docs.aws.amazon.com/lambda/)
  - [API Gateway](https://docs.aws.amazon.com/apigateway/)
  - [.NET on AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html)
  - [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
