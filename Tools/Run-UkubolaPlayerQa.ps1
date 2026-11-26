param(
    [string] $Executable,
    [string] $Scene = 'E1M1_Save',
    [switch] $LegacyLoad,
    [string] $Scenario,
    [int] $ScreenWidth = 1280,
    [int] $ScreenHeight = 960,
    [ValidateRange(-1,2)] [int] $Quality = -1,
    [switch] $ClassicAspect,
    [string] $ReportPath,
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
$scenarioJson = @{ mode = $mode; scene = $Scene; warmupFrames = 60; sampleFrames = 300; yaw = @(0,60,120,180,240,300) } | ConvertTo-Json
if ($Scenario) { $scenarioJson = Get-Content -LiteralPath $Scenario -Raw }
$scenarioData = $scenarioJson | ConvertFrom-Json
$scenarioData | Add-Member -NotePropertyName captureWidth -NotePropertyValue $ScreenWidth -Force
$scenarioData | Add-Member -NotePropertyName captureHeight -NotePropertyValue $ScreenHeight -Force
if ($Quality -ge 0) { $scenarioData | Add-Member -NotePropertyName quality -NotePropertyValue $Quality -Force }
if ($PSBoundParameters.ContainsKey('ClassicAspect')) { $scenarioData | Add-Member -NotePropertyName classicAspect -NotePropertyValue ([bool]$ClassicAspect) -Force }
$scenarioJson = $scenarioData | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText((Join-Path $qaRoot 'scenario.json'), $scenarioJson, [Text.UTF8Encoding]::new($false))
$arguments = @('-ukubola-qa-root', ('"' + $qaRoot + '"'), '-screen-width', $ScreenWidth.ToString(), '-screen-height', $ScreenHeight.ToString(), '-screen-fullscreen', '0', '-force-d3d11', '-logFile', ('"' + (Join-Path $qaRoot 'Player.log') + '"'))
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
$result | Select-Object success,scene,quality,outputWidth,outputHeight,windowWidth,windowHeight,internalWidth,internalHeight,behaviorChecks,error | ConvertTo-Json -Depth 4
if (Select-String -LiteralPath (Join-Path $qaRoot 'Player.log') -Pattern 'variant .*not found|Shader error|is corrupted!|Crash!!!' -Quiet) { throw "Player shader/data validation failed; inspect $qaRoot\Player.log" }
if (-not $result.success -or $player.ExitCode -ne 0) { exit 1 }
if ($LegacyLoad -and -not $result.snapshotRoundTrip) { throw 'Legacy snapshot round trip was not verified.' }
if ($result.captures.Count -lt 6) { throw 'Six real screenshots were not produced.' }
$validationPath = Join-Path $qaRoot 'capture-validation.json'
python (Join-Path $PSScriptRoot 'Validate-UkubolaCaptures.py') $resultPath | Set-Content -LiteralPath $validationPath -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Blank or missing captures rejected; inspect the retained player evidence.' }
Write-Output "Validated $($result.captures.Count) real captures: $validationPath"
if ($ReportPath) {
    $reportDirectory = Split-Path $ReportPath -Parent
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    Copy-Item -LiteralPath $resultPath -Destination $ReportPath
}
