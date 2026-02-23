param(
    [string]$DestinationRoot = "C:\Program Files\Nick Lavoie\PerformanceOverlay",

    [string]$Repository = "nicklavoie/steam-deck-tools",
    [string]$WorkflowFile = "build_gamebar_overlay.yaml",
    [string]$ArtifactNamePattern = "NickGameBar-*.zip",
    [string]$Ref = "main",
    [string]$GitHubToken,
    [switch]$CleanDestination
)

$ErrorActionPreference = 'Stop'

function Get-AuthHeaders {
    param([string]$Token)

    $headers = @{
        'Accept' = 'application/vnd.github+json'
        'User-Agent' = 'steam-deck-tools-gamebar-installer-script'
        'X-GitHub-Api-Version' = '2022-11-28'
    }

    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }

    return $headers
}

if (-not $GitHubToken -and $env:GITHUB_TOKEN) {
    $GitHubToken = $env:GITHUB_TOKEN
}

$headers = Get-AuthHeaders -Token $GitHubToken

if ($Repository -notmatch '^[^/]+/[^/]+$') {
    throw "Repository must be in 'owner/repo' format."
}

$owner, $repo = $Repository.Split('/')

Write-Host "Looking up latest successful workflow run for $Repository ($WorkflowFile) on ref '$Ref'..."
$runsUrl = "https://api.github.com/repos/$owner/$repo/actions/workflows/$WorkflowFile/runs?status=success&per_page=20&branch=$Ref"
$runsResponse = Invoke-RestMethod -Uri $runsUrl -Headers $headers -Method Get

$run = $runsResponse.workflow_runs |
    Where-Object { $_.conclusion -eq 'success' -and $_.status -eq 'completed' } |
    Select-Object -First 1

if (-not $run) {
    throw "No successful completed workflow runs found for $WorkflowFile on branch '$Ref'."
}

Write-Host "Using workflow run #$($run.run_number) (id: $($run.id))."

$artifactsUrl = "https://api.github.com/repos/$owner/$repo/actions/runs/$($run.id)/artifacts?per_page=100"
$artifactsResponse = Invoke-RestMethod -Uri $artifactsUrl -Headers $headers -Method Get

$artifact = $artifactsResponse.artifacts |
    Where-Object { -not $_.expired -and $_.name -like $ArtifactNamePattern } |
    Sort-Object created_at -Descending |
    Select-Object -First 1

if (-not $artifact) {
    $available = ($artifactsResponse.artifacts | ForEach-Object { $_.name }) -join ', '
    throw "No non-expired artifact matching '$ArtifactNamePattern' found in run $($run.id). Available artifacts: $available"
}

Write-Host "Selected artifact: $($artifact.name)"

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("sdt-gamebar-install-" + [System.Guid]::NewGuid().ToString('N'))
$artifactZip = Join-Path $tempRoot "artifact_container.zip"
$artifactExtractDir = Join-Path $tempRoot "artifact_container"
$payloadExtractDir = Join-Path $tempRoot "payload"

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $encodedArtifactName = [uri]::EscapeDataString($artifact.name)
    $downloadUrl = "https://nightly.link/$owner/$repo/actions/runs/$($run.id)/$encodedArtifactName.zip"

    Write-Host "Downloading artifact from nightly.link..."
    Invoke-WebRequest -Uri $downloadUrl -OutFile $artifactZip -UseBasicParsing

    Write-Host "Extracting artifact container..."
    Expand-Archive -Path $artifactZip -DestinationPath $artifactExtractDir -Force

    $innerZip = Get-ChildItem -Path $artifactExtractDir -Filter *.zip -File | Select-Object -First 1
    if (-not $innerZip) {
        throw "Expected an inner zip inside artifact container, but none was found."
    }

    Write-Host "Extracting payload zip: $($innerZip.Name)"
    Expand-Archive -Path $innerZip.FullName -DestinationPath $payloadExtractDir -Force

    $telemetryHostPayload = Get-ChildItem -Path $payloadExtractDir -Recurse -Directory |
        Where-Object { $_.FullName -like '*Program Files*Nick Lavoie*PerformanceOverlay*NickGameBar.TelemetryHost' } |
        Select-Object -First 1

    if (-not $telemetryHostPayload) {
        throw "Telemetry host payload path not found in artifact."
    }

    if (-not (Test-Path -LiteralPath $DestinationRoot)) {
        New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    }

    if ($CleanDestination) {
        Write-Host "Cleaning destination directory: $DestinationRoot"
        Get-ChildItem -LiteralPath $DestinationRoot -Force | Remove-Item -Recurse -Force
    }

    $destinationHostDir = Join-Path $DestinationRoot 'NickGameBar.TelemetryHost'
    if (-not (Test-Path -LiteralPath $destinationHostDir)) {
        New-Item -ItemType Directory -Path $destinationHostDir -Force | Out-Null
    }

    Write-Host "Copying telemetry host files to: $destinationHostDir"
    Copy-Item -Path (Join-Path $telemetryHostPayload.FullName '*') -Destination $destinationHostDir -Recurse -Force

    Write-Host "Done. Telemetry host installed to '$destinationHostDir'."
    Write-Host "Widget app package files remain in the artifact under AppPackages for manual sideload."
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
