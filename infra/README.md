# infra

Terraform root for BetterMe. Layout is `projects/<group>/<env>`. See [AGENTS.md](AGENTS.md).

```bash
cd projects/betterme/dev
terraform init
terraform fmt -recursive ../..
terraform validate
terraform plan
```

CI runs `fmt -check` and `validate` only — no apply.
