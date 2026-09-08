#!/usr/bin/env bash
# Configure liveness (/health) and readiness (/ready) probes on an Azure Container App.
# Usage: bash scripts/configure-aca-probes.sh <app-name> <resource-group>
#
# Uses PATCH with only the template so we do not round-trip configuration fields
# (identitySettings, runtime, secrets) that older API versions reject or wipe.
set -euo pipefail

APP="${1:?container app name required}"
RG="${2:?resource group required}"
API_VERSION="${ACA_API_VERSION:-2024-03-01}"

echo "==> Configuring health probes on $APP"
ID=$(az containerapp show -n "$APP" -g "$RG" --query id -o tsv)

BODY=$(az containerapp show -n "$APP" -g "$RG" -o json | jq '
  {
    properties: {
      template: (
        .properties.template
        | {
            containers: (
              .containers // []
              | map(
                  del(.probes)
                  | . + {
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
            ),
            scale: .scale,
            volumes: .volumes,
            initContainers: .initContainers,
            serviceBinds: .serviceBinds
          }
        | with_entries(select(.value != null))
      )
    }
  }
')

TMP=$(mktemp)
printf '%s' "$BODY" > "$TMP"

az rest --method PATCH \
  --uri "https://management.azure.com${ID}?api-version=${API_VERSION}" \
  --body @"$TMP" \
  --only-show-errors >/dev/null

rm -f "$TMP"

echo "    Liveness=/health Readiness=/ready on port 8080"
