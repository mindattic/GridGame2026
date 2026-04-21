$root = $PSScriptRoot
$backupBase = "R:\Backup\GridGame"
$repoUrl = "https://github.com/mindattic/GridGame2026.git"
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"

# === Helpers ===============================================================

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

# === 1. Run Application ====================================================

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

# === 2. Commit and Sync ====================================================

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

# === 3. Create Backup ======================================================

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

# === 4. Setup ==============================================================
# One-time (idempotent): clone/pull repo, activate pre-push git hooks, and
# launch Unity for the initial project import. Running it a second time is
# safe -- pulls latest, re-confirms hook activation, relaunches Unity.

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

    # Activate pre-push git hook
    $hooksDir = Join-Path $root ".githooks"
    if (Test-Path $hooksDir) {
        $current = git -C $root config --get core.hooksPath 2>$null
        if ($current -ne ".githooks") {
            git -C $root config core.hooksPath .githooks
            Write-Host "  Git hooks activated (core.hooksPath -> .githooks)" -ForegroundColor Green
        } else {
            Write-Host "  Git hooks already active." -ForegroundColor DarkGray
        }
    } else {
        Write-Host "  .githooks folder missing - skipping hook activation." -ForegroundColor Yellow
    }

    # Launch Unity for initial import
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

# === 5. Build Player (headless) ============================================

function Invoke-BuildPlayer {
    Invoke-Batchmode "Build Standalone Windows" "CliEntryPoints.BuildStandaloneWindows"
}

# === 6. Set Start Scene ====================================================
# Rewrites the StartScene constant in Assets/Scripts/Data/Config/StartSceneConfig.cs.
# StartSceneAuthority.[InitializeOnLoad] applies the change to
# EditorSceneManager.playModeStartScene and EditorBuildSettings.scenes[0] on
# the next Unity domain reload.

function Invoke-SetStartScene {
    Write-Header "GridGame Set Start Scene"

    $scenesRoot = Join-Path $root "Assets\Scenes"
    $configPath = Join-Path $root "Assets\Scripts\Data\Config\StartSceneConfig.cs"

    if (-not (Test-Path $configPath)) {
        Write-Host "  StartSceneConfig.cs not found at:" -ForegroundColor Red
        Write-Host "  $configPath" -ForegroundColor Red
        Wait-ForEnter
        return
    }
    if (-not (Test-Path $scenesRoot)) {
        Write-Host "  Scenes folder not found at:" -ForegroundColor Red
        Write-Host "  $scenesRoot" -ForegroundColor Red
        Wait-ForEnter
        return
    }

    $content = Get-Content $configPath -Raw
    $rx = 'public const string StartScene = "([^"]*)";'
    $match = [regex]::Match($content, $rx)
    if (-not $match.Success) {
        Write-Host "  Could not find StartScene constant in StartSceneConfig.cs." -ForegroundColor Red
        Wait-ForEnter
        return
    }
    $current = $match.Groups[1].Value

    Write-Host "  Current start scene: " -NoNewline -ForegroundColor DarkGray
    Write-Host $current -ForegroundColor White
    Write-Host ""

    $scenes = Get-ChildItem -Path $scenesRoot -Filter "*.unity" -Recurse | Sort-Object Name
    if ($scenes.Count -eq 0) {
        Write-Host "  No .unity files under $scenesRoot" -ForegroundColor Red
        Wait-ForEnter
        return
    }

    Write-Host "  Available scenes:" -ForegroundColor DarkGray
    for ($i = 0; $i -lt $scenes.Count; $i++) {
        $name = [IO.Path]::GetFileNameWithoutExtension($scenes[$i].Name)
        $padded = ($i + 1).ToString().PadLeft(2)
        if ($name -eq $current) {
            Write-Host "    [$padded]  $name  (current)" -ForegroundColor Green
        } else {
            Write-Host "    [$padded]  $name" -ForegroundColor White
        }
    }
    Write-Host ""
    Write-Host "    [ 0]  Back" -ForegroundColor DarkGray
    Write-Host ""

    $choice = Read-MenuChoice
    if ($choice -eq "0" -or $choice -eq "") { return }

    $idx = 0
    if (-not [int]::TryParse($choice, [ref]$idx) -or $idx -lt 1 -or $idx -gt $scenes.Count) {
        Write-Host "  Invalid choice." -ForegroundColor Red
        Wait-ForEnter
        return
    }

    $picked = [IO.Path]::GetFileNameWithoutExtension($scenes[$idx - 1].Name)
    if ($picked -eq $current) {
        Write-Host ""
        Write-Host "  Already set to $picked." -ForegroundColor DarkGray
        Wait-ForEnter
        return
    }

    $replacement = 'public const string StartScene = "' + $picked + '";'
    $new = [regex]::Replace($content, $rx, $replacement)

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($configPath, $new, $utf8NoBom)

    Write-Host ""
    Write-Host "  Start scene: " -NoNewline -ForegroundColor White
    Write-Host "$current -> $picked" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Unity applies the change on the next domain reload" -ForegroundColor DarkGray
    Write-Host "  (editing any script / re-opening the editor)." -ForegroundColor DarkGray
    Wait-ForEnter
}

# === Batchmode runner (used by Build) ======================================

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

# === Main Menu =============================================================

$mainMenu = [ordered]@{
    "1" = @{ Name = "Run Application";         Action = { Invoke-Run } }
    "2" = @{ Name = "Commit and Sync";         Action = { Invoke-Commit } }
    "3" = @{ Name = "Create Backup";           Action = { Invoke-Backup } }
    "4" = @{ Name = "Setup";                   Action = { Invoke-Setup } }
    "5" = @{ Name = "Build Player (headless)"; Action = { Invoke-BuildPlayer } }
    "6" = @{ Name = "Set Start Scene";         Action = { Invoke-SetStartScene } }
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
