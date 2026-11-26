param(
    [ValidateSet('CheckAccess','Draft','Publish')] [string] $Mode='CheckAccess',
    [string] $AssetManifest,
    [string] $Commit,
    [string] $Notes,
    [long] $ReleaseId=0,
    [string] $Tag='evolution-preview-2026-11-26'
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$repository='Antony41554/DoomClone'
$api='https://api.github.com/repos/'+$repository
if ($Tag -notmatch '^evolution-preview-[a-zA-Z0-9.-]+$') { throw 'Unexpected preview tag' }
# Use the existing Git credential helper entirely in memory. Never print,
# persist, interpolate into command arguments, or include credentials in receipts.
$credentialRequest="protocol=https`nhost=github.com`npath=$repository.git`n`n"
$credentialReply=$credentialRequest | git -c credential.interactive=never credential fill 2>$null
if ($LASTEXITCODE -ne 0) { throw 'GitHub credential helper could not provide access' }
$credentials=@{}
foreach ($line in $credentialReply) { $parts=$line -split '=',2; if ($parts.Count -eq 2) { $credentials[$parts[0]]=$parts[1] } }
if (-not $credentials.ContainsKey('password')) { throw 'No GitHub credential available' }
$headers=@{Authorization='Bearer '+$credentials['password'];Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2026-03-10';'User-Agent'='UKUBOLA-local-release'}
try {
    $repo=Invoke-RestMethod -Uri $api -Headers $headers -Method Get
    if (-not $repo.permissions.push) { throw 'Repository write access is unavailable' }
    if ($Mode -eq 'CheckAccess') {
        @{success=$true;repository=$repo.full_name;canPush=[bool]$repo.permissions.push;defaultBranch=$repo.default_branch} | ConvertTo-Json
        exit 0
    }
    if (-not $AssetManifest -or -not $Notes -or $Commit -notmatch '^[a-f0-9]{40}$') { throw 'Concrete assets, notes and full source commit are required' }
    $manifest=Get-Content -LiteralPath $AssetManifest -Raw | ConvertFrom-Json
    if (-not $manifest.verifiedExtraction -or -not $manifest.nativeQaPassed -or -not $manifest.extractedExecutionPassed -or $manifest.assets.Count -lt 2) { throw 'Package evidence has not passed extraction and native QA' }
    foreach ($asset in $manifest.assets) {
        $assetPath=[IO.Path]::GetFullPath((Join-Path $projectRoot $asset.path))
        $buildRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds'))+[IO.Path]::DirectorySeparatorChar
        if (-not $assetPath.StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Only declared local build artifacts can be uploaded' }
        if ((Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $asset.sha256) { throw 'Release asset hash mismatch' }
    }
    $releases=Invoke-RestMethod -Uri ($api+'/releases?per_page=100') -Headers $headers -Method Get
    $release=@($releases | Where-Object tag_name -eq $Tag)
    if ($release.Count -gt 1) { throw 'Ambiguous release tag' }
    if ($release.Count -eq 0) {
        if ($Mode -ne 'Draft') { throw 'Prepare and verify a draft first' }
        $body=@{tag_name=$Tag;target_commitish=$Commit;name='UKUBOLA — Outpost Evolution Preview (Windows x64)';body=(Get-Content -LiteralPath $Notes -Raw);draft=$true;prerelease=$true} | ConvertTo-Json -Depth 5
        $release=Invoke-RestMethod -Uri ($api+'/releases') -Headers $headers -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    } else { $release=$release[0] }
    if (-not $release.draft) { throw 'Existing published release is preserved; choose a new tag' }
    if ($ReleaseId -gt 0 -and $release.id -ne $ReleaseId) { throw 'Release ID does not match the prepared draft' }
    if ($release.target_commitish -ne $Commit) { throw 'Draft source commit does not match the verified package' }
    foreach ($asset in $manifest.assets) {
        $existing=@($release.assets | Where-Object name -eq $asset.name)
        if ($Mode -eq 'Draft' -and $existing.Count -eq 0) {
            $uploadBase=($release.upload_url -split '\{')[0]
            $uploadUri=[Uri]$uploadBase
            if ($uploadUri.Host -ne 'uploads.github.com' -or -not $uploadUri.AbsolutePath.StartsWith('/repos/'+$repository+'/releases/')) { throw 'Unexpected GitHub upload destination' }
            $uploaded=Invoke-RestMethod -Uri ($uploadBase+'?name='+[Uri]::EscapeDataString($asset.name)) -Headers $headers -Method Post -ContentType $asset.contentType -InFile (Join-Path $projectRoot $asset.path) -TimeoutSec 1200
            if ($uploaded.state -ne 'uploaded' -or $uploaded.digest -ne ('sha256:'+$asset.sha256)) { throw 'GitHub did not confirm the uploaded asset digest' }
        } elseif ($existing.Count -ne 1 -or $existing[0].digest -ne ('sha256:'+$asset.sha256) -or $existing[0].state -ne 'uploaded') { throw 'Release asset is missing or differs; existing assets are preserved' }
    }
    $release=Invoke-RestMethod -Uri ($api+'/releases/'+$release.id) -Headers $headers -Method Get
    if ($Mode -eq 'Publish') {
        $publishBody=@{draft=$false;prerelease=$true;make_latest='false'} | ConvertTo-Json
        $release=Invoke-RestMethod -Uri ($api+'/releases/'+$release.id) -Headers $headers -Method Patch -ContentType 'application/json' -Body $publishBody
        if ($release.draft) { throw 'GitHub did not publish the prepared preview' }
    }
    $receiptRoot=Join-Path $projectRoot ('Evidence/Evolution/github-release-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $receiptRoot | Out-Null
    $receipt=@{success=$true;mode=$Mode;releaseId=$release.id;url=$release.html_url;draft=$release.draft;tag=$release.tag_name;commit=$Commit;assets=@($release.assets | Select-Object name,size,digest,state,browser_download_url)}
    $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $receiptRoot 'result.json') -Encoding utf8
    $receipt | ConvertTo-Json -Depth 6
} finally {
    $headers.Remove('Authorization');$credentials.Clear();$credentialReply=$null;$credentialRequest=$null
}
exit 0
