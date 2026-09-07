# BetterMe Terraform

`infra/` is the Terraform root so it does not collide with `src/`.

## Environment and Structure

- Project path: `projects/<group>/<env>/` (example: `projects/betterme/dev/`).
- Common envs: `dev`, `uat`, `prod`. Only `dev` is scaffolded.
- Typical files in each env: `main.tf`, `_providers.tf`, `_data.tf`, plus resource files (for example `postgres-*.tf`, `log-analytics.tf`, `app-*.tf`).
- Reusable modules live in `modules/`.
- Module plumbing: `main.tf`, `_variables.tf`, `_outputs.tf`.

## Common Tasks

### 1) Add Resource(s) to an Existing Group

1. Add or update resource files under `projects/<group>/<env>/`.
2. Keep `main.tf` limited to `locals` and `module.app_group`.
3. Format before commit.

```bash
terraform fmt -recursive infra
```

### 2) Validate (no apply)

```bash
cd infra/projects/betterme/dev
terraform init
terraform validate
terraform plan
```

Do not apply from CI. Destroy the isolated `dev` resource group after a live demo.
