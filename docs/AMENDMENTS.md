---
codex: 1
project: GridGame2026
code: GG
layer: amendments
status: living
updated: 2026-06-07
---

# GridGame2026 — Amendments (append-only; amendment wins over the bible)

> Never rewrite an amendment — supersede it with a new one. Beyond ~25, fold into the bible (L0)
> and start a new epoch, noting the git tag; history stays in git.

## GG-A1 — Adopt the MindAttic Codex documentation standard (supersedes the root `game_bible.md` + `user_stories.md`)

**What changed.** The repo's bible system was migrated to the Codex layout:

- `game_bible.md` (root) → [`docs/BIBLE.md`](BIBLE.md), reformatted to the L0 nine-section template
  with stable IDs (`{#GG-§N}`, `{#GG-LAW-n}`). The full original canon is preserved verbatim in
  BIBLE Appendix A so all `§N` cross-references keep resolving. A 1-line pointer remains at the old
  `game_bible.md` path.
- `user_stories.md` (root) → [`docs/USER_STORIES.md`](USER_STORIES.md), with Codex front-matter and
  the legend/audit-log conventions. **Story IDs were kept as `US-NNN`** (not renamed to
  `GG-US-<Epic><n>`) because the bible cross-references hundreds of them by number; renaming would
  break every link. A pointer remains at the old path.
- Duplicated structured canon was extracted to **L5 data** under [`docs/data/`](data/) with JSON
  Schemas in [`docs/data/_schema/`](data/_schema/): `spells.json` (GG-§7), `buffs.json` (GG-§8),
  `classes.json` (GG-§23), `enemy_archetypes.json` (GG-§14), `item_rarities.json` (GG-§24). Prose now
  cites entities by `id`.
- The SessionStart hook `.claude/hooks/inject-bible.ps1` (read `game_bible.md`) was **replaced** by
  `.claude/hooks/inject-digest.ps1`, which injects the generated `docs/BIBLE.digest.md`.
  `.claude/settings.json` was updated to point at the new hook.
- Added `tools/codex.ps1` (`doctor` + `digest`) and a Codex section to `CLAUDE.md`.

**Why.** Single home per fact, machine-checkable cross-references and data, and a smaller
authoritative digest injected at session start instead of the full ~180 KB bible.

**Migration notes / defaults chosen.**
- The org-wide house rules at `../MindAttic.HouseRules.md` already existed and is **inherited by
  reference** from BIBLE §5 — not copied or modified.
- The L0 template's nine sections are the new outline; the original 30+ sections live in Appendix A
  rather than being lost or truncated ("preserve all content").
- `doctor`'s "✅ names a test" check is **best-effort**: this project verifies via in-editor
  play-test + code-reading (headless Unity is unlicensed here, GG-§6), so doctor warns rather than
  hard-fails when a ✅ story's evidence token is a source file/demo rather than a test method.
