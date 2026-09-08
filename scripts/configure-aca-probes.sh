#!/usr/bin/env bash
# Configure liveness (/health) and readiness (/ready) probes on an Azure Container App.
# Usage: bash scripts/configure-aca-probes.sh <app-name> <resource-group>
#
# PATCH only containers (with probes). Do not round-trip scale/configuration —
# az show returns fields (cooldownPeriod, pollingInterval, identitySettings, runtime)
# that api-version 2024-03-01 rejects.
set -euo pipefail

APP="${1:?container app name required}"
RG="${2:?resource group required}"
API_VERSION="${ACA_API_VERSION:-2024-03-01}"

echo "==> Configuring health probes on $APP"
ID=$(az containerapp show -n "$APP" -g "$RG" --query id -o tsv)

BODY=$(az containerapp show -n "$APP" -g "$RG" -o json | jq '
  {
    properties: {
      template: {
        containers: (
          .properties.template.containers // []
          | map(
              # Keep only fields the 2024-03-01 container schema accepts, plus probes.
              {
                name: .name,
                image: .image,
                command: .command,
                args: .args,
                env: .env,
                resources: .resources,
                volumeMounts: .volumeMounts,
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
              | with_entries(select(.value != null))
            )
        )
      }
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
