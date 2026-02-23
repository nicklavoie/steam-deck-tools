param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [switch]$ForceReinstall
)

$ErrorActionPreference = "Stop"
$identityName = "SteamDeckToolsGameBarWidget"
$unsignedPublisherMarker = "OID.2.25.311729368913984317654407730594956997722=1"

if (-not (Test-Path -LiteralPath $PackagePath)) {
    throw "PackagePath not found: $PackagePath"
}

$tempExtractDir = $null
$dependencyPackages = @()

function Get-PackagePublisher {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $manifestEntry = $zip.GetEntry("AppxManifest.xml")
        if (-not $manifestEntry) {
            return $null
        }

        $stream = $manifestEntry.Open()
        $reader = New-Object System.IO.StreamReader($stream)
        try {
            [xml]$manifestXml = $reader.ReadToEnd()
            return $manifestXml.Package.Identity.Publisher
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }
}

try {
    $packageItem = Get-Item -LiteralPath $PackagePath
    if (-not $packageItem.PSIsContainer -and $packageItem.Extension -eq ".zip") {
        $tempExtractDir = Join-Path ([System.IO.Path]::GetTempPath()) ("sdt-widget-install-" + [System.Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $tempExtractDir -Force | Out-Null
        Expand-Archive -Path $packageItem.FullName -DestinationPath $tempExtractDir -Force
        $packageItem = Get-Item -LiteralPath $tempExtractDir
    }

    if ($packageItem.PSIsContainer) {
        $allPackages = Get-ChildItem -LiteralPath $packageItem.FullName -Recurse -File |
            Where-Object { $_.Extension -in ".msix", ".appx", ".msixbundle", ".appxbundle" }

        $packageItem = $allPackages |
            Where-Object { $_.BaseName -eq $identityName -or $_.BaseName -like "$identityName_*" } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $packageItem) {
            $packageItem = $allPackages |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1
        }

        $dependencyPackages = $allPackages |
            Where-Object { $_.FullName -match "[\\/]+Dependencies[\\/]+" -and $_.Extension -in ".msix", ".appx" } |
            Sort-Object FullName |
            Select-Object -ExpandProperty FullName
    }
    else {
        $dependenciesFolder = Join-Path $packageItem.Directory.FullName "Dependencies"
        if (Test-Path -LiteralPath $dependenciesFolder) {
            $dependencyPackages = Get-ChildItem -LiteralPath $dependenciesFolder -Recurse -File |
                Where-Object { $_.Extension -in ".msix", ".appx" } |
                Sort-Object FullName |
                Select-Object -ExpandProperty FullName
        }
    }

    if (-not $packageItem) {
        throw "No .msix/.appx package was found under '$PackagePath'."
    }

    $installedPackage = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue

    if ($ForceReinstall -and $installedPackage) {
        Write-Host "Removing existing package: $($installedPackage.PackageFullName)"
        Remove-AppxPackage -Package $installedPackage.PackageFullName
    }

    $publisher = Get-PackagePublisher -Path $packageItem.FullName
    if ($publisher -and -not $publisher.Contains($unsignedPublisherMarker)) {
        throw "Selected package '$($packageItem.Name)' has publisher '$publisher', which is not in the unsigned namespace. Make sure you picked the SteamDeckToolsGameBarWidget package file."
    }

    Write-Host "Installing package: $($packageItem.FullName)"

    try {
        $addPackageArgs = @{
            Path = $packageItem.FullName
            ForceUpdateFromAnyVersion = $true
            ForceApplicationShutdown = $true
            AllowUnsigned = $true
        }

        if ($dependencyPackages.Count -gt 0) {
            $addPackageArgs["DependencyPath"] = $dependencyPackages
            Write-Host "Using dependency packages:"
            $dependencyPackages | ForEach-Object { Write-Host "  $_" }
        }

        Add-AppxPackage @addPackageArgs
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
