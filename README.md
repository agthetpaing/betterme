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

## Terraform (isolated Azure `dev`)

`infra/` is the Terraform root. It creates a **new** resource group (`rg-betterme-dev-ae`). It does not import or change existing Container Apps / Static Web Apps.

```
infra/
  AGENTS.md
  modules/app-group/           # resource group, Key Vault, identity
  projects/betterme/dev/
    main.tf                    # locals + module.app_group
    _providers.tf
    _data.tf
    postgres-core.tf           # inline Flexible Server 16
    log-analytics.tf
```

```bash
cd infra/projects/betterme/dev
terraform init
terraform fmt -recursive ../../..
terraform plan
# terraform apply    # optional demo — Burstable B1ms, destroy afterwards
# terraform destroy
```

Before apply, add your public IP to `postgres_firewall_rules` in `postgres-core.tf` (named like `Allow-Dev-Home`). Admin credentials land in Key Vault. State is local and gitignored; swap in an `azurerm` backend + OIDC when sharing state.

Connection string secret name: `psql-betterme-dev-ae-core-connection-string`.

## Ops and health

| Endpoint | Auth | Meaning |
|----------|------|---------|
| `GET /health` | Anonymous | Process is up |
| `GET /ready` | Anonymous | Postgres accepts `SELECT 1` |
| `GET /api/ops/database` | Admin JWT | Version, migrations, `pg_stat_activity`, `pg_stat_statements` |

## How this maps to Xero DRE (5-minute demo)

1. Open `infra/projects/betterme/dev` — env root files are `main.tf`, `_providers.tf`, `postgres-core.tf`.
2. `terraform plan` — `app_group` (RG, Key Vault, identity) plus golden-path Postgres (backups, diagnostics, tags, secrets in Key Vault).
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
