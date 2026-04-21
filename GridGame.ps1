$root = $PSScriptRoot
$backupBase = "R:\Backup\GridGame"
$repoUrl = "https://github.com/mindattic/GridGame2026.git"
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Header($title) {
    Clear-Host
    Write-Host ""
    Write-Host "  $title" -ForegroundColor Cyan
    Write-Host "  $('=' * $title.Length)" -ForegroundColor DarkCyan
    Write-Host ""
}

function Read-MenuChoice {
    $val = Read-Host "  >"
    $val = $val.Trim()
    if ($val -eq "") { return "" }
    return $val
}

function Wait-ForEnter {
    Write-Host ""
    Write-Host "  Press any key to continue..." -ForegroundColor DarkGray
    $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
}

function Get-GitStatus {
    $changes = git -C $root status --porcelain 2>$null
    if ($changes) { return $changes.Count } else { return 0 }
}

function Get-BackupFolder {
    $date = Get-Date -Format "yyyy-MM-dd"
    $folder = Join-Path $backupBase $date
    if (-not (Test-Path $folder)) { return $folder }
    foreach ($i in 97..122) {
        $c = [char]$i
        $candidate = Join-Path $backupBase "${date}${c}"
        if (-not (Test-Path $candidate)) { return $candidate }
    }
    return Join-Path $backupBase "${date}z"
}

# ── 1. Run Application ──────────────────────────────────────────────────────

function Invoke-Run {
    Write-Header "GridGame Run"

    if (-not (Test-Path $unityEditor)) {
        Write-Host "  Unity editor not found at:" -ForegroundColor Red
        Write-Host "  $unityEditor" -ForegroundColor Red
        Wait-ForEnter
        return
    }

    Write-Host "  Launching Unity editor..." -ForegroundColor Green
    Start-Process $unityEditor -ArgumentList "-projectPath `"$root`""

    Wait-ForEnter
}

# ── 2. Commit and Sync ──────────────────────────────────────────────────────

function Invoke-Commit {
    Write-Header "GridGame Commit & Sync"

    $count = Get-GitStatus
    if ($count -gt 0) {
        Write-Host "  $count file(s) changed:" -ForegroundColor Yellow
        Write-Host ""
        git -C $root status --short 2>$null | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
        Write-Host ""
    } else {
        Write-Host "  Nothing to commit." -ForegroundColor DarkGray
        Wait-ForEnter
        return
    }

    $message = Read-Host "  Commit message"
    if ([string]::IsNullOrWhiteSpace($message)) {
        Write-Host "  Aborted (empty message)." -ForegroundColor Red
        Wait-ForEnter
        return
    }

    Write-Host ""
    Write-Host "  Staging $count file(s)..." -NoNewline -ForegroundColor White
    git -C $root add -A 2>$null
    Write-Host " done" -ForegroundColor Green

    Write-Host "  Committing..." -NoNewline -ForegroundColor White
    git -C $root commit -m $message 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host " commit failed!" -ForegroundColor Red
        Wait-ForEnter
        return
    }
    Write-Host " committed" -ForegroundColor Green

    Write-Host "  Pushing..." -NoNewline -ForegroundColor White
    git -C $root push 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host " push failed!" -ForegroundColor Red
        Wait-ForEnter
        return
    }
    Write-Host " synced" -ForegroundColor Green

    Write-Host ""
    Write-Host "  Done." -ForegroundColor Green
    Wait-ForEnter
}

# ── 3. Create Backup ────────────────────────────────────────────────────────

function Invoke-Backup {
    $dest = Get-BackupFolder
    Write-Header "GridGame Backup"
    Write-Host "  Destination: " -NoNewline -ForegroundColor DarkGray
    Write-Host $dest -ForegroundColor White
    Write-Host ""
    Write-Host "  [1]  Start backup" -ForegroundColor White
    Write-Host ""
    Write-Host "  [0]  Back" -ForegroundColor DarkGray
    Write-Host ""

    $choice = Read-MenuChoice
    if ($choice -ne "1") { return }

    $dest = Get-BackupFolder
    Write-Host ""
    Write-Host "  Backing up project to:" -ForegroundColor Yellow
    Write-Host "  $dest" -ForegroundColor White
    Write-Host ""

    New-Item -ItemType Directory -Path $dest -Force | Out-Null

    Write-Host "  Copying project..." -NoNewline -ForegroundColor White
    Copy-Item $root $dest -Recurse -Force -Exclude @("Library", "Temp", "obj", "Logs")
    Write-Host " done" -ForegroundColor Green

    Write-Host ""
    Write-Host "  Backup complete." -ForegroundColor Green
    Wait-ForEnter
}

# ── 4. Setup ────────────────────────────────────────────────────────────────

function Invoke-Setup {
    Write-Header "GridGame Setup"

    # Clone or pull
    if (Test-Path (Join-Path $root ".git")) {
        Write-Host "  Repository exists, pulling..." -NoNewline -ForegroundColor DarkGray
        git -C $root pull --ff-only 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host " up to date" -ForegroundColor Green
        } else {
            Write-Host " pull failed (resolve manually)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  Cloning repository..." -NoNewline -ForegroundColor White
        git clone $repoUrl $root 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host " done" -ForegroundColor Green
        } else {
            Write-Host " FAILED" -ForegroundColor Red
            Wait-ForEnter
            return
        }
    }

    # Open Unity to trigger initial import/build
    Write-Host ""
    if (Test-Path $unityEditor) {
        Write-Host "  Launching Unity for initial project import..." -ForegroundColor Yellow
        Start-Process $unityEditor -ArgumentList "-projectPath `"$root`""
        Write-Host "  Unity is importing the project in the background." -ForegroundColor Green
    } else {
        Write-Host "  Unity editor not found at:" -ForegroundColor Red
        Write-Host "  $unityEditor" -ForegroundColor Red
        Write-Host "  Install Unity 6000.3.2f1 via Unity Hub." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "  Setup complete." -ForegroundColor Green
    Wait-ForEnter
}

# ── 5. Scaffold all scenes (headless) ──────────────────────────────────────

function Invoke-ScaffoldAll {
    Invoke-Batchmode "Scaffold All Scenes" "CliEntryPoints.ScaffoldAllScenes"
}

# ── 6. Generate documentation (headless) ───────────────────────────────────

function Invoke-GenerateDocs {
    Invoke-Batchmode "Generate Documentation" "CliEntryPoints.GenerateDocs"
}

# ── 7. Run tests (headless) ────────────────────────────────────────────────

function Invoke-RunTests {
    Invoke-Batchmode "Run Edit Tests" "CliEntryPoints.RunEditTests"
}

# ── 8. Build player (headless) ─────────────────────────────────────────────

function Invoke-BuildPlayer {
    Invoke-Batchmode "Build Standalone Windows" "CliEntryPoints.BuildStandaloneWindows"
}

# ── 9. Check [SerializeField] ban (headless) ───────────────────────────────

function Invoke-CheckFieldBan {
    Invoke-Batchmode "Check [SerializeField] Ban" "CliEntryPoints.CheckSerializedFieldBan"
}

# ── 10. Regenerate [SerializeField] allowlist (headless) ───────────────────

function Invoke-RegenFieldAllowlist {
    Invoke-Batchmode "Regenerate [SerializeField] Allowlist" "CliEntryPoints.RegenerateSerializedFieldAllowlist"
}

# ── 11. Verify scaffold drift (headless) ───────────────────────────────────

function Invoke-VerifyDrift {
    Invoke-Batchmode "Verify Scaffold Drift" "CliEntryPoints.VerifyScaffoldDrift"
}

# ── 12. Regenerate scaffold snapshots (headless) ───────────────────────────

function Invoke-RegenSnapshots {
    Invoke-Batchmode "Regenerate Scaffold Snapshots" "CliEntryPoints.RegenerateScaffoldSnapshots"
}

# ── 13. Check Resources.Load ban (headless) ────────────────────────────────

function Invoke-CheckResourcesLoadBan {
    Invoke-Batchmode "Check Resources.Load Ban" "CliEntryPoints.CheckResourcesLoadBan"
}

# ── 14. Regenerate Resources.Load allowlist (headless) ─────────────────────

function Invoke-RegenResourcesLoadAllowlist {
    Invoke-Batchmode "Regenerate Resources.Load Allowlist" "CliEntryPoints.RegenerateResourcesLoadAllowlist"
}

# ── 15. Check Instantiate ban (headless) ───────────────────────────────────

function Invoke-CheckInstantiateBan {
    Invoke-Batchmode "Check Instantiate Ban" "CliEntryPoints.CheckInstantiateBan"
}

# ── 16. Regenerate Instantiate allowlist (headless) ────────────────────────

function Invoke-RegenInstantiateAllowlist {
    Invoke-Batchmode "Regenerate Instantiate Allowlist" "CliEntryPoints.RegenerateInstantiateAllowlist"
}

# ── 17. Save scene scaffolds (scene → code, headless) ──────────────────────

function Invoke-SaveSceneScaffolds {
    Invoke-Batchmode "Save Scene Scaffolds" "CliEntryPoints.SaveSceneScaffolds"
}

# ── 18. Check all guardrails (headless, one Unity launch) ──────────────────

function Invoke-CheckAllGuardrails {
    Invoke-Batchmode "Check All Guardrails" "CliEntryPoints.CheckAllGuardrails"
}

# ── Batchmode runner ───────────────────────────────────────────────────────

function Invoke-Batchmode($title, $method) {
    Write-Header "GridGame $title"

    if (-not (Test-Path $unityEditor)) {
        Write-Host "  Unity editor not found at:" -ForegroundColor Red
        Write-Host "  $unityEditor" -ForegroundColor Red
        Wait-ForEnter
        return
    }

    $logFile = Join-Path $root "Logs/cli-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    New-Item -ItemType Directory -Path (Split-Path $logFile) -Force | Out-Null

    Write-Host "  Running: $method" -ForegroundColor Yellow
    Write-Host "  Log:     $logFile" -ForegroundColor DarkGray
    Write-Host ""

    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", "`"$root`"",
        "-executeMethod", $method,
        "-quit",
        "-logFile", "`"$logFile`""
    )

    $proc = Start-Process -FilePath $unityEditor -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow
    if ($proc.ExitCode -eq 0) {
        Write-Host "  $title completed." -ForegroundColor Green
    } else {
        Write-Host "  $title FAILED (exit $($proc.ExitCode)). See log for details." -ForegroundColor Red
    }
    Wait-ForEnter
}

# ── Main Menu ────────────────────────────────────────────────────────────────

$mainMenu = [ordered]@{
    "1"  = @{ Name = "Run Application";                         Action = { Invoke-Run } }
    "2"  = @{ Name = "Commit and Sync";                         Action = { Invoke-Commit } }
    "3"  = @{ Name = "Create Backup";                           Action = { Invoke-Backup } }
    "4"  = @{ Name = "Setup";                                   Action = { Invoke-Setup } }
    "5"  = @{ Name = "Scaffold All Scenes (headless)";          Action = { Invoke-ScaffoldAll } }
    "6"  = @{ Name = "Generate Documentation (headless)";       Action = { Invoke-GenerateDocs } }
    "7"  = @{ Name = "Run Tests (headless)";                    Action = { Invoke-RunTests } }
    "8"  = @{ Name = "Build Player (headless)";                 Action = { Invoke-BuildPlayer } }
    "9"  = @{ Name = "Check [SerializeField] Ban (headless)";   Action = { Invoke-CheckFieldBan } }
    "10" = @{ Name = "Regenerate [SerializeField] Allowlist";   Action = { Invoke-RegenFieldAllowlist } }
    "11" = @{ Name = "Verify Scaffold Drift (headless)";        Action = { Invoke-VerifyDrift } }
    "12" = @{ Name = "Regenerate Scaffold Snapshots";           Action = { Invoke-RegenSnapshots } }
    "13" = @{ Name = "Check Resources.Load Ban (headless)";     Action = { Invoke-CheckResourcesLoadBan } }
    "14" = @{ Name = "Regenerate Resources.Load Allowlist";     Action = { Invoke-RegenResourcesLoadAllowlist } }
    "15" = @{ Name = "Check Instantiate Ban (headless)";        Action = { Invoke-CheckInstantiateBan } }
    "16" = @{ Name = "Regenerate Instantiate Allowlist";        Action = { Invoke-RegenInstantiateAllowlist } }
    "17" = @{ Name = "Save Scene Scaffolds (scene -> code)";    Action = { Invoke-SaveSceneScaffolds } }
    "18" = @{ Name = "Check All Guardrails (CI smoke test)";    Action = { Invoke-CheckAllGuardrails } }
}

$Host.UI.RawUI.WindowTitle = "Main Menu"

while ($true) {
    Write-Header "GridGame"
    foreach ($key in $mainMenu.Keys) {
        $padded = $key.PadLeft(2)
        Write-Host "    [$padded]  $($mainMenu[$key].Name)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "    [ 0]  Quit" -ForegroundColor DarkGray
    Write-Host ""

    $choice = Read-MenuChoice

    if ($choice -eq "0") { break }

    if ($mainMenu.ContainsKey($choice)) {
        & $mainMenu[$choice].Action
    } else {
        Write-Host "  Invalid choice." -ForegroundColor Red
        Start-Sleep -Seconds 1
    }
}
