# .claude/hooks/inject-digest.ps1
# -----------------------------------------------------------------------------
# SessionStart hook (MindAttic Codex standard): injects docs/BIBLE.digest.md into
# Claude Code's context so the authoritative canon is in-context from the first
# prompt -- without loading the full ~180 KB bible.
#
# Emits Claude Code hook JSON on stdout:
#   { "hookSpecificOutput": { "hookEventName": "SessionStart",
#                              "additionalContext": "<preamble + digest>" } }
#
# If the digest is missing or empty, emits {} so the session still starts.
# Replaces the legacy inject-bible.ps1 (which read game_bible.md).
# -----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

# Repo root relative to THIS script -- it lives in <repo>/.claude/hooks/
$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$digestPath = Join-Path $repoRoot 'docs/BIBLE.digest.md'

if (-not (Test-Path $digestPath)) { Write-Output '{}'; exit 0 }

$digest = Get-Content -Raw -Path $digestPath -Encoding UTF8
if ([string]::IsNullOrWhiteSpace($digest)) { Write-Output '{}'; exit 0 }

$preamble = @'
# docs/BIBLE.digest.md -- auto-loaded canonical digest (GridGame2026 / GG)

This is the AUTHORITATIVE digest of the GridGame2026 Project Bible (the one-sentence definition,
what the game is NOT, the Laws, the glossary, and the current status index). Full detail lives in
docs/BIBLE.md; structured canon (spells/buffs/classes/enemies/rarities) is in docs/data/*.json;
remaining work is in docs/USER_STORIES.md.

Rule: if anything the user requests, asserts, or implies contradicts this canon, surface the
contradiction and ask them to either (a) record the new direction (a docs/AMENDMENTS.md entry --
"amendment wins" -- or a bible edit), or (b) correct their own assumption. Do NOT silently drift.

---

'@

$payload = [ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName     = 'SessionStart'
        additionalContext = $preamble + $digest
    }
}

$json = $payload | ConvertTo-Json -Compress -Depth 10

# PowerShell 5.1's ConvertTo-Json passes non-ASCII through raw, and stdout defaults to
# Windows-1252 on this box -- em-dashes / box-drawing would arrive mangled. Walk every char;
# ASCII passes through, anything >= 0x80 becomes \uXXXX. Result is pure ASCII, codepage-independent.
$sb = New-Object System.Text.StringBuilder ($json.Length + 64)
foreach ($ch in $json.ToCharArray()) {
    $code = [int]$ch
    if ($code -lt 128) { [void]$sb.Append($ch) }
    else { [void]$sb.AppendFormat('\u{0:x4}', $code) }
}

Write-Output $sb.ToString()
