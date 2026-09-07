# BetterMe

A mental health platform for psychologists and patients: mood tracking, journaling, session booking, and a curated resource library. Built as a personal project with ASP.NET Core, Blazor WebAssembly, EF Core, and PostgreSQL.

**Live app:** [https://ambitious-pond-095898400.5.azurestaticapps.net/login](https://ambitious-pond-095898400.5.azurestaticapps.net/login)

## What it does

- **Patients**: log mood, write journal entries, browse resources, book sessions, complete onboarding.
- **Psychologists**: manage patients, sessions, and published resources.
- **Admins**: operational visibility via `/api/ops/database` (migrations, connections, `pg_stat_statements`).

Auth is JWT-based (access + refresh tokens). Schema changes ship as EF migrations in git.

## Architecture

```mermaid
flowchart TB
  subgraph users [Users]
    Browser[Browser]
  end

  subgraph azure [Azure (eastasia)]
    SWA[Static Web App<br/>Blazor WASM]
    API[Container App<br/>BetterMe.API]
    PG[(PostgreSQL 16<br/>Flexible Server)]
    KV[Key Vault<br/>secrets]
    LAW[Log Analytics]
  end

  subgraph cicd [GitHub Actions]
    TF[Terraform<br/>plan / apply]
    DeployAPI[Deploy API<br/>build → GHCR → ACA]
    DeployWeb[Deploy Web<br/>publish → SWA]
  end

  subgraph state [Terraform state]
    SA[(Storage account<br/>eastasia)]
  end

  Browser -->|HTTPS| SWA
  Browser -->|HTTPS /api| API
  SWA -.->|ApiBaseUrl| API
  API -->|EF Core| PG
  API -.->|connection string, JWT| KV
  PG -->|diagnostics| LAW

  TF -->|OIDC| azure
  TF --> SA
  DeployAPI -->|image + registry auth| API
  DeployWeb -->|upload wwwroot| SWA
```

| Layer | Tech |
|-------|------|
| Frontend | Blazor WebAssembly, MudBlazor |
| API | ASP.NET Core 7, JWT, Swagger (dev) |
| Data | EF Core 7, PostgreSQL 16, Npgsql |
| Infra | Terraform (`infra/`), Azure Container Apps, Static Web Apps, Key Vault |
| CI/CD | GitHub Actions: Terraform, API deploy, frontend deploy |

**Repo layout**

```
src/
  BetterMe.API/           # REST API, health, ops
  BetterMe.Web/           # Blazor WASM client
  BetterMe.Infrastructure/# EF Core, Identity, services
  BetterMe.Shared/        # DTOs, enums
infra/
  modules/app-group/      # RG, Key Vault, managed identity
  projects/betterme/dev/  # Postgres, Log Analytics
tests/BetterMe.Tests/
```

## Local development

```bash
docker compose up -d
# If this machine has no .NET 7 runtime: $env:DOTNET_ROLL_FORWARD='LatestMajor'
dotnet ef database update --project src/BetterMe.Infrastructure --startup-project src/BetterMe.API
dotnet run --project src/BetterMe.API
dotnet run --project src/BetterMe.Web
```

| Service | URL |
|---------|-----|
| API (HTTP) | http://localhost:5062 |
| API Swagger | https://localhost:7262/swagger |
| Blazor | https://localhost:7157 |
| Postgres | localhost:5432 (`betterme` / `betterme_user` / `localdevpassword`) |
| pgAdmin | http://localhost:5050 |

The API applies pending EF migrations on startup (single replica), then seeds roles. Resource seed data runs in Development only.

## Azure hosting

Infrastructure lives in `infra/` (resource group `rg-betterme-dev-ea`, region **eastasia**). Terraform state is in a separate storage account; CI applies on push to `main` via GitHub OIDC.

**First-time setup:** run in [Azure Cloud Shell](https://shell.azure.com) on your subscription:

```bash
bash scripts/azure-hosting-bootstrap.sh
```

That creates the Container Apps environment, API app, Static Web App, and prints values for GitHub secrets.

**GitHub `Prod` environment** (variables and secrets):

| Name | Type | Purpose |
|------|------|---------|
| `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` | Variable | Terraform OIDC |
| `TF_STATE_RG` / `TF_STATE_SA` / `TF_STATE_CONTAINER` | Variable | Remote state |
| `API_BASE_URL` | Variable | Container App HTTPS URL (frontend build) |
| `AZURE_RESOURCE_GROUP` | Secret | `rg-betterme-dev-ea` |
| `AZURE_CREDENTIALS` | Secret | Deploy SP JSON (`--sdk-auth`) |
| `GHCR_TOKEN` | Secret | PAT with `read:packages` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Secret | SWA deployment token |

Federated credential for Terraform (subject must match `environment: Prod`):

```
repo:agthetpaing/betterme:environment:Prod
```

**Deploy workflows:** Actions → **Deploy API** or **Deploy Frontend** → Run workflow.

Manual Terraform apply (optional):

```bash
cd infra/projects/betterme/dev
cp backend.hcl.example backend.hcl
az login
terraform init -backend-config=backend.hcl
terraform apply
```

## Ops and health

| Endpoint | Auth | Meaning |
|----------|------|---------|
| `GET /health` | Anonymous | Process is up |
| `GET /ready` | Anonymous | Postgres accepts `SELECT 1` |
| `GET /api/ops/database` | Admin JWT | Version, migrations, `pg_stat_activity`, `pg_stat_statements` |

## Roadmap

- Terraform for Container App and Static Web App (currently bootstrap script)
- `uat` / `prod` environments
- Private endpoints for Postgres and Key Vault
