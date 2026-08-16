---
codex: 1
project: GridGame2026
code: GG
layer: rfc
status: planned
updated: 2026-08-15
---

# RFC 0002 — V2 vision: "Terra Battle, but better than Terra Battle ever was"

> Records the owner's post-PoC direction (2026-08-15 session). NOTHING here is in the V1/PoC
> build window; each item needs its own amendment + stories when V2 starts. The PoC decisions
> these would revise stay binding until then.

## The one-line vision

Terra Battle's slide-to-pincer core, elevated: a Grandia-2 interruptible timeline, a real
equipment/crafting/bounty economy, themed biome campaigns with a story, line-dodging and trap
play that make sliding itself the skill expression — with modern production polish.

## V2 items (owner-ruled "record, don't build yet")

1. **Gacha-style random summoning.** V1 keeps the deterministic gold-cost summon vendor
   (US-132) and the "NOT a gacha" pillar (§3). V2 may add random pulls — owner options
   discussed: pull-only, or dual-track (pricier deterministic recruit + cheaper random pull).
   Hard floor either way: **no premium currency, no energy system** — gold only.
   *Requires:* amendment revising §3, pull-pool/rarity design, pity rules if any.
2. **Branching storyline that locks/unlocks characters → multiple playthroughs.** V1 ships the
   linear skippable crawl (US-131, GG-A5). V2: fork points in the campaign that gate specific
   summonable characters per path. *Requires:* narrative design doc, save-slot/NG+ interaction
   (§29.1 #5 roguelike/NG+ backlog), amendment superseding GG-A5's "no branching."
3. **HD art pass.** Replace programmer art with commissioned/licensed HD sprite sets and biome
   backdrops; URP post-processing profile per biome. V1 polish stops at VFX/post/UI-consistency
   (US-123 language, US-137 audio).
4. **More RPG content breadth** (continuous): more classes beyond the 7, more spells/items via
   the existing data-driven pipeline (`docs/data/*.json` + libraries), relic passives (§24.1),
   deeper buff tiers (§8.6).

## Explicitly NOT changing (v1 pillars that survive V2)

- No premium currency / energy / live-service mechanics (§3).
- Portrait-lock + AspectGuard letterbox (GG-LAW-8) — re-affirmed by owner 2026-08-15.
- Movement never deals damage (GG-LAW-1); pincer rules (GG-LAW-2).
- Code-only / builder-driven authoring (GG-LAW-3) and the guardrails (GG-LAW-4).
