# BetterMe

Mental health platform for psychologists and patients. ASP.NET Core API + Blazor WebAssembly, EF Core, PostgreSQL 16.

This repo also encodes a **database golden path**: Terraform uses a `projects/<group>/<env>` layout, schema is EF migrations (not `EnsureCreated`), and the API exposes health plus a small admin ops surface.

## Local development

```bash
docker compose up -d
# If this machine has no .NET 7 runtime: $env:DOTNET_ROLL_FORWARD='LatestMajor'
dotnet ef database update --project src/BetterMe.Infrastructure --startup-project src/BetterMe.API
dotnet run --project src/BetterMe.API
dotnet run --project src/BetterMe.Web
```

If you previously used `EnsureCreated()`, reset the local volume first so EF can own the schema:

```bash
docker compose down -v
docker compose up -d
```

| Service | URL |
|---------|-----|
| API (HTTP) | http://localhost:5062 |
| API Swagger | https://localhost:7262/swagger |
| Blazor | https://localhost:7157 |
| Postgres | localhost:5432 (`betterme` / `betterme_user` / `localdevpassword`) |
| pgAdmin | http://localhost:5050 |

Apply schema with `dotnet ef database update`. The API does **not** call `Database.Migrate()` on startup — replicas must not race schema changes. Seed data runs in Development only.

## Terraform (Azure `dev` — eastasia)

`infra/` provisions the database platform: resource group, Key Vault, PostgreSQL Flexible Server 16, Log Analytics. Region is **eastasia** (`betterme-dev-ea`). CI applies on push to `main` via GitHub Actions (OIDC + **Prod** environment).

```
infra/
  AGENTS.md
  modules/app-group/
  projects/betterme/dev/
    main.tf
    _providers.tf
    backend.hcl.example
    postgres-core.tf
    log-analytics.tf
```

### GitHub **Prod** environment variables

| Variable | Value |
|----------|-------|
| `AZURE_CLIENT_ID` | SP app ID (OIDC) |
| `AZURE_TENANT_ID` | Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID |
| `TF_STATE_RG` | `rg-betterme-tfstate-sea` |
| `TF_STATE_SA` | `stbettermetfstateae` |
| `TF_STATE_CONTAINER` | `dev` |

Azure AD federated credentials (subject must match the workflow — **Prod environment** changes the claim):

| Credential name | Subject | Used by |
|-----------------|---------|---------|
| `github-betterme-prod-env` | `repo:agthetpaing/betterme:environment:Prod` | `plan` + `apply` (required) |
| `github-betterme-pr` | `repo:agthetpaing/betterme:pull_request` | PR `plan` (optional) |

```bash
APP_ID="<your-sp-client-id>"

az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "github-betterme-prod-env",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:agthetpaing/betterme:environment:Prod",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### Local apply (optional)

```bash
cd infra/projects/betterme/dev
cp backend.hcl.example backend.hcl
az login
terraform init -backend-config=backend.hcl
terraform plan
terraform apply
```

After apply, run migrations against Azure Postgres:

```bash
az keyvault secret show --vault-name kv-betterme-dev-ea \
  --name psql-betterme-dev-ea-core-connection-string --query value -o tsv
```

Connection string secret: `psql-betterme-dev-ea-core-connection-string`.

### App hosting (personal Azure — Cloud Shell only)

Do **not** run these against a work/Sportcast `az` login. Use **Azure Cloud Shell** on **Azure for Students**.

```bash
# From repo root in Cloud Shell (or copy scripts/azure-hosting-bootstrap.sh)
bash scripts/azure-hosting-bootstrap.sh
```

That script creates the Container Apps Environment, `betterme-api`, Static Web App, JWT Key Vault secret, and prints GitHub secret values.

Then set GitHub **secrets** (repo **or** `Prod` environment — deploy workflows use `environment: Prod`):

| Name | Type | Value |
|------|------|-------|
| `AZURE_RESOURCE_GROUP` | Secret | `rg-betterme-dev-ea` |
| `AZURE_CREDENTIALS` | Secret | SP JSON from Cloud Shell (`--sdk-auth`) |
| `GHCR_TOKEN` | Secret | Classic PAT with `read:packages` (Container App image pulls) |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Secret | From bootstrap script / SWA portal |
| `API_BASE_URL` | Variable | `https://<betterme-api-fqdn>` |

Create deploy credentials in Cloud Shell:

```bash
az ad sp create-for-rbac --name github-betterme-deploy --role contributor \
  --scopes /subscriptions/eaa747f1-714a-45ac-af51-957558f36fc9/resourceGroups/rg-betterme-dev-ea \
  --sdk-auth
```

Paste the JSON into secret `AZURE_CREDENTIALS`. Then run **Actions → Deploy API to Azure Container Apps → Run workflow**. That builds, pushes to GHCR, and updates `betterme-api` with registry credentials (no manual `az containerapp update`).

EF migrations (add your public IP to Postgres firewall first if needed):

```bash
export ConnectionStrings__DefaultConnection="$(az keyvault secret show \
  --vault-name kv-betterme-dev-ea \
  --name psql-betterme-dev-ea-core-connection-string --query value -o tsv)"
dotnet ef database update --project src/BetterMe.Infrastructure --startup-project src/BetterMe.API
```

## Ops and health

| Endpoint | Auth | Meaning |
|----------|------|---------|
| `GET /health` | Anonymous | Process is up |
| `GET /ready` | Anonymous | Postgres accepts `SELECT 1` |
| `GET /api/ops/database` | Admin JWT | Version, migrations, `pg_stat_activity`, `pg_stat_statements` |

## How this maps to Xero DRE (5-minute demo)

1. Open `infra/projects/betterme/dev` — env root files are `main.tf`, `_providers.tf`, `postgres-core.tf`.
2. Push to `main` or run `terraform plan` — `app_group` plus golden-path Postgres in **eastasia**.
3. `docker compose up` + `dotnet ef database update` — schema as code.
4. Hit `/ready`, then `GET /api/ops/database` as an Admin.
5. Azure → AWS: Flexible Server ≈ RDS / Aurora Postgres; diagnostic settings ≈ Enhanced Monitoring; Key Vault ≈ Secrets Manager; the module interface is the portable part.
6. `terraform destroy` — do not leave spend running.

## Azure → AWS translation

| This repo (Azure) | Xero-shaped equivalent (AWS) |
|-------------------|------------------------------|
| PostgreSQL Flexible Server | RDS / Aurora PostgreSQL |
| `backup_retention_days` | RDS backup retention |
| Diagnostic settings → Log Analytics | Enhanced Monitoring / CloudWatch |
| Key Vault secrets | Secrets Manager |
| `projects/<group>/<env>` roots | Same IaC split: one state per env |
| EF migrations | Schema change as a reviewed artefact |
| `/ready` + `/api/ops/database` | Fleet health / internal tooling |

## Out of scope (for now)

Private endpoints, importing the existing Container App, `uat`/`prod` directories, and a chat agent on top of the ops endpoint.
