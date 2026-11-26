param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$matrixRoot=Join-Path $projectRoot ('Evidence\Evolution\presentation-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $matrixRoot -Force | Out-Null
$cases=@(
    @{name='Low_720p';quality=0;width=1280;height=720;internalWidth=480;internalHeight=270;classic=$false},
    @{name='Medium_1080p';quality=1;width=1920;height=1080;internalWidth=640;internalHeight=360;classic=$false},
    @{name='High_1440p';quality=2;width=2560;height=1440;internalWidth=960;internalHeight=540;classic=$false},
    @{name='Medium_4x3';quality=1;width=1280;height=960;internalWidth=480;internalHeight=360;classic=$true}
)
$completed=@()
try {
    foreach($case in $cases) {
        $report=Join-Path $matrixRoot ($case.name+'.json')
        Write-Output "Checking $($case.name)"
        & (Join-Path $PSScriptRoot 'Run-UkubolaPlayerQa.ps1') -Executable $Executable -Scene ArtRoom -Scenario (Join-Path $PSScriptRoot 'QA\ArtRoom.json') -ScreenWidth $case.width -ScreenHeight $case.height -Quality $case.quality -ClassicAspect:([bool]$case.classic) -ReportPath $report
        if($LASTEXITCODE -ne 0){throw "Player QA failed: $($case.name)"}
        $actual=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if($actual.internalWidth -ne $case.internalWidth -or $actual.internalHeight -ne $case.internalHeight -or $actual.outputWidth -ne $case.width -or $actual.outputHeight -ne $case.height -or $actual.quality -ne $case.quality){throw "Unexpected render dimensions/profile: $($case.name)"}
        $completed+=@{name=$case.name;success=$true;report=$report;windowWidth=$actual.windowWidth;windowHeight=$actual.windowHeight;outputWidth=$actual.outputWidth;outputHeight=$actual.outputHeight}
    }
    @{success=$true;kind='Standalone offscreen output and UI at requested dimensions; physical window recorded separately. Physical stair/nav checks; static timing is not P26 performance acceptance';cases=$completed} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $matrixRoot 'result.json') -Encoding utf8
    Write-Output "Presentation matrix passed: $matrixRoot"
}
catch {
    @{success=$false;error=$_.Exception.Message;cases=$completed} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $matrixRoot 'result.json') -Encoding utf8
    throw
}
