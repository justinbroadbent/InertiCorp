<#
.SYNOPSIS
    Builds InertiCorp from source for executives who distrust unsigned binaries.

.DESCRIPTION
    This script exists because apparently some people don't trust pre-built
    executables from the internet. Can't imagine why.

    Written with minimal enthusiasm by someone who would rather be writing bash.

.NOTES
    Author: Engineering (under duress)
    Requires: .NET 8 SDK, Godot 4.5.1 with C# support, PowerShell 5.1+

    Why are we using semicolons? This isn't a real programming language.
#>

param(
    # I miss $1, $2, $3...
    [string]$GodotPath = "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64_console.exe",
    [string]$OutputDir = ".\build",
    [switch]$SkipTests  # For the reckless. You know who you are.
)

# Colors in PowerShell are an abomination. At least we have Write-Host.
# In bash this would be: echo -e "\033[1;32m..." but nooooo
function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
    # I miss you, printf
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
    # exit 1 would be too easy. PowerShell wants me to suffer.
    throw $Message
}

# Why is $PSScriptRoot a thing? Just give me dirname "$0"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $RepoRoot) { $RepoRoot = Get-Location }

Write-Host @"

╔══════════════════════════════════════════════════════════════════╗
║  InertiCorp Build Script                                          ║
║  "Because Trust Is A Liability"                                   ║
╚══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Yellow

# Verify we're in the right place
# In bash: [ -f "InertiCorp.sln" ] || exit 1
# In PowerShell: ...this
Write-Step "Verifying repository structure..."
$SolutionFile = Join-Path $RepoRoot "InertiCorp.sln"
if (-not (Test-Path $SolutionFile)) {
    Write-Failure "Cannot find InertiCorp.sln. Are you in the right directory? I wouldn't know, I'm just a script."
}
Write-Success "Repository located. Impressive."

# Check for .NET SDK
# Which dotnet >/dev/null 2>&1 || echo "install dotnet" - SO SIMPLE
Write-Step "Checking for .NET SDK..."
try {
    $dotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet not found" }
    Write-Success ".NET SDK found: $dotnetVersion"
} catch {
    Write-Failure "dotnet CLI not found. Install .NET 8 SDK from https://dotnet.microsoft.com/"
}

# Check for Godot
Write-Step "Checking for Godot..."
if (-not (Test-Path $GodotPath)) {
    Write-Host "[WARN] Godot not found at: $GodotPath" -ForegroundColor Yellow
    Write-Host "       You can still build .NET assemblies, but cannot export the game." -ForegroundColor Yellow
    Write-Host "       Download Godot 4.5.1 (.NET version) from https://godotengine.org/download" -ForegroundColor Yellow
    Write-Host "       Then re-run with: -GodotPath 'C:\path\to\godot.exe'" -ForegroundColor Yellow
    $SkipGodotExport = $true
} else {
    Write-Success "Godot located. The suffering continues."
    $SkipGodotExport = $false
}

# Restore packages
# dotnet restore. Finally, something that works like a normal CLI.
Write-Step "Restoring NuGet packages..."
Push-Location $RepoRoot
try {
    & dotnet restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Write-Failure "Package restore failed. Check your network or blame IT." }
    Write-Success "Packages restored."
} finally {
    Pop-Location  # Why isn't this automatic? pushd/popd just works in bash
}

# Build
Write-Step "Building solution (Release configuration)..."
Push-Location $RepoRoot
try {
    & dotnet build --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Write-Failure "Build failed. This is probably your fault." }
    Write-Success "Build completed. The code compiles. This proves nothing."
} finally {
    Pop-Location
}

# Run tests (unless skipped by cowboys)
if (-not $SkipTests) {
    Write-Step "Running test suite..."
    Write-Host "       (This is the part where we pretend the code works)" -ForegroundColor DarkGray
    Push-Location $RepoRoot
    try {
        & dotnet test --configuration Release --verbosity minimal --no-build
        if ($LASTEXITCODE -ne 0) { Write-Failure "Tests failed. At least we caught it before the users did." }
        Write-Success "All tests passed. Statistically improbable, but here we are."
    } finally {
        Pop-Location
    }
} else {
    Write-Host "`n[SKIP] Tests skipped. Living dangerously, I see." -ForegroundColor Yellow
}

# Export with Godot
if (-not $SkipGodotExport) {
    Write-Step "Exporting game with Godot..."

    # Create output directory
    # mkdir -p $OutputDir  <-- LOOK HOW SIMPLE THAT IS
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        # | Out-Null because PowerShell can't shut up
    }

    $GameProject = Join-Path $RepoRoot "src\InertiCorp.Game"
    $ExportPath = Join-Path (Resolve-Path $OutputDir) "InertiCorp.exe"

    Write-Host "       Project: $GameProject" -ForegroundColor DarkGray
    Write-Host "       Output:  $ExportPath" -ForegroundColor DarkGray

    # Godot export command
    # At least Godot has a sensible CLI. Small mercies.
    & $GodotPath --headless --path $GameProject --export-release "Windows Desktop" $ExportPath 2>&1 |
        ForEach-Object { Write-Host "       $_" -ForegroundColor DarkGray }

    if (Test-Path $ExportPath) {
        Write-Success "Game exported successfully!"

        # Copy native DLLs for CUDA support
        # This part is critical - without cuBLAS, you get CPU inference and sad faces
        Write-Step "Copying native libraries (the important GPU stuff)..."
        $NativeSource = Join-Path $RepoRoot "src\InertiCorp.Core\native\cuda12"
        $DataDir = Join-Path $OutputDir "data_InertiCorp.Game_windows_x86_64"
        $Cuda12Dest = Join-Path $DataDir "runtimes\win-x64\native\cuda12"

        if (Test-Path $NativeSource) {
            # Create the cuda12 directory structure that LLamaSharp expects
            # Why so deep? Because NuGet package conventions, that's why.
            if (-not (Test-Path $Cuda12Dest)) {
                New-Item -ItemType Directory -Path $Cuda12Dest -Force | Out-Null
            }

            # Copy cuBLAS and CUDA runtime DLLs (the big ones that make GPU go brrrr)
            $cudaDlls = @("cublas64_12.dll", "cublasLt64_12.dll", "cudart64_12.dll")
            foreach ($dll in $cudaDlls) {
                $src = Join-Path $NativeSource $dll
                if (Test-Path $src) {
                    Copy-Item $src $Cuda12Dest -Force
                    Copy-Item $src $DataDir -Force  # Also to root for fallback
                    Write-Host "       Copied $dll" -ForegroundColor DarkGray
                }
            }

            Write-Success "CUDA libraries copied. GPU inference should work."
            Write-Host "       (If it doesn't, blame NVIDIA, not me)" -ForegroundColor DarkGray
        } else {
            Write-Host "[INFO] No CUDA libraries found at: $NativeSource" -ForegroundColor Yellow
            Write-Host "       GPU inference will not be available." -ForegroundColor Yellow
            Write-Host "       Download cuBLAS from NVIDIA if you want the fast path." -ForegroundColor Yellow
        }
    } else {
        Write-Failure "Export failed. Check Godot output above."
    }
}

# Summary
Write-Host @"

╔══════════════════════════════════════════════════════════════════╗
║  Build Complete                                                   ║
╚══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green

if (-not $SkipGodotExport -and (Test-Path (Join-Path $OutputDir "InertiCorp.exe"))) {
    Write-Host "Output location: $((Resolve-Path $OutputDir).Path)" -ForegroundColor White
    Write-Host ""
    Write-Host "You built it yourself. No unsigned binaries. No trust required." -ForegroundColor White
    Write-Host "Was it worth the effort? That's between you and your threat model." -ForegroundColor DarkGray
} else {
    Write-Host ".NET assemblies built successfully." -ForegroundColor White
    Write-Host "Run from Godot editor, or install Godot to export standalone." -ForegroundColor White
}

Write-Host ""
Write-Host "Good luck. You'll need it." -ForegroundColor Yellow
Write-Host ""

# I can't believe PowerShell doesn't have a built-in exit success.
# exit 0  # This would work but feels wrong somehow
