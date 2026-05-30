# .claude/hooks/inject-bible.ps1
# -----------------------------------------------------------------------------
# SessionStart hook: injects game_bible.md into Claude Code's context so the
# canonical design spec is in-context from the first prompt -- no need for the
# user to remind Claude what game they're building.
#
# Emits JSON on stdout matching the Claude Code hook output schema:
#   { "hookSpecificOutput": { "hookEventName": "SessionStart",
#                              "additionalContext": "<bible body>" } }
#
# If game_bible.md is missing or empty, exits silently with no injection so
# the session still starts normally.
# -----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

# Resolve repo root relative to THIS script -- script lives in <repo>/.claude/hooks/
$repoRoot  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$biblePath = Join-Path $repoRoot 'game_bible.md'

if (-not (Test-Path $biblePath)) { Write-Output '{}'; exit 0 }

# Force UTF-8 so the bible's ASCII box-drawing + diacritics survive round-trip.
$bible = Get-Content -Raw -Path $biblePath -Encoding UTF8
if ([string]::IsNullOrWhiteSpace($bible)) { Write-Output '{}'; exit 0 }

$preamble = @'
# game_bible.md -- auto-loaded canonical design spec

The full GridGame2026 design bible is below. It is the source of truth for "what the game IS." Read it before any gameplay-affecting change.

**Rule:** if anything the user requests, asserts, or implies contradicts the bible, surface the contradiction and ask them to either (a) update the bible to reflect the new direction, or (b) correct their own assumption. Do NOT silently drift. The bible only stays useful if disagreements get reconciled in writing.

---

'@

$payload = [ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName     = 'SessionStart'
        additionalContext = $preamble + $bible
    }
}

# Compress + ample depth so the long bible string isn't truncated.
$json = $payload | ConvertTo-Json -Compress -Depth 10

# PowerShell 5.1's ConvertTo-Json passes non-ASCII through raw, and stdout
# defaults to Windows-1252 on this box -- em-dashes and box-drawing would
# arrive mangled. Walk every char; ASCII passes through, anything >= 0x80
# becomes \uXXXX. Result is pure ASCII -- codepage-independent on stdout.
# (Avoids literal non-ASCII chars in this script's source, which PS 5.1
# misinterprets without a UTF-8 BOM.)
$sb = New-Object System.Text.StringBuilder ($json.Length + 64)
foreach ($ch in $json.ToCharArray()) {
    $code = [int]$ch
    if ($code -lt 128) { [void]$sb.Append($ch) }
    else { [void]$sb.AppendFormat('\u{0:x4}', $code) }
}

Write-Output $sb.ToString()
