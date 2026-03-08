# Project Documentation

This folder contains documentation designed to be readable by both humans and LLMs.

## Structure

```
Documentation/
├── README.md                    # This file
├── DOCUMENTATION_STYLE_GUIDE.md # How to write documentation
├── Scenes/                      # Scene hierarchy documentation
│   ├── GameScene_Hierarchy.md
│   ├── TitleScreen_Hierarchy.md
│   └── ...
├── Prefabs/                     # Prefab structure documentation
│   └── ActorPrefab_Analysis.md
└── Architecture/                # System architecture docs
    └── ...
```

## Generating Documentation

### Scene Documentation
1. Open Unity
2. Go to **Tools → Scene Analyzer**
3. Configure options and click **Analyze Current Scene**
4. Or use **Tools → Analyze Current Scene** for quick analysis

### Prefab Documentation
1. Select a prefab in the Project window
2. Go to **Tools → Analyze Selected Prefab**
3. Output saved to `Editor/PrefabAnalysis_Output.txt`

## Why This Exists

Unity's `.scene` and `.prefab` files are YAML but contain:
- GUIDs instead of asset names
- Serialized binary data
- Deep nesting that's hard to parse

This documentation provides:
- Human-readable hierarchy trees
- Component listings with key properties
- Asset references by name
- Summary statistics

## For LLMs

When providing context about a scene or prefab:
1. Include the relevant markdown file from this folder
2. The hierarchy shows parent-child relationships
3. Component listings show what scripts/renderers are attached
4. Summary sections highlight key objects (Managers, Cameras, etc.)
