[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$NumericVersion,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$IsccPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"


function Write-Step
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host ""
    Write-Host $Message -ForegroundColor Cyan
}


function Resolve-RepositoryRoot
{
    $installerDirectory =
        $PSScriptRoot

    $parentDirectory =
        Split-Path `
            -Path $installerDirectory `
            -Parent

    $candidateRoots = @(
        $parentDirectory,
        $installerDirectory
    )

    foreach ($candidate in $candidateRoots)
    {
        $candidateProject =
            Join-Path `
                -Path $candidate `
                -ChildPath "SmartExchanger\SmartExchanger.csproj"

        if (Test-Path `
                -LiteralPath $candidateProject `
                -PathType Leaf)
        {
            return (
                Resolve-Path `
                    -LiteralPath $candidate
            ).Path
        }
    }

    throw @"
Cannot locate the repository root.

Expected project:
SmartExchanger\SmartExchanger.csproj

Place this script in:
Installer\Build-Installer.ps1
"@
}


function Assert-FileExists
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (
        Test-Path `
            -LiteralPath $Path `
            -PathType Leaf))
    {
        throw "$Description does not exist: $Path"
    }
}


function Assert-DirectoryContainsFiles
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Filter,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (
        Test-Path `
            -LiteralPath $Path `
            -PathType Container))
    {
        throw "$Description directory does not exist: $Path"
    }

    $matchingFiles =
        @(
            Get-ChildItem `
                -LiteralPath $Path `
                -Filter $Filter `
                -File `
                -Recurse `
                -ErrorAction Stop
        )

    if ($matchingFiles.Count -eq 0)
    {
        throw @"
$Description directory does not contain files matching '$Filter':

$Path
"@
    }

    Write-Host `
        "  OK: $Description ($($matchingFiles.Count) file(s))" `
        -ForegroundColor Green
}


$repositoryRoot =
    Resolve-RepositoryRoot


$projectPath =
    Join-Path `
        -Path $repositoryRoot `
        -ChildPath "SmartExchanger\SmartExchanger.csproj"


$innoScriptPath =
    Join-Path `
        -Path $repositoryRoot `
        -ChildPath "Installer\SmartExchanger.iss"


$applicationIconPath =
    Join-Path `
        -Path $repositoryRoot `
        -ChildPath "SmartExchanger\Assets\Icons\small-icon.ico"


$artifactsDirectory =
    Join-Path `
        -Path $repositoryRoot `
        -ChildPath "artifacts"


$publishDirectory =
    Join-Path `
        -Path $artifactsDirectory `
        -ChildPath "publish\win-x64"


$installerOutputDirectory =
    Join-Path `
        -Path $artifactsDirectory `
        -ChildPath "installer"


$expectedInstallerName =
    "SmartExchanger-$Version-win-x64-setup.exe"


$expectedInstallerPath =
    Join-Path `
        -Path $installerOutputDirectory `
        -ChildPath $expectedInstallerName


$checksumPath =
    Join-Path `
        -Path $installerOutputDirectory `
        -ChildPath "SHA256SUMS.txt"


Assert-FileExists `
    -Path $projectPath `
    -Description "SmartExchanger project"


Assert-FileExists `
    -Path $innoScriptPath `
    -Description "Inno Setup script"


Assert-FileExists `
    -Path $applicationIconPath `
    -Description "Application icon"


Assert-FileExists `
    -Path $IsccPath `
    -Description "Inno Setup compiler"


$resolvedIsccPath =
    (
        Resolve-Path `
            -LiteralPath $IsccPath
    ).Path


if (
    [System.IO.Path]::GetFileName(
        $resolvedIsccPath
    ) -ine "ISCC.exe"
)
{
    throw @"
The IsccPath argument must point directly to ISCC.exe.

Received:
$resolvedIsccPath
"@
}


Write-Host `
    "Repository root: $repositoryRoot" `
    -ForegroundColor DarkGray

Write-Host `
    "Project:         $projectPath" `
    -ForegroundColor DarkGray

Write-Host `
    "Inno script:     $innoScriptPath" `
    -ForegroundColor DarkGray

Write-Host `
    "ISCC:            $resolvedIsccPath" `
    -ForegroundColor DarkGray

Write-Host `
    "Version:         $Version" `
    -ForegroundColor DarkGray

Write-Host `
    "Numeric version: $NumericVersion" `
    -ForegroundColor DarkGray


Write-Step "Cleaning previous build artifacts..."


if (Test-Path -LiteralPath $publishDirectory)
{
    Remove-Item `
        -LiteralPath $publishDirectory `
        -Recurse `
        -Force
}


if (Test-Path -LiteralPath $installerOutputDirectory)
{
    Remove-Item `
        -LiteralPath $installerOutputDirectory `
        -Recurse `
        -Force
}


New-Item `
    -ItemType Directory `
    -Path $publishDirectory `
    -Force |
    Out-Null


New-Item `
    -ItemType Directory `
    -Path $installerOutputDirectory `
    -Force |
    Out-Null


Write-Step "Publishing SmartExchanger as self-contained win-x64..."


& dotnet publish `
    $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false


if ($LASTEXITCODE -ne 0)
{
    throw @"
dotnet publish failed with exit code $LASTEXITCODE.
"@
}


Write-Step "Validating published files..."


$requiredFiles = @(
    "SmartExchanger.exe",
    "SmartExchanger.dll",
    "SmartExchanger.deps.json",
    "SmartExchanger.runtimeconfig.json",
    "hostfxr.dll",
    "hostpolicy.dll",
    "coreclr.dll",
    "System.Private.CoreLib.dll",
    "PresentationFramework.dll",
    "WindowsBase.dll",
    "appsettings.json"
)


foreach ($file in $requiredFiles)
{
    $filePath =
        Join-Path `
            -Path $publishDirectory `
            -ChildPath $file

    if (-not (
        Test-Path `
            -LiteralPath $filePath `
            -PathType Leaf))
    {
        throw @"
The publication is incomplete.

Missing file:
$file
"@
    }

    Write-Host `
        "  OK: $file" `
        -ForegroundColor Green
}


$runtimeConfigPath =
    Join-Path `
        -Path $publishDirectory `
        -ChildPath "SmartExchanger.runtimeconfig.json"


$runtimeConfig =
    Get-Content `
        -LiteralPath $runtimeConfigPath `
        -Raw |
    ConvertFrom-Json


if ($null -eq $runtimeConfig.runtimeOptions)
{
    throw @"
runtimeconfig.json does not contain runtimeOptions.
"@
}


$includedFrameworksProperty =
    $runtimeConfig.runtimeOptions.PSObject.Properties[
        "includedFrameworks"
    ]


if ($null -eq $includedFrameworksProperty)
{
    throw @"
runtimeconfig.json does not contain includedFrameworks.

The publication does not appear to be self-contained.
"@
}


$includedFrameworks =
    @(
        $includedFrameworksProperty.Value
    )


if ($includedFrameworks.Count -eq 0)
{
    throw @"
runtimeconfig.json contains an empty includedFrameworks collection.

The publication does not appear to be self-contained.
"@
}


$includedFrameworkNames = @()

foreach ($includedFramework in $includedFrameworks)
{
    $includedFrameworkNames +=
        [string]$includedFramework.name
}


$requiredFrameworks = @(
    "Microsoft.NETCore.App",
    "Microsoft.WindowsDesktop.App"
)


foreach ($framework in $requiredFrameworks)
{
    if ($includedFrameworkNames -notcontains $framework)
    {
        throw @"
The self-contained publication does not include framework:

$framework
"@
    }

    Write-Host `
        "  OK: included framework $framework" `
        -ForegroundColor Green
}

$shadersDirectory =
    Join-Path `
        -Path $publishDirectory `
        -ChildPath "Shaders"

$compiledHelixShadersDirectory =
    Join-Path `
        -Path $publishDirectory `
        -ChildPath "Shaders\MaterialPreview\Helix\PS"


$environmentMapsDirectory =
    Join-Path `
        -Path $publishDirectory `
        -ChildPath "Assets\EnvironmentMaps"


Assert-DirectoryContainsFiles `
    -Path $shadersDirectory `
    -Filter "*.sksl" `
    -Description "Shaders"

$requiredCompiledShaders = @(
    "psMeshPBRTriplanar.cso",
    "psMeshPBRTriplanarOIT.cso",
    "psMeshPBRTriplanarOITDP.cso"
)

foreach ($shader in $requiredCompiledShaders)
{
    $shaderPath =
        Join-Path `
            -Path $compiledHelixShadersDirectory `
            -ChildPath $shader

    Assert-FileExists `
        -Path $shaderPath `
        -Description "Compiled Helix shader"

    Write-Host `
        "  OK: $shader" `
        -ForegroundColor Green
}


Assert-DirectoryContainsFiles `
    -Path $environmentMapsDirectory `
    -Filter "*.dds" `
    -Description "Environment maps"


Write-Step "Building installer with Inno Setup..."


$isccArguments = @(
    "/DMyAppVersion=$Version",
    "/DMyAppVersionNumeric=$NumericVersion",
    "/DPublishDir=$publishDirectory",
    "/DRepositoryRoot=$repositoryRoot",
    $innoScriptPath
)


& $resolvedIsccPath @isccArguments


if ($LASTEXITCODE -ne 0)
{
    throw @"
Inno Setup failed with exit code $LASTEXITCODE.
"@
}


$installerPath =
    $null


if (
    Test-Path `
        -LiteralPath $expectedInstallerPath `
        -PathType Leaf
)
{
    $installerPath =
        $expectedInstallerPath
}
else
{
    $generatedInstallers =
        @(
            Get-ChildItem `
                -LiteralPath $installerOutputDirectory `
                -Filter "*.exe" `
                -File `
                -ErrorAction SilentlyContinue |
            Sort-Object `
                -Property LastWriteTimeUtc `
                -Descending
        )

    if ($generatedInstallers.Count -gt 0)
    {
        $installerPath =
            $generatedInstallers[0].FullName

        Write-Warning @"
The expected installer name was not found:

$expectedInstallerPath

Using the newest generated installer instead:

$installerPath
"@
    }
}


if ([string]::IsNullOrWhiteSpace($installerPath))
{
    throw @"
Inno Setup completed, but no installer EXE was found in:

$installerOutputDirectory
"@
}


Write-Step "Generating SHA-256 checksum..."


$hash =
    Get-FileHash `
        -LiteralPath $installerPath `
        -Algorithm SHA256


$installerFileName =
    [System.IO.Path]::GetFileName(
        $installerPath
    )


$checksumLine =
    "$($hash.Hash.ToLowerInvariant())  $installerFileName"


$utf8WithoutBom =
    New-Object `
        System.Text.UTF8Encoding `
        $false


[System.IO.File]::WriteAllText(
    $checksumPath,
    $checksumLine + [Environment]::NewLine,
    $utf8WithoutBom
)


$installerInfo =
    Get-Item `
        -LiteralPath $installerPath


$installerSizeMb =
    [Math]::Round(
        $installerInfo.Length / 1MB,
        2
    )


Write-Host ""
Write-Host `
    "Installer created successfully." `
    -ForegroundColor Green

Write-Host `
    "Installer: $installerPath" `
    -ForegroundColor White

Write-Host `
    "Size:      $installerSizeMb MB" `
    -ForegroundColor White

Write-Host `
    "SHA-256:   $($hash.Hash)" `
    -ForegroundColor White

Write-Host `
    "Checksums: $checksumPath" `
    -ForegroundColor White