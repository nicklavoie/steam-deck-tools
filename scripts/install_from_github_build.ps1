param(
    [Parameter(Mandatory = $true)]
    [string]$DestinationDir,

    [string]$Repository = "nicklavoie/steam-deck-tools",
    [string]$WorkflowFile = "build_performance_overlay.yaml",
    [string]$ArtifactNamePattern = "PerformanceOverlay-*.zip",
    [string]$Ref = "main",
    [string]$GitHubToken,
    [switch]$CleanDestination
)

$ErrorActionPreference = 'Stop'

function Get-AuthHeaders {
    param([string]$Token)

    $headers = @{
        'Accept' = 'application/vnd.github+json'
        'User-Agent' = 'steam-deck-tools-installer-script'
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

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("sdt-install-" + [System.Guid]::NewGuid().ToString('N'))
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

    if (-not (Test-Path -LiteralPath $DestinationDir)) {
        New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    }

    if ($CleanDestination) {
        Write-Host "Cleaning destination directory: $DestinationDir"
        Get-ChildItem -LiteralPath $DestinationDir -Force | Remove-Item -Recurse -Force
    }

    Write-Host "Copying payload files to destination: $DestinationDir"
    Copy-Item -Path (Join-Path $payloadExtractDir '*') -Destination $DestinationDir -Recurse -Force

    Write-Host "Done. Updated files in '$DestinationDir' from artifact '$($artifact.name)'."
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
