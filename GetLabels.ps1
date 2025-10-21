$env:DOCKER_METADATA_OUTPUT_JSON = '{"tags":["ghcr.io/jeremy-morren/azure-cli-managed-identity:main"],"labels":{"org.opencontainers.image.created":"2025-10-21T02:44:46.055Z","org.opencontainers.image.description":"An HTTP server that simulates a Managed Identity endpoint for use by containers in development/CI","org.opencontainers.image.licenses":"MIT","org.opencontainers.image.revision":"68f214056e49ec3d5433ebce0d1606d9aef4a6e0","org.opencontainers.image.source":"https://github.com/jeremy-morren/azure-cli-managed-identity","org.opencontainers.image.title":"azure-cli-managed-identity","org.opencontainers.image.url":"https://github.com/jeremy-morren/azure-cli-managed-identity","org.opencontainers.image.version":"main"},"annotations":["manifest:org.opencontainers.image.created=2025-10-21T02:44:46.055Z","manifest:org.opencontainers.image.description=An HTTP server that simulates a Managed Identity endpoint for use by containers in development/CI","manifest:org.opencontainers.image.licenses=MIT","manifest:org.opencontainers.image.revision=68f214056e49ec3d5433ebce0d1606d9aef4a6e0","manifest:org.opencontainers.image.source=https://github.com/jeremy-morren/azure-cli-managed-identity","manifest:org.opencontainers.image.title=azure-cli-managed-identity","manifest:org.opencontainers.image.url=https://github.com/jeremy-morren/azure-cli-managed-identity","manifest:org.opencontainers.image.version=main"]}'

$metadata = $env:DOCKER_METADATA_OUTPUT_JSON | ConvertFrom-Json

$labels = @()
foreach ($l in $metadata.labels.PSObject.Properties) {
    $labels += "$($l.Name)=$($l.Value)"
}

$labels