$editorDir = "D:\Projects\Unity\GridGame2026\Assets\Editor"

$allNamespaces = @(
    'Scripts.Canvas',
    'Scripts.Data.Actor',
    'Scripts.Data.Items',
    'Scripts.Data.Skills',
    'Scripts.Effects',
    'Scripts.Factories',
    'Scripts.Helpers',
    'Scripts.Hub',
    'Scripts.Instances',
    'Scripts.Instances.Actor',
    'Scripts.Instances.Board',
    'Scripts.Instances.SynergyLine',
    'Scripts.Inventory',
    'Scripts.Libraries',
    'Scripts.Managers',
    'Scripts.Models',
    'Scripts.Models.Actor',
    'Scripts.Overworld',
    'Scripts.Sequences',
    'Scripts.Serialization',
    'Scripts.Utilities'
)

$files = Get-ChildItem -Path $editorDir -Filter "*.cs" -Recurse
$count = 0

foreach ($f in $files) {
    $file = $f.FullName
    $content = [System.IO.File]::ReadAllText($file)
    $changed = $false

    # Fix old namespace references
    $original = $content
    $content = $content -replace 'Assets\.Scripts\.Behaviors\.Actor', 'Scripts.Instances.Actor'
    $content = $content -replace 'Assets\.Scripts\.GUI', 'Scripts.Canvas'
    $content = $content -replace 'Assets\.Data\.Actor', 'Scripts.Data.Actor'
    $content = $content -replace 'Assets\.Scripts\.', 'Scripts.'
    $content = $content -replace 'Assets\.Helpers', 'Scripts.Helpers'
    $content = $content -replace 'Assets\.Helper(?!s)', 'Scripts.Helpers'
    $content = $content -replace 'Game\.Behaviors\.Actor', 'Scripts.Instances.Actor'
    $content = $content -replace 'Game\.Behaviors', 'Scripts.Managers'
    $content = $content -replace 'Game\.Instances\.Actor', 'Scripts.Instances.Actor'
    $content = $content -replace 'Game\.Instances', 'Scripts.Instances'
    $content = $content -replace 'Game\.Manager(?!s)', 'Scripts.Managers'
    $content = $content -replace 'Game\.Models\.Profile', 'Scripts.Models'
    $content = $content -replace 'Game\.Models', 'Scripts.Models'

    if ($content -ne $original) { $changed = $true }

    # Add missing project namespace usings
    $lines = $content -split "`r?`n"
    $existingUsings = @{}
    foreach ($ln in $lines) {
        if ($ln -match '^\s*using\s+(?!static\s)([A-Za-z][A-Za-z0-9_.]*)\s*;') {
            $existingUsings[$matches[1]] = $true
        }
    }

    $addUsings = @()
    foreach ($pn in $allNamespaces) {
        if (-not $existingUsings.ContainsKey($pn)) {
            $addUsings += "using $pn;"
        }
    }

    if ($addUsings.Count -gt 0) {
        $lastUsIdx = -1
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^\s*using\s+' -and $lines[$i] -notmatch '\(' -and $lines[$i] -notmatch '^\s*using\s+var\s') {
                $lastUsIdx = $i
            }
        }

        $result = [System.Collections.Generic.List[string]]::new()
        if ($lastUsIdx -ge 0) {
            for ($i = 0; $i -le $lastUsIdx; $i++) { $result.Add($lines[$i]) }
            foreach ($au in $addUsings) { $result.Add($au) }
            for ($i = $lastUsIdx + 1; $i -lt $lines.Count; $i++) { $result.Add($lines[$i]) }
        } else {
            foreach ($au in $addUsings) { $result.Add($au) }
            $result.Add("")
            foreach ($ln in $lines) { $result.Add($ln) }
        }
        $content = ($result -join "`r`n")
        $changed = $true
    }

    if ($changed) {
        [System.IO.File]::WriteAllText($file, $content)
        $count++
        Write-Host "Fixed: $($f.Name)"
    }
}

Write-Host "Done! Fixed $count editor files."
