param([int] $TimeoutSeconds=600)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$proofRoot=Join-Path $projectRoot ('Evidence/Evolution/slice-generation-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $proofRoot | Out-Null
$semantic=Join-Path $projectRoot 'Assets/_UKUBOLA/Generated/Evolution/OutpostSlice.semantic.json'
foreach ($run in @(1,2)) {
    & (Join-Path $PSScriptRoot 'Invoke-UkubolaJob.ps1') -Operation BuildOutpostSlice -TimeoutSeconds $TimeoutSeconds
    if ($LASTEXITCODE -ne 0) { throw "Generation $run failed" }
    Copy-Item -LiteralPath $semantic -Destination (Join-Path $proofRoot "semantic-$run.json")
}
$first=Get-FileHash -LiteralPath (Join-Path $proofRoot 'semantic-1.json') -Algorithm SHA256
$second=Get-FileHash -LiteralPath (Join-Path $proofRoot 'semantic-2.json') -Algorithm SHA256
$same=$first.Hash -eq $second.Hash
@{success=$same;semanticSha256=$second.Hash;kind='Semantic identities, placements, static geometry and component counts; scene YAML bytes are not an identity contract'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $proofRoot 'result.json') -Encoding utf8
if (-not $same) { throw "Non-repeatable slice generation: $proofRoot" }
Write-Output "Two generations preserve mission semantics: $proofRoot"
exit 0
