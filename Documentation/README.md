# Project Documentation

This folder contains documentation designed to be readable by both humans and LLMs.

## Structure

```
Documentation/
├── README.md                      # This file
├── DOCUMENTATION_STYLE_GUIDE.md   # How to write documentation
├── Addressables.md                # Addressables setup and conventions
├── AltTester-Setup.md             # AltTester integration notes
├── ProjectSettings.md             # Notable ProjectSettings values
├── Builders/                      # Builder system docs and scene snapshots
│   ├── README.md                  # Builder system overview
│   ├── SceneHierarchies.txt       # Parsed hierarchy of every scene file
│   └── Drift/                     # BuilderDriftChecker snapshot files
└── Scenes/                        # Per-scene hierarchy docs
```

## Generating Scene Hierarchies

`Documentation/Builders/SceneHierarchies.txt` is the authoritative parsed output of every scene file. Regenerate it with:

```powershell
$scenes = @('SplashScreen','TitleScreen','ProfileSelect','ProfileCreate','SaveFileSelect',
            'StageSelect','LoadingScreen','Hub','PostBattleScreen','Settings','Credits',
            'PartyManager','Abilities','Alchemist','Blacksmith','Equip','Party','Vendor',
            'Game','Overworld','Bestiary')
foreach ($s in $scenes) {
    powershell -ExecutionPolicy Bypass -File "Tools\ParseScene.ps1" -ScenePath "Assets\Scenes\$s.unity"
}
```

Or via batchmode: `CliEntryPoints.GenerateDocs`

## Why This Exists

Unity's `.scene` and `.prefab` files are YAML but contain GUIDs instead of asset names, serialized binary data, and deep nesting that is hard to parse. This documentation provides:

- Human-readable hierarchy trees
- Component listings with key properties
- Asset references by name

## For LLMs

When providing context about a scene or prefab:
1. Include the relevant section from `Documentation/Builders/SceneHierarchies.txt`
2. The hierarchy shows parent-child relationships
3. Component listings show what scripts/renderers are attached
4. The builder `.cs` file under `Assets/Editor/Builders/` is the authoritative source of truth — the `.unity` is a derived artifact

## Codex Canon

Design and rules documentation lives under `docs/` (not here):

| File | Purpose |
|---|---|
| `docs/BIBLE.md` | L0 — source of truth for what GridGame2026 is and the Laws |
| `docs/AMENDMENTS.md` | L1 — append-only change log; amendments override the bible |
| `docs/USER_STORIES.md` | L2 — dependency-ordered build board |
| `docs/rfc/` | Design notes that graduate into the bible + stories |
| `docs/data/*.json` | Canon-as-data (spells, buffs, classes, enemy archetypes) |

Do not edit Codex files (`BIBLE.md`, `AMENDMENTS.md`, `USER_STORIES.md`, `rfc-*.md`) or `*.digest.md` files directly — use `tools/codex.ps1` to validate and regenerate digests.
