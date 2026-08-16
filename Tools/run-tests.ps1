# run-tests.ps1 - headless Unity Test Framework runner for GridGame2026.
# (ASCII only: PowerShell 5.1 reads BOM-less files as ANSI, so non-ASCII chars corrupt the parse.)
#
# Runs the EditMode or PlayMode suite via Unity's native -runTests CLI and gates on THREE
# signals (any one alone can lie):
#   1. results XML exists and contains zero <test-case result="Failed">
#   2. the log contains zero "error CS" lines (a compile failure can make -runTests exit 0
#      having run NOTHING - the classic false-positive trap)
#   3. Unity's process exit code
#
# PlayMode runs must NOT pass -nographics (Unity needs a display context for play mode);
# EditMode runs use -nographics for speed. The Editor must be CLOSED (project lock).
#
# Usage:
#   powershell -File tools\run-tests.ps1 -Platform EditMode
#   powershell -File tools\run-tests.ps1 -Platform PlayMode
#   powershell -File tools\run-tests.ps1 -Platform PlayMode -Filter "Scripts.Tests.PlayMode.SceneBootSmokeTests"
#
# Exit codes: 0 = suite green, 1 = failures/compile errors, 2 = environment problem.

param(
    [Parameter(Mandatory = $true)][ValidateSet("EditMode", "PlayMode")][string]$Platform,
    [string]$Filter = "",
    # Visible: run WITHOUT -batchmode (editor GUI opens, tests run, editor exits).
    # The batchmode-with-graphics hybrid PlayMode mode wedges intermittently on this
    # machine (frozen main loop after a scene load); a real GUI message pump does not.
    [switch]$Visible
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

# --- Locate the exact editor version (authoritative: ProjectVersion.txt) ---
$verLine = Get-Content (Join-Path $root "ProjectSettings\ProjectVersion.txt") | Select-Object -First 1
$editorVersion = ($verLine -replace '^m_EditorVersion:\s*', '').Trim()
$unityEditor = "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
if (-not (Test-Path $unityEditor)) {
    Write-Host "[run-tests] Unity $editorVersion not found at $unityEditor" -ForegroundColor Red
    exit 2
}

# --- Refuse to run if an Editor holds the project lock ---
$lockfile = Join-Path $root "Temp\UnityLockfile"
if (Test-Path $lockfile) {
    try {
        $fs = [System.IO.File]::Open($lockfile, 'Open', 'ReadWrite', 'None')
        $fs.Close()
    } catch {
        Write-Host "[run-tests] Unity Editor is running on this project (lockfile held). Close it first." -ForegroundColor Red
        exit 2
    }
}

# --- Compose the run ---
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logsDir = Join-Path $root "Logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
$resultsPath = Join-Path $logsDir "$($Platform.ToLower())-results-$stamp.xml"
$logPath = Join-Path $logsDir "$($Platform.ToLower())-run-$stamp.log"

$unityArgs = @(
    "-projectPath", "`"$root`"",
    "-runTests",
    "-testPlatform", $Platform,
    "-testResults", "`"$resultsPath`"",
    "-logFile", "`"$logPath`""
)
if (-not $Visible) { $unityArgs = @("-batchmode") + $unityArgs }
if ($Platform -eq "EditMode" -and -not $Visible) { $unityArgs = @("-nographics") + $unityArgs }
if ($Filter -ne "") { $unityArgs += @("-testFilter", "`"$Filter`"") }

Write-Host "[run-tests] $Platform suite starting (editor $editorVersion)"
Write-Host "[run-tests] results: $resultsPath"
Write-Host "[run-tests] log:     $logPath"

$proc = Start-Process -FilePath $unityEditor -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow
$exitCode = $proc.ExitCode

# --- Gate 1: compile errors mask as "0 tests, exit 0" - always check the log first ---
$compileErrors = @()
if (Test-Path $logPath) {
    $compileErrors = @(Select-String -Path $logPath -Pattern "error CS\d+" | Select-Object -First 20)
}
if ($compileErrors.Count -gt 0) {
    Write-Host "[run-tests] COMPILE ERRORS ($($compileErrors.Count) shown, may be more):" -ForegroundColor Red
    $compileErrors | ForEach-Object { Write-Host "    $($_.Line.Trim())" -ForegroundColor Red }
    exit 1
}

# --- Gate 2: results XML must exist and contain zero failures ---
if (-not (Test-Path $resultsPath)) {
    Write-Host "[run-tests] No results XML produced (exit $exitCode). Test run never happened - check the log." -ForegroundColor Red
    exit 1
}
$xml = [xml](Get-Content $resultsPath)
$allCases = $xml.SelectNodes("//test-case")
$failed = $xml.SelectNodes("//test-case[@result='Failed']")
$passedCount = ($xml.SelectNodes("//test-case[@result='Passed']")).Count

Write-Host "[run-tests] $($allCases.Count) tests: $passedCount passed, $($failed.Count) failed (unity exit $exitCode)"

if ($failed.Count -gt 0) {
    Write-Host "[run-tests] FAILED tests:" -ForegroundColor Red
    foreach ($case in $failed) {
        Write-Host "    $($case.fullname)" -ForegroundColor Red
        $msg = $case.SelectSingleNode(".//failure/message")
        if ($null -ne $msg) {
            $text = $msg.InnerText.Trim()
            if ($text.Length -gt 600) { $text = $text.Substring(0, 600) + " ..." }
            Write-Host "      $text" -ForegroundColor DarkYellow
        }
    }
    exit 1
}

if ($allCases.Count -eq 0) {
    Write-Host "[run-tests] Zero tests discovered - treat as FAILURE (filter typo or assembly problem)." -ForegroundColor Red
    exit 1
}

Write-Host "[run-tests] GREEN." -ForegroundColor Green
exit 0
