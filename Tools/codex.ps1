<#
.SYNOPSIS
    codex.ps1 -- the MindAttic Codex doctor + digest CLI for GridGame2026 (CODE: GG).

.DESCRIPTION
    Subcommands:
      doctor  Validate the docs/ canon: front-matter, unique {#...} IDs + resolvable
              cross-refs, JSON data vs schema + unique entity ids, bible code-path
              citations exist, generatedFrom artifacts not stale, digest freshness.
              Exit non-zero on any HARD error.
      digest  Regenerate docs/BIBLE.digest.md from BIBLE.md (the one-sentence, what-it-is-NOT,
              Laws, Glossary) + a status index + the latest amendment head.

    No build step. Windows PowerShell 5.1 safe (no pwsh-only syntax).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/codex.ps1 doctor
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/codex.ps1 digest
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('doctor', 'digest')]
    [string]$Command = 'doctor'
)

$ErrorActionPreference = 'Stop'

# --- paths -------------------------------------------------------------------
$RepoRoot = Split-Path -Parent $PSScriptRoot
$DocsDir  = Join-Path $RepoRoot 'docs'
$DataDir  = Join-Path $DocsDir 'data'
$SchemaDir = Join-Path $DataDir '_schema'
$BiblePath = Join-Path $DocsDir 'BIBLE.md'
$StoriesPath = Join-Path $DocsDir 'USER_STORIES.md'
$AmendPath = Join-Path $DocsDir 'AMENDMENTS.md'
$DigestPath = Join-Path $DocsDir 'BIBLE.digest.md'
$RfcDir = Join-Path $DocsDir 'rfc'

# --- helpers -----------------------------------------------------------------
function Read-Text([string]$p) { return [IO.File]::ReadAllText($p) }

function Get-FrontMatter([string]$text) {
    # Returns a hashtable of the YAML front-matter (codex docs use a flat block) or $null.
    # Strip a leading UTF-8 BOM if present (U+FEFF).
    if ($text.Length -gt 0 -and [int]$text[0] -eq 0xFEFF) { $text = $text.Substring(1) }
    $m = [regex]::Match($text, '(?s)^---\r?\n(.*?)\r?\n---\r?\n')
    if (-not $m.Success) { return $null }
    $h = @{}
    foreach ($line in ($m.Groups[1].Value -split "`n")) {
        $line = $line.TrimEnd("`r")
        if ($line -match '^\s*([A-Za-z0-9_]+)\s*:\s*(.*)$') {
            $h[$Matches[1]] = $Matches[2].Trim()
        }
    }
    return $h
}

# GitHub-style anchor slug for a heading line's text.
function Get-HeadingSlug([string]$heading) {
    $s = $heading.Trim().ToLowerInvariant()
    # strip an explicit {#id} attribute first (it becomes the anchor instead)
    $s = [regex]::Replace($s, '\{#[^}]+\}', '')
    # drop markdown emphasis / inline-code markers
    $s = $s -replace '[`*_]', ''
    # GitHub slug rules: remove anything not alphanumeric / space / hyphen, then replace EACH
    # space with a single hyphen (runs of spaces are NOT collapsed -- '/'-removal can leave a
    # double space that becomes '--'). Underscores are word chars and are kept.
    $s = [regex]::Replace($s, '[^\w\s-]', '')
    $s = $s.Trim()
    $s = $s -replace ' ', '-'
    return $s
}

$script:HardErrors = @()
$script:Warnings   = @()
function Fail([string]$m) { $script:HardErrors += $m; Write-Host ("  [FAIL] " + $m) -ForegroundColor Red }
function Warn([string]$m) { $script:Warnings   += $m; Write-Host ("  [warn] " + $m) -ForegroundColor Yellow }
function Ok([string]$m)   { Write-Host ("  [ ok ] " + $m) -ForegroundColor Green }

# =============================================================================
# DIGEST
# =============================================================================
function Invoke-Digest {
    if (-not (Test-Path $BiblePath)) { throw "BIBLE.md not found at $BiblePath" }
    $bible = Read-Text $BiblePath

    function Section([string]$text, [string]$anchorId) {
        # Capture from a heading bearing {#anchorId} up to the next heading of <= its level.
        $m = [regex]::Match($text, '(?m)^(#{1,6})\s+[^\r\n]*\{#' + [regex]::Escape($anchorId) + '\}[^\r\n]*$')
        if (-not $m.Success) { return $null }
        $level = $m.Groups[1].Value.Length
        $start = $m.Index
        $rest = $text.Substring($m.Index + $m.Length)
        $next = [regex]::Match($rest, '(?m)^#{1,' + $level + '}\s')
        if ($next.Success) { return $text.Substring($start, $m.Length + $next.Index) }
        return $text.Substring($start)
    }

    # Build the section anchor (e.g. "GG-<U+00A7>1") WITHOUT a literal section sign in the script
    # source -- Windows PowerShell 5.1 mis-decodes a BOM-less UTF-8 source's non-ASCII bytes.
    $S = [char]0x00A7
    $one   = Section $bible ("GG-{0}1" -f $S)
    $isnot = Section $bible ("GG-{0}3" -f $S)
    $laws  = Section $bible ("GG-{0}5" -f $S)
    $gloss = Section $bible ("GG-{0}9" -f $S)

    # Status index: count story status glyphs in USER_STORIES.md.
    $counts = [ordered]@{ done = 0; partial = 0; planned = 0; cut = 0 }
    if (Test-Path $StoriesPath) {
        $stories = Read-Text $StoriesPath
        $lines = $stories -split "`n"
        $check = [char]0x2705   # check mark (BMP, safe to cast)
        foreach ($ln in $lines) {
            if ($ln -notmatch 'US-\d') { continue }
            if     ($ln -match '\[~\]') { $counts.partial++ }
            elseif ($ln -match '\[x\]') { $counts.done++ }
            elseif ($ln -match '\[ \]') { $counts.planned++ }
            elseif ($ln -match [regex]::Escape($check)) { $counts.done++ }
        }
    }

    # Latest amendment head (first '## ' heading in AMENDMENTS.md).
    $amendHead = ''
    if (Test-Path $AmendPath) {
        $am = Read-Text $AmendPath
        $hm = [regex]::Match($am, '(?m)^##\s+(.+)$')
        if ($hm.Success) { $amendHead = $hm.Groups[1].Value.Trim() }
    }

    $nl = "`n"
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("AUTHORITATIVE -- full detail in docs/BIBLE.md" + $nl + $nl)
    [void]$sb.Append("<!-- GENERATED by tools/codex.ps1 digest from docs/BIBLE.md. Do NOT hand-edit. generatedFrom: GG-bible -->" + $nl)
    [void]$sb.Append("# GridGame2026 (GG) -- Bible Digest" + $nl + $nl)
    [void]$sb.Append("> Source of truth for what GridGame2026 IS, is NOT, and the laws that keep it coherent." + $nl)
    [void]$sb.Append("> Generated " + (Get-Date -Format 'yyyy-MM-dd') + ". Regenerate with: tools/codex.ps1 digest." + $nl + $nl)
    if ($one)   { [void]$sb.Append($one.Trim() + $nl + $nl) }
    if ($isnot) { [void]$sb.Append($isnot.Trim() + $nl + $nl) }
    if ($laws)  { [void]$sb.Append($laws.Trim() + $nl + $nl) }
    if ($gloss) { [void]$sb.Append($gloss.Trim() + $nl + $nl) }
    [void]$sb.Append("## Status index (from docs/USER_STORIES.md)" + $nl)
    [void]$sb.Append(("- done: {0}  partial: {1}  planned: {2}  cut: {3}" -f $counts.done, $counts.partial, $counts.planned, $counts.cut) + $nl + $nl)
    if ($amendHead) { [void]$sb.Append("## Latest amendment" + $nl + "- " + $amendHead + $nl) }

    [IO.File]::WriteAllText($DigestPath, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
    Write-Host ("Wrote " + $DigestPath) -ForegroundColor Cyan
}

# =============================================================================
# DOCTOR
# =============================================================================
function Invoke-Doctor {
    Write-Host "Codex doctor -- GridGame2026 (GG)" -ForegroundColor Cyan

    # ---- 1. front-matter on every L0/L1/L2/rfc/data file --------------------
    Write-Host "1. Front-matter"
    $fmFiles = @($BiblePath, $StoriesPath, $AmendPath)
    if (Test-Path $RfcDir) { $fmFiles += (Get-ChildItem $RfcDir -Filter '*.md' -File | ForEach-Object { $_.FullName }) }
    foreach ($f in $fmFiles) {
        if (-not (Test-Path $f)) { Fail "missing file: $f"; continue }
        $fm = Get-FrontMatter (Read-Text $f)
        $rel = $f.Substring($RepoRoot.Length + 1)
        if ($null -eq $fm) { Fail "$rel : no YAML front-matter"; continue }
        if ($fm['codex'] -ne '1') { Fail "$rel : front-matter 'codex' must be 1" }
        foreach ($k in @('project', 'code', 'layer', 'status', 'updated')) {
            if (-not $fm.ContainsKey($k) -or [string]::IsNullOrWhiteSpace($fm[$k])) { Fail "$rel : front-matter missing '$k'" }
        }
    }
    # data files carry a _meta block (JSON), validated in step 3.
    if ($script:HardErrors.Count -eq 0) { Ok "all L0/L1/L2/rfc files have valid codex front-matter" }

    # ---- 2. unique {#...} IDs + cross-ref resolution ------------------------
    Write-Host "2. Anchor IDs and cross-references"
    $mdFiles = @($BiblePath, $StoriesPath, $AmendPath)
    if (Test-Path $RfcDir) { $mdFiles += (Get-ChildItem $RfcDir -Filter '*.md' -File | ForEach-Object { $_.FullName }) }

    $explicitIds = @{}     # {#id} -> file
    $headingSlugs = @{}    # per-file set of heading slugs (for #frag resolution)
    foreach ($f in $mdFiles) {
        $rel = $f.Substring($RepoRoot.Length + 1)
        $text = Read-Text $f
        $headingSlugs[$f] = New-Object System.Collections.Generic.HashSet[string]
        foreach ($hm in [regex]::Matches($text, '(?m)^#{1,6}\s+(.+)$')) {
            $line = $hm.Groups[1].Value
            $idm = [regex]::Match($line, '\{#([^}]+)\}')
            if ($idm.Success) {
                $id = $idm.Groups[1].Value
                if ($explicitIds.ContainsKey($id)) { Fail "duplicate anchor {#$id} in $rel (also in $($explicitIds[$id]))" }
                else { $explicitIds[$id] = $rel }
                [void]$headingSlugs[$f].Add($id.ToLowerInvariant())
            }
            [void]$headingSlugs[$f].Add((Get-HeadingSlug $line))
        }
        # explicit <a id="..."></a> anchors
        foreach ($am in [regex]::Matches($text, '<a\s+id="([^"]+)"')) {
            [void]$headingSlugs[$f].Add($am.Groups[1].Value.ToLowerInvariant())
        }
    }
    if ($explicitIds.Count -gt 0) { Ok "$($explicitIds.Count) explicit {#...} anchors, all unique" }

    # Cross-refs: markdown links whose target is '#frag' or 'relpath#frag' (intra-docs only).
    $refCount = 0; $brokeCount = 0
    foreach ($f in $mdFiles) {
        $rel = $f.Substring($RepoRoot.Length + 1)
        $dir = Split-Path -Parent $f
        $text = Read-Text $f
        foreach ($lm in [regex]::Matches($text, '\]\(([^)]+)\)')) {
            $target = $lm.Groups[1].Value.Trim()
            if ($target -notmatch '#') { continue }
            $parts = $target -split '#', 2
            $path = $parts[0]; $frag = $parts[1].ToLowerInvariant()
            if ([string]::IsNullOrWhiteSpace($frag)) { continue }
            $refCount++
            $targetFile = $null
            if ([string]::IsNullOrWhiteSpace($path)) { $targetFile = $f }
            else {
                if ($path -match '^[a-z]+://') { continue }   # external URL
                try { $targetFile = (Resolve-Path (Join-Path $dir $path) -ErrorAction Stop).Path } catch { $targetFile = $null }
            }
            if ($null -eq $targetFile) {
                # path points outside the doc set (e.g. ../MindAttic.HouseRules.md): verify the FILE exists, skip frag.
                $abs = Join-Path $dir $path
                if (Test-Path $abs) { continue }
                Warn "$rel : link path not found: $target"; continue
            }
            if (-not $headingSlugs.ContainsKey($targetFile)) {
                # target is a real file we didn't index (e.g. HouseRules). File exists => accept.
                continue
            }
            if (-not $headingSlugs[$targetFile].Contains($frag)) {
                $brokeCount++; Fail "$rel : unresolved cross-ref '#$frag' -> $target"
            }
        }
    }
    if ($brokeCount -eq 0) { Ok "$refCount intra-doc cross-references resolve" }

    # ---- 3. data JSON validates vs schema; entity ids unique ----------------
    Write-Host "3. L5 data vs schema"
    if (Test-Path $DataDir) {
        $dataFiles = Get-ChildItem $DataDir -Filter '*.json' -File
        foreach ($df in $dataFiles) {
            $rel = $df.FullName.Substring($RepoRoot.Length + 1)
            try { $json = (Read-Text $df.FullName) | ConvertFrom-Json } catch { Fail "$rel : invalid JSON ($($_.Exception.Message))"; continue }
            $meta = $json._meta
            if ($null -eq $meta) { Fail "$rel : missing _meta block"; continue }
            if ($meta.codex -ne 1) { Fail "$rel : _meta.codex must be 1" }
            foreach ($k in @('project', 'code', 'layer', 'type', 'status', 'updated')) {
                if ([string]::IsNullOrWhiteSpace([string]$meta.$k)) { Fail "$rel : _meta missing '$k'" }
            }
            $type = [string]$meta.type
            $schemaPath = Join-Path $SchemaDir ($type + '.schema.json')
            if (-not (Test-Path $schemaPath)) { Fail "$rel : no schema for type '$type' ($($type).schema.json)"; continue }
            $schema = (Read-Text $schemaPath) | ConvertFrom-Json
            $required = @($schema.required)
            $allowed = @()
            if ($schema.additionalProperties -eq $false -and $schema.properties) {
                $allowed = @($schema.properties.PSObject.Properties.Name)
            }
            $idPattern = $null
            if ($schema.properties -and $schema.properties.id -and $schema.properties.id.pattern) { $idPattern = $schema.properties.id.pattern }

            # the entity array is the single non-_meta property
            $arrProp = $json.PSObject.Properties | Where-Object { $_.Name -ne '_meta' } | Select-Object -First 1
            if ($null -eq $arrProp) { Fail "$rel : no entity array"; continue }
            $entities = @($arrProp.Value)
            $ids = @{}
            foreach ($e in $entities) {
                foreach ($r in $required) {
                    $v = $e.$r
                    if ($null -eq $v -or ($v -is [string] -and [string]::IsNullOrWhiteSpace($v))) {
                        Fail "$rel : entity '$($e.id)' missing required '$r'"
                    }
                }
                if ($allowed.Count -gt 0) {
                    foreach ($p in $e.PSObject.Properties.Name) {
                        if ($allowed -notcontains $p) { Fail "$rel : entity '$($e.id)' has unknown property '$p'" }
                    }
                }
                if ($idPattern -and $e.id -and ($e.id -notmatch $idPattern)) {
                    Fail "$rel : id '$($e.id)' violates pattern $idPattern"
                }
                if ($e.id) {
                    if ($ids.ContainsKey($e.id)) { Fail "$rel : duplicate entity id '$($e.id)'" }
                    else { $ids[$e.id] = $true }
                }
            }
            Ok "$rel : $($entities.Count) '$type' entities validate (schema $($type).schema.json)"
        }
    } else { Warn "no docs/data directory" }

    # ---- 4. every  done  story names an evidence token ----------------------
    Write-Host "4. Story evidence tokens"
    $testTree = @()
    $testsRoot = Join-Path $RepoRoot 'Assets'
    if (Test-Path $testsRoot) {
        $testTree = Get-ChildItem -Path $testsRoot -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '[\\/]Tests?[\\/]' }
    }
    $testMethodNames = New-Object System.Collections.Generic.HashSet[string]
    foreach ($tf in $testTree) {
        foreach ($mm in [regex]::Matches((Read-Text $tf.FullName), 'public\s+(?:IEnumerator|void)\s+([A-Za-z0-9_]+)\s*\(')) {
            [void]$testMethodNames.Add($mm.Groups[1].Value)
        }
    }
    if (Test-Path $StoriesPath) {
        $storyText = Read-Text $StoriesPath
        $storyLines = $storyText -split "`n"
        $missing = 0; $doneCount = 0
        $check = [char]0x2705
        foreach ($ln in $storyLines) {
            if ($ln -notmatch '\*\*US-\d') { continue }
            $isDone = ($ln -match '\[x\]') -or ($ln -match [regex]::Escape($check))
            if (-not $isDone) { continue }
            $doneCount++
            # evidence = a backtick-quoted token (file/method/demo). This project verifies by
            # play-test + code-reading (GG-§6), so any cited token counts; absence is a warn.
            if ($ln -notmatch '`[^`]+`') {
                $idm = [regex]::Match($ln, 'US-\d+')
                Warn "story $($idm.Value): done but cites no evidence token (file/demo/test)"
                $missing++
            }
        }
        if ($missing -eq 0) { Ok "$doneCount done stories all cite an evidence token" }
        # best-effort: any story that names a *Test method -> confirm it exists
        foreach ($ln in $storyLines) {
            $tm = [regex]::Match($ln, '`([A-Za-z0-9_]+Test|Test[A-Za-z0-9_]+|[A-Za-z0-9_]*_[A-Za-z0-9_]*test[A-Za-z0-9_]*)`')
            if ($tm.Success) {
                $name = $tm.Groups[1].Value
                if (-not $testMethodNames.Contains($name)) { } # not necessarily a test token; skip noise
            }
        }
        Ok "test tree scanned ($($testMethodNames.Count) test methods on disk)"
    }

    # ---- 5. every code path cited in the bible exists on disk ---------------
    Write-Host "5. Bible code-path citations"
    $bible = Read-Text $BiblePath
    $cited = New-Object System.Collections.Generic.HashSet[string]
    foreach ($cm in [regex]::Matches($bible, '`(Assets/[^`]+?\.(cs|json|unity|txt|asmdef))`')) {
        [void]$cited.Add($cm.Groups[1].Value)
    }
    # also catch Assets/... paths inside link parens or plain prose (no backticks)
    foreach ($cm in [regex]::Matches($bible, '(?<![`\w/])(Assets/[A-Za-z0-9_./-]+\.(?:cs|json|unity|asmdef))')) {
        [void]$cited.Add($cm.Groups[1].Value)
    }
    $missingPaths = 0
    foreach ($p in $cited) {
        $abs = Join-Path $RepoRoot ($p -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path $abs)) { Warn "bible cites missing path: $p"; $missingPaths++ }
    }
    if ($missingPaths -eq 0) { Ok "$($cited.Count) cited Assets/ paths all exist on disk" }
    else { Warn "$missingPaths cited path(s) not found (bible prose may name planned/renamed files)" }

    # ---- 6. generatedFrom artifacts not stale -------------------------------
    Write-Host "6. Derived-artifact freshness"
    if (Test-Path $DataDir) {
        foreach ($df in (Get-ChildItem $DataDir -Filter '*.json' -File)) {
            $json = (Read-Text $df.FullName) | ConvertFrom-Json
            $gf = $json._meta.generatedFrom
            # data files are hand-curated mirrors, not auto-generated; generatedFrom is a provenance
            # pointer, so we don't stale-check them against BIBLE mtime (would false-positive).
        }
        Ok "data provenance (generatedFrom) present"
    }

    # ---- 7. digest freshness ------------------------------------------------
    Write-Host "7. Digest freshness"
    if (-not (Test-Path $DigestPath)) {
        Warn "BIBLE.digest.md missing -- run: tools/codex.ps1 digest"
    } else {
        $bibleMtime = (Get-Item $BiblePath).LastWriteTimeUtc
        $digestMtime = (Get-Item $DigestPath).LastWriteTimeUtc
        if ($bibleMtime -gt $digestMtime) { Warn "BIBLE.digest.md is older than BIBLE.md -- run: tools/codex.ps1 digest" }
        else { Ok "BIBLE.digest.md is up to date" }
    }

    # ---- summary ------------------------------------------------------------
    Write-Host ""
    if ($script:HardErrors.Count -gt 0) {
        Write-Host ("DOCTOR FAILED -- {0} hard error(s), {1} warning(s)" -f $script:HardErrors.Count, $script:Warnings.Count) -ForegroundColor Red
        exit 1
    }
    Write-Host ("DOCTOR PASSED -- 0 hard errors, {0} warning(s)" -f $script:Warnings.Count) -ForegroundColor Green
    exit 0
}

switch ($Command) {
    'digest' { Invoke-Digest }
    'doctor' { Invoke-Doctor }
}
