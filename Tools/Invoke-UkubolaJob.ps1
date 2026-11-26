param(
    [Parameter(Mandatory=$true)] [ValidateSet('Audit','SelfTests','RunEditTests','BuildArtRoom','BakeArtRoom','BuildArtPreview','BuildArtPreviewV2','ValidateLegacy','BuildLegacy','RestoreBuildSettings','ReloadCheckpoint','Fail','PlayQa')] [string] $Operation,
    [string] $Scene,
    [int] $TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$queueRoot = Join-Path $projectRoot 'Temp\UkubolaAutomation'
$jobId = [Guid]::NewGuid().ToString('N')
$resultPath = Join-Path $projectRoot "Evidence\Evolution\$jobId\result.json"
New-Item -ItemType Directory -Path $queueRoot -Force | Out-Null
$job = @{ id = $jobId; operation = $Operation; scene = $Scene } | ConvertTo-Json
$pendingPath = Join-Path $queueRoot "$jobId.pending"
[IO.File]::WriteAllText($pendingPath, $job, [Text.UTF8Encoding]::new($false))
Move-Item -LiteralPath $pendingPath -Destination (Join-Path $queueRoot "$jobId.job.json")
Write-Output "Submitted $Operation : $jobId"
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while (-not (Test-Path -LiteralPath $resultPath)) {
    if ([DateTime]::UtcNow -ge $deadline) { throw "Job timed out; retained request/journal for recovery. Result: $resultPath" }
    Start-Sleep -Milliseconds 500
}
$result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
$result | ConvertTo-Json -Depth 8
if ($result.status -ne 'succeeded') { exit 1 }
