param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [switch]$ForceReinstall
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PackagePath)) {
    throw "PackagePath not found: $PackagePath"
}

$tempExtractDir = $null

try {
    $packageItem = Get-Item -LiteralPath $PackagePath
    if (-not $packageItem.PSIsContainer -and $packageItem.Extension -eq ".zip") {
        $tempExtractDir = Join-Path ([System.IO.Path]::GetTempPath()) ("sdt-widget-install-" + [System.Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $tempExtractDir -Force | Out-Null
        Expand-Archive -Path $packageItem.FullName -DestinationPath $tempExtractDir -Force
        $packageItem = Get-Item -LiteralPath $tempExtractDir
    }

    if ($packageItem.PSIsContainer) {
        $packageItem = Get-ChildItem -LiteralPath $packageItem.FullName -Recurse -File |
            Where-Object { $_.Extension -in ".msix", ".appx", ".msixbundle", ".appxbundle" } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
    }

    if (-not $packageItem) {
        throw "No .msix/.appx package was found under '$PackagePath'."
    }

    $identityName = "SteamDeckToolsGameBarWidget"
    $installedPackage = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue

    if ($ForceReinstall -and $installedPackage) {
        Write-Host "Removing existing package: $($installedPackage.PackageFullName)"
        Remove-AppxPackage -Package $installedPackage.PackageFullName
    }

    Write-Host "Installing package: $($packageItem.FullName)"

    try {
        Add-AppxPackage `
            -Path $packageItem.FullName `
            -ForceUpdateFromAnyVersion `
            -ForceApplicationShutdown `
            -AllowUnsigned
    }
    catch {
        Write-Host ""
        Write-Host "Install failed. If this is an unsigned package, enable Developer Mode in Windows:"
        Write-Host "Settings > Privacy & security > For developers > Developer Mode"
        throw
    }

    Write-Host "Installed. Open Xbox Game Bar (Win+G), click Widget Menu, then add 'Performance Overlay Control'."
}
finally {
    if ($tempExtractDir -and (Test-Path -LiteralPath $tempExtractDir)) {
        Remove-Item -LiteralPath $tempExtractDir -Recurse -Force
    }
}
