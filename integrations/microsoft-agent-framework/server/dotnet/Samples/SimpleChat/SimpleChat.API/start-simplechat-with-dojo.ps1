#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts SimpleChat.API with GitHub Models token and launches the AG-UI dojo
.DESCRIPTION
    This script ensures SimpleChat.API has a valid GitHub token configured,
    launches the .NET API server on port 5018, and starts the dojo frontend.
.EXAMPLE
    .\start-simplechat-with-dojo.ps1
#>

param(
    [string]$Port = "5018",
    [switch]$SkipTokenCheck
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }

Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  SimpleChat.API + Dojo Launcher" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProjectPath = $scriptPath
$dojoPath = Join-Path (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptPath))))) "typescript-sdk"

Write-Info "API Project: $apiProjectPath"
Write-Info "Dojo Path: $dojoPath"

# Check if gh CLI is available
$ghAvailable = $null -ne (Get-Command gh -ErrorAction SilentlyContinue)

# Step 1: Check and configure GitHub token
if (-not $SkipTokenCheck) {
    Write-Host "`n--- Checking GitHub Token Configuration ---" -ForegroundColor Yellow
    
    # Try to get existing token from user secrets
    Push-Location $apiProjectPath
    try {
        $existingToken = dotnet user-secrets list 2>$null | Select-String "GitHubToken" | ForEach-Object { $_.ToString().Split('=')[1].Trim() }
        
        if ($existingToken) {
            Write-Success "GitHub token found in user secrets"
            $useExisting = Read-Host "Use existing token? (Y/n)"
            if ($useExisting -eq "" -or $useExisting -eq "Y" -or $useExisting -eq "y") {
                Write-Success "Using existing GitHub token"
            } else {
                $existingToken = $null
            }
        }
        
        if (-not $existingToken) {
            Write-Info "No GitHub token configured in user secrets"
            
            # Try to get token from gh CLI
            if ($ghAvailable) {
                Write-Info "Attempting to get token from GitHub CLI (gh)..."
                try {
                    $ghToken = gh auth token 2>$null
                    if ($ghToken) {
                        Write-Success "Found GitHub token from 'gh auth token'"
                        $useGhToken = Read-Host "Use this token? (Y/n)"
                        if ($useGhToken -eq "" -or $useGhToken -eq "Y" -or $useGhToken -eq "y") {
                            Write-Info "Setting GitHub token in user secrets..."
                            dotnet user-secrets set "GitHubToken" $ghToken | Out-Null
                            Write-Success "GitHub token configured successfully"
                        } else {
                            $ghToken = $null
                        }
                    }
                } catch {
                    Write-Warning "Could not get token from gh CLI"
                }
            }
            
            # Manual token entry
            if (-not $ghToken) {
                Write-Host "`nTo use GitHub Models, you need a GitHub Personal Access Token." -ForegroundColor Cyan
                Write-Host "Get one at: https://github.com/settings/tokens" -ForegroundColor Cyan
                Write-Host "Or run: gh auth token" -ForegroundColor Cyan
                Write-Host ""
                
                $manualToken = Read-Host "Enter your GitHub token (or press Enter to skip)"
                if ($manualToken) {
                    Write-Info "Setting GitHub token in user secrets..."
                    dotnet user-secrets set "GitHubToken" $manualToken | Out-Null
                    Write-Success "GitHub token configured successfully"
                } else {
                    Write-Error "No GitHub token provided. The API will not start."
                    Pop-Location
                    exit 1
                }
            }
        }
    } finally {
        Pop-Location
    }
}

# Step 2: Check if ports are available
Write-Host "`n--- Checking Port Availability ---" -ForegroundColor Yellow

$portInUse = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($portInUse) {
    Write-Warning "Port $Port is already in use"
    $killProcess = Read-Host "Kill the process and continue? (Y/n)"
    if ($killProcess -eq "" -or $killProcess -eq "Y" -or $killProcess -eq "y") {
        $portInUse | ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force
            Write-Success "Killed process using port $Port"
        }
        Start-Sleep -Seconds 2
    } else {
        Write-Error "Cannot start API on port $Port"
        exit 1
    }
} else {
    Write-Success "Port $Port is available"
}

# Check dojo port (3000)
$dojoPortInUse = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue
if ($dojoPortInUse) {
    Write-Warning "Dojo port 3000 is already in use"
    $killDojoProcess = Read-Host "Kill the process and continue? (Y/n)"
    if ($killDojoProcess -eq "" -or $killDojoProcess -eq "Y" -or $killDojoProcess -eq "y") {
        $dojoPortInUse | ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force
            Write-Success "Killed process using port 3000"
        }
        Start-Sleep -Seconds 2
    }
}

# Step 3: Build the API project
Write-Host "`n--- Building SimpleChat.API ---" -ForegroundColor Yellow
Push-Location $apiProjectPath
try {
    Write-Info "Running dotnet build..."
    $buildOutput = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build successful"
    } else {
        Write-Error "Build failed:"
        Write-Host $buildOutput -ForegroundColor Red
        Pop-Location
        exit 1
    }
} finally {
    Pop-Location
}

# Step 4: Check if pnpm is available
Write-Host "`n--- Checking Dojo Dependencies ---" -ForegroundColor Yellow
$pnpmAvailable = $null -ne (Get-Command pnpm -ErrorAction SilentlyContinue)
if (-not $pnpmAvailable) {
    Write-Error "pnpm is not installed. Install it with: npm install -g pnpm"
    exit 1
}
Write-Success "pnpm is available"

# Check if node_modules exists in dojo
if (-not (Test-Path (Join-Path $dojoPath "node_modules"))) {
    Write-Warning "Dojo dependencies not installed"
    $installDeps = Read-Host "Install dependencies with 'pnpm install'? (Y/n)"
    if ($installDeps -eq "" -or $installDeps -eq "Y" -or $installDeps -eq "y") {
        Write-Info "Installing dojo dependencies..."
        Push-Location $dojoPath
        try {
            pnpm install
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Dependencies installed"
            } else {
                Write-Error "Failed to install dependencies"
                Pop-Location
                exit 1
            }
        } finally {
            Pop-Location
        }
    }
}

# Step 5: Start the API server in background
Write-Host "`n--- Starting SimpleChat.API ---" -ForegroundColor Yellow
Push-Location $apiProjectPath
try {
    Write-Info "Starting API on http://localhost:$Port ..."
    
    # Start dotnet run in a new window
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --urls http://localhost:$Port" -PassThru -WindowStyle Normal
    
    # Wait for API to start
    Write-Info "Waiting for API to be ready..."
    $maxAttempts = 30
    $attempt = 0
    $apiReady = $false
    
    while ($attempt -lt $maxAttempts -and -not $apiReady) {
        Start-Sleep -Seconds 1
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$Port" -Method POST -ContentType "application/json" -Body '{"threadId":"health-check","runId":"hc-1","messages":[{"id":"hc-msg","role":"user","content":"test"}]}' -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $apiReady = $true
            }
        } catch {
            # Still starting up
        }
        $attempt++
    }
    
    if ($apiReady) {
        Write-Success "SimpleChat.API is running on http://localhost:$Port"
    } else {
        Write-Warning "API may still be starting up..."
    }
} finally {
    Pop-Location
}

# Step 6: Start the dojo
Write-Host "`n--- Starting Dojo ---" -ForegroundColor Yellow
Push-Location $dojoPath
try {
    Write-Info "Starting dojo on http://localhost:3000 ..."
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "  Both services are starting!" -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "SimpleChat.API: " -NoNewline -ForegroundColor Cyan
    Write-Host "http://localhost:$Port" -ForegroundColor Green
    Write-Host "Dojo:           " -NoNewline -ForegroundColor Cyan
    Write-Host "http://localhost:3000" -ForegroundColor Green
    Write-Host ""
    Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Yellow
    Write-Host ""
    
    # Start pnpm dev (this will block until Ctrl+C)
    pnpm dev
    
} catch {
    Write-Error "Error starting dojo: $_"
} finally {
    Pop-Location
    
    # Cleanup: Stop the API process
    Write-Host "`n--- Cleaning up ---" -ForegroundColor Yellow
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Info "Stopping SimpleChat.API..."
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Success "SimpleChat.API stopped"
    }
}

Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  Services stopped" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta
