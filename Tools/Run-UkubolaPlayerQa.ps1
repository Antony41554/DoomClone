param(
    [string] $Executable,
    [string] $Scene = 'E1M1_Save',
    [switch] $LegacyLoad,
    [int] $TimeoutSeconds = 240
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $Executable) { $Executable = Join-Path $projectRoot 'Builds\UKUBOLA_Legacy_baee6d2_QA2\UKUBOLA.exe' }
$Executable = (Resolve-Path -LiteralPath $Executable).Path
$qaRoot = Join-Path $projectRoot ('Evidence\Evolution\player-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qaRoot -Force | Out-Null
$mode = 'capture'
if ($LegacyLoad) {
    $mode = 'legacy-load'
    $saveRoot = Join-Path $qaRoot 'Saves'
    New-Item -ItemType Directory -Path $saveRoot -Force | Out-Null
    $preservedSave = Join-Path $projectRoot 'Backups\Evolution\baseline\ProductionSaves\campaign-v1.json'
    Copy-Item -LiteralPath $preservedSave -Destination (Join-Path $saveRoot 'campaign-v1.json')
}
$scenario = @{ mode = $mode; scene = $Scene; warmupFrames = 60; sampleFrames = 300; yaw = @(0,60,120,180,240,300) } | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $qaRoot 'scenario.json'), $scenario, [Text.UTF8Encoding]::new($false))
$arguments = @('-ukubola-qa-root', ('"' + $qaRoot + '"'), '-screen-width', '1280', '-screen-height', '960', '-screen-fullscreen', '0', '-force-d3d11', '-logFile', ('"' + (Join-Path $qaRoot 'Player.log') + '"'))
$player = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "QA process $($player.Id): $qaRoot"
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while (-not $player.HasExited) {
    if ([DateTime]::UtcNow -ge $deadline) {
        $player.Refresh()
        if (-not $player.HasExited) { $player.Kill() }
        throw "QA timed out. Evidence and log retained: $qaRoot"
    }
    Start-Sleep -Milliseconds 500
    $player.Refresh()
}
$resultPath = Join-Path $qaRoot 'run-result.json'
if (-not (Test-Path -LiteralPath $resultPath)) { throw "Player exited $($player.ExitCode) without QA report: $qaRoot" }
$result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
$result | ConvertTo-Json -Depth 8
if (-not $result.success -or $player.ExitCode -ne 0) { exit 1 }
if ($LegacyLoad -and -not $result.snapshotRoundTrip) { throw 'Legacy snapshot round trip was not verified.' }
if ($result.captures.Count -lt 6) { throw 'Six real screenshots were not produced.' }
python (Join-Path $PSScriptRoot 'Validate-UkubolaCaptures.py') $resultPath
if ($LASTEXITCODE -ne 0) { throw 'Blank or missing captures rejected; inspect the retained player evidence.' }
