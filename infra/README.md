# infra

Terraform root for BetterMe. Layout: `projects/<group>/<env>/`. See [AGENTS.md](AGENTS.md).

## CI (GitHub Actions)

Push to `main` (with `infra/**` changes) runs `terraform apply` via the **Prod** environment using OIDC.

**Prod environment variables required:**

| Variable | Example |
|----------|---------|
| `AZURE_CLIENT_ID` | Service principal app ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Subscription GUID |
| `TF_STATE_RG` | `rg-betterme-tfstate-sea` |
| `TF_STATE_SA` | `stbettermetfstateae` |
| `TF_STATE_CONTAINER` | `dev` |

PRs run `fmt`, `validate`, and `terraform plan` (also uses **Prod** env + OIDC).

## Local

```bash
cd projects/betterme/dev
cp backend.hcl.example backend.hcl   # edit if needed
az login
terraform init -backend-config=backend.hcl
terraform fmt -recursive ../../..
terraform plan
terraform apply
```

Region: **eastasia** (`betterme-dev-ea`).
