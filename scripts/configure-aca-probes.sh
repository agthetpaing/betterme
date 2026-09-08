#!/usr/bin/env bash
# Configure liveness (/health) and readiness (/ready) probes on an Azure Container App.
# Usage: bash scripts/configure-aca-probes.sh <app-name> <resource-group>
set -euo pipefail

APP="${1:?container app name required}"
RG="${2:?resource group required}"

echo "==> Configuring health probes on $APP"
ID=$(az containerapp show -n "$APP" -g "$RG" --query id -o tsv)

BODY=$(az containerapp show -n "$APP" -g "$RG" -o json | jq '
  del(.id, .name, .type, .systemData, .etag) |
  .properties |= del(
    .provisioningState,
    .latestRevisionName,
    .latestReadyRevisionName,
    .latestRevisionFqdn,
    .runningStatus,
    .eventStreamEndpoint,
    .outboundIpAddresses
  ) |
  .properties.template.containers |= map(
    . + {
      probes: [
        {
          type: "Liveness",
          httpGet: { path: "/health", port: 8080 },
          periodSeconds: 30,
          failureThreshold: 3,
          initialDelaySeconds: 10,
          timeoutSeconds: 5
        },
        {
          type: "Readiness",
          httpGet: { path: "/ready", port: 8080 },
          periodSeconds: 10,
          failureThreshold: 3,
          initialDelaySeconds: 5,
          timeoutSeconds: 5
        }
      ]
    }
  )
')

az rest --method PUT \
  --uri "https://management.azure.com${ID}?api-version=2024-03-01" \
  --body "$BODY" \
  --only-show-errors >/dev/null

echo "    Liveness=/health Readiness=/ready on port 8080"
