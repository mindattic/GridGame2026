param(
    [string]$Source      = 'D:\Projects\Unity\GridGame2025',
    [string]$BackupBase  = 'R:\Backup\SnowCrash',
    [string]$Subfolder   = 'GridGame2025'
)

$ErrorActionPreference = 'Stop'

# Get date as yyyy-MM-dd
$today = Get-Date -Format 'yyyy-MM-dd'
$baseFolder   = Join-Path $BackupBase $today
$backupFolder = $baseFolder

function Get-AlphaSuffix {
    param([int]$Index)
    # 0 -> a, 1 -> b, ..., 25 -> z, 26 -> aa, etc.
    $letters = 'abcdefghijklmnopqrstuvwxyz'
    $n = $Index
    $s = ''
    do {
        $q = [math]::Floor($n / 26)
        $r = $n % 26
        $s = $letters[$r] + $s
        $n = $q - 1
    } while ($q -gt 0)
    return $s
}

if (Test-Path -LiteralPath $backupFolder) {
    $i = 0
    do {
        $suffix = Get-AlphaSuffix -Index $i
        $backupFolder = '{0}_{1}' -f $baseFolder, $suffix
        $i++
    } while (Test-Path -LiteralPath $backupFolder)
}

New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null

# Copy project
# Use Copy-Item to mirror XCOPY behavior from the batch file
$dest = Join-Path $backupFolder $Subfolder
Copy-Item -LiteralPath $Source -Destination $dest -Recurse -Force

Write-Host ('Backup completed: {0}' -f $backupFolder)
