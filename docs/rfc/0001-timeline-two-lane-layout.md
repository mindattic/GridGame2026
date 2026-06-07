---
codex: 1
project: GridGame2026
code: GG
layer: rfc
status: planned
updated: 2026-06-07
---

# RFC 0001 — Timeline two-lane layout (turn-icons above, cast-icons below)

> Tracks the design-locked-but-unbuilt story [`US-114`](../USER_STORIES.md). Graduates into
> BIBLE [§2.6](../BIBLE.md#26-one-timeline-two-lanes--turn-icons-above-cast-icons-below) once shipped.

## Problem

Today actor turn-icons and spell cast progress share one row on the TimelineBar; casts render as
stacked, shrinking `SpellCastBar` bars. There is no single visual axis where you can read "whose turn
lands first" against "when does this cast resolve". As enemy charge casts (US-026) ride the same
timeline, the single-lane layout gets crowded and hard to read.

## Options compared

- **A — Keep one lane, restyle the cast bars.** Cheapest, but does not solve the read problem; casts
  still don't line up on the shared u-axis against turn-icons.
- **B — Two lanes on one shared u-axis (chosen).** Large actor/portrait turn-icons render **above**
  the timeline line; ¼-size cast icons render **below** it on the same axis, so a cast's horizontal
  position *is* its progress and lines up under the turn-icons. Retire `SpellCastBar` entirely.
- **C — A separate, second timeline strip for casts.** Clear separation but doubles screen real
  estate (costly on portrait mobile, GG-LAW-8) and breaks the "one shared clock" mental model.

## Decision

**Option B.** One shared timeline, two lanes, one trigger. A cast resolves when its below-line icon
reaches the trigger, off any particular turn (the IP-gauge model, BIBLE §2.6). Enemy charge icons
(US-026) ride the same below-line lane.

## What NOT to do

- Do not add a second timeline strip (Option C) — portrait space is locked (GG-LAW-8).
- Do not keep the stacked shrinking `SpellCastBar` bars; the cast's *position* is the progress read.
- Do not couple cast resolution to a turn boundary — it resolves on the shared continuous clock.

## Phased plan (with risk)

1. Render turn-icons large above the line, cast icons ~¼-size below it on the same u-axis. *(Risk:
   anchor math depends on US-001 AspectGuard normalization landing first.)*
2. Retire `Canvas/SpellCastBar` + `Factories/SpellCastBarFactory`; `AbilityBar.HandleSpell` stops
   spawning a cast bar. *(Risk: lingering references — sweep for both before deleting.)*
3. Verify in-editor on reference + off-aspect devices (the layout is the whole point; headless can't
   verify it — GG-§6).

## Graduates into

- BIBLE [§2.6](../BIBLE.md#26-one-timeline-two-lanes--turn-icons-above-cast-icons-below), §2.8, §9.
- Story [`US-114`](../USER_STORIES.md) (flip ⬜ → ✅ when play-tested). **Dep:** US-001.
