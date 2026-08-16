# GridGame2026 — Proof-of-Concept Play Script

*The one human pass the automated suites can't do: look, feel, and sound. Everything on this
list is already machine-verified for behavior (EditMode + PlayMode suites, all green); you're
checking that it also LOOKS and FEELS right. Launch via `GridGame.Console.ps1` → Option 1.*

## 1. Front door (2 min)
- [ ] Boot lands on **SplashScreen** → auto-advances to **TitleScreen** (title music playing —
      "Teller of the Tales").
- [ ] **New Game** → ProfileCreate keyboard works → back to Title.
- [ ] **Continue** → **StageSelect** (NOT straight into a battle). Vendor music switches on.
- [ ] StageSelect shows the campaign list, stage 1 selected, detail panel shows waves/enemies/
      **Recommended level**, and the **BountyBar** strip at the bottom (browse with Next,
      Accept one).

## 2. First battle (5 min) — GreenValley-01
- [ ] Confirm launches the **story crawl** (first Green Valley entry) — text scrolls upward,
      Skip works, then the battle loads. Battle music ("Crusade").
- [ ] Drag a hero: cardinal-only movement, displaced actors slide aside, tiles highlight.
- [ ] Form a pincer: **portrait pair slides in** (patterns VARY between pincers — counter-sweep,
      stagger, same-side chase), grow/shrink anticipation, damage lands, coins burst.
- [ ] **CombatFeed** (left side) narrates: hits with damage numbers, "X lends support to Y!",
      casts with **inline spell icons**, statuses with glyphs.
- [ ] Cast a spell from the AbilityBar — note **locked slots** ("Locked / Stage N") on a fresh
      save; cast icon rides the timeline; **Silenced** (via Debug window demo) blocks spell slots.
- [ ] Let an enemy icon reach the trigger: enemy acts; before handoff, if you finished moves
      early, watch **"Banked N mana"** mint blue orbs.
- [ ] Win → Victory sting → PostBattle: XP panes, then loot with the **"Gold +N" row first**.
      Back at StageSelect: stage 2 unlocked, bounty progress advanced if you killed Slimes.

## 3. Economy loop (5 min)
- [ ] Merchant: buy a potion (gold decreases — the gold you EARNED in battle).
- [ ] Blacksmith: Repair tab shows worn gear (run "Wear Gear −5" demo first if pristine).
- [ ] Alchemist: Heal Party button (wound someone first via demo), Mix a recipe.
- [ ] **Summon Circle** (NavBar): recruit a hero for 250g → check them in Party's carousel →
      recruit price rose to 500g.
- [ ] Abilities: assign a **spell** (new Skills & Spells section) and an item to slots.
- [ ] Bounty complete? **Claim** pays gold on the spot.

## 4. Advanced combat (5 min)
- [ ] **Desert-02**: watch a Scorpion **lay a Venom Snare** (purple tile); deliberately slide
      onto it — damage + poisoned (feed narrates); poison ticks at turn ends.
- [ ] A fire-affinity caster **telegraphs a red LINE** on the board; slide out of it before the
      cast icon reaches the trigger — "dodged!" in the feed. Stand in it once to feel the hit.
- [ ] Hit an enemy whose icon is in the red **Pushback Zone** — icon shoves left + stun.
- [ ] **Swamp-02**: the Naga is a **SNAKE BOSS** — head + 3 segments following in a chain.
      Pincer the head: "Armored!" Strike the tail first, peel it segment by segment, then the
      head. Chain ripples when it moves; segments can't be displaced.
- [ ] Cyclops (2×2 boss) still works: immovable, full-width flank to pincer.

## 5. Look & feel sweep (3 min)
- [ ] Credits: audio attribution section present (MacLeod ×3, Pixabay, bundled-pack notes).
- [ ] Bestiary, Settings (volume sliders affect music live), Endless mode boots.
- [ ] Aspect check: Device Simulator or resize the Game view across ratios — letterbox/pillarbox
      bars, never stretch; safe-area respected on notched profiles.
- [ ] Defeat path: lose a battle (or "Wound Party" demo + weak party) → Defeat music →
      PostBattle "Defeat" → StageSelect; heroes healed.

## Known/expected rough edges
- Programmer art everywhere (placeholder glyph icons; HD art pass is RFC 0002 / V2).
- Balance beyond the level curve (pushback tuning, per-stage feel) awaits your notes.
- MelancholyLull + bundled SFX pack flagged "origin untracked" in Credits — replace before any
  commercial release.
