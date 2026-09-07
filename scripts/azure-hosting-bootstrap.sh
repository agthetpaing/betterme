#!/usr/bin/env bash
# Run in Azure Cloud Shell on your PERSONAL (Azure for Students) subscription.
# Do not run this from a work/Sportcast az login session.
#
# Prerequisites:
#   - Terraform apply already created rg-betterme-dev-ea, kv-betterme-dev-ea, Postgres
#   - Logged into the correct subscription
#
# Usage:
#   bash scripts/azure-hosting-bootstrap.sh
#   # or after cloning: curl/raw + bash, or paste into Cloud Shell

set -euo pipefail

SUB_ID="${AZURE_SUBSCRIPTION_ID:-eaa747f1-714a-45ac-af51-957558f36fc9}"
RG="rg-betterme-dev-ea"
LOCATION="eastasia"
KV="kv-betterme-dev-ea"
CAE="cae-betterme-dev-ea"
APP="betterme-api"
SWA="swa-betterme-dev-ea"
IDENTITY="id-betterme-dev-ea"
CONN_SECRET="psql-betterme-dev-ea-core-connection-string"
JWT_SECRET_NAME="jwt-signing-key"
IMAGE="${CONTAINER_IMAGE:-ghcr.io/agthetpaing/betterme/betterme-api:latest}"

echo "==> Using subscription $SUB_ID"
az account set --subscription "$SUB_ID"

# Terraform applied as the GitHub SP, so only that principal has KV access policies.
# Grant the signed-in Cloud Shell user secrets access (control-plane; needs Contributor on the vault/RG).
USER_OID=$(az ad signed-in-user show --query id -o tsv)
echo "==> Grant Key Vault secrets access to signed-in user ($USER_OID)"
az keyvault set-policy --name "$KV" \
  --object-id "$USER_OID" \
  --secret-permissions get list set delete \
  --only-show-errors >/dev/null

echo "==> Ensure JWT signing key in Key Vault"
if ! az keyvault secret show --vault-name "$KV" --name "$JWT_SECRET_NAME" &>/dev/null; then
  JWT_VALUE=$(openssl rand -base64 48 | tr -d '\n')
  az keyvault secret set --vault-name "$KV" --name "$JWT_SECRET_NAME" --value "$JWT_VALUE" >/dev/null
  echo "    Created $JWT_SECRET_NAME"
else
  echo "    $JWT_SECRET_NAME already exists"
fi

echo "==> Container Apps Environment"
if ! az containerapp env show -n "$CAE" -g "$RG" &>/dev/null; then
  az containerapp env create \
    --name "$CAE" \
    --resource-group "$RG" \
    --location "$LOCATION" \
    --logs-destination none
else
  echo "    $CAE already exists"
fi

IDENTITY_ID=$(az identity show -n "$IDENTITY" -g "$RG" --query id -o tsv)
IDENTITY_PRINCIPAL=$(az identity show -n "$IDENTITY" -g "$RG" --query principalId -o tsv)
KV_URI=$(az keyvault show -n "$KV" --query properties.vaultUri -o tsv)

echo "==> Grant identity get/list on Key Vault (if missing)"
az keyvault set-policy --name "$KV" \
  --object-id "$IDENTITY_PRINCIPAL" \
  --secret-permissions get list \
  --only-show-errors >/dev/null || true

echo "==> Container App $APP"
if ! az containerapp show -n "$APP" -g "$RG" &>/dev/null; then
  az containerapp create \
    --name "$APP" \
    --resource-group "$RG" \
    --environment "$CAE" \
    --image "mcr.microsoft.com/k8se/quickstart:latest" \
    --target-port 8080 \
    --ingress external \
    --min-replicas 0 \
    --max-replicas 1 \
    --cpu 0.25 \
    --memory 0.5Gi \
    --user-assigned "$IDENTITY_ID" \
    --env-vars \
      "ASPNETCORE_ENVIRONMENT=Production" \
      "Jwt__Issuer=BetterMe.API" \
      "Jwt__Audience=BetterMe.Web"
else
  echo "    $APP already exists Ξ²β‚¬β€ updating secrets/env"
fi

# Key Vault references require the system/user-assigned identity on the app
az containerapp secret set \
  --name "$APP" \
  --resource-group "$RG" \
  --secrets \
    "db-conn=keyvaultref:${KV_URI}secrets/${CONN_SECRET},identityref:${IDENTITY_ID}" \
    "jwt-key=keyvaultref:${KV_URI}secrets/${JWT_SECRET_NAME},identityref:${IDENTITY_ID}"

az containerapp update \
  --name "$APP" \
  --resource-group "$RG" \
  --set-env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "Jwt__Issuer=BetterMe.API" \
    "Jwt__Audience=BetterMe.Web" \
    "ConnectionStrings__DefaultConnection=secretref:db-conn" \
    "Jwt__Key=secretref:jwt-key"

API_FQDN=$(az containerapp show -n "$APP" -g "$RG" --query properties.configuration.ingress.fqdn -o tsv)
API_URL="https://${API_FQDN}"
echo "==> API URL: $API_URL"

echo "==> Static Web App $SWA"
if ! az staticwebapp show -n "$SWA" -g "$RG" &>/dev/null; then
  az staticwebapp create \
    --name "$SWA" \
    --resource-group "$RG" \
    --location "eastasia" \
    --sku Free
else
  echo "    $SWA already exists"
fi

SWA_URL=$(az staticwebapp show -n "$SWA" -g "$RG" --query defaultHostname -o tsv)
SWA_HTTPS="https://${SWA_URL}"
SWA_TOKEN=$(az staticwebapp secrets list -n "$SWA" -g "$RG" --query "properties.apiKey" -o tsv)

az containerapp update \
  --name "$APP" \
  --resource-group "$RG" \
  --set-env-vars "AllowedOrigins=${SWA_HTTPS}"

echo "==> EF migrations (run from a machine with .NET + your home IP on Postgres firewall)"
echo "    export ConnectionStrings__DefaultConnection=\$(az keyvault secret show --vault-name $KV --name $CONN_SECRET --query value -o tsv)"
echo "    dotnet ef database update --project src/BetterMe.Infrastructure --startup-project src/BetterMe.API"

echo ""
echo "==> GitHub secrets / variables to set (repo or Prod environment)"
echo ""
echo "  Repository / environment SECRET AZURE_RESOURCE_GROUP = $RG"
echo "  Repository / environment SECRET AZURE_STATIC_WEB_APPS_API_TOKEN = <copied below>"
echo "  Repository / environment VARIABLE API_BASE_URL = $API_URL"
echo ""
echo "  For deploy-api.yml, create SP credentials (or reuse github-betterme-terraform with a client secret):"
echo "    az ad sp create-for-rbac --name github-betterme-deploy --role contributor \\"
echo "      --scopes /subscriptions/$SUB_ID/resourceGroups/$RG --sdk-auth"
echo "    Ξ²β€ β€™ store entire JSON as SECRET AZURE_CREDENTIALS"
echo ""
echo "  SWA deployment token:"
echo "  $SWA_TOKEN"
echo ""
echo "Done. Push API/Web changes (or re-run workflows) after secrets are set."
echo "Optional: after first image push, update the app image:"
echo "  az containerapp update -n $APP -g $RG --image $IMAGE"
