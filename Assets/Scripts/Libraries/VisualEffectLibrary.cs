using Scripts.Helpers;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// VISUALEFFECTLIBRARY - Registry of all visual effects.
    /// 
    /// PURPOSE:
    /// Lazy-loads and caches VisualEffectAsset definitions
    /// for all VFX used in combat, abilities, and UI.
    /// 
    /// VFX ASSET PROPERTIES:
    /// - Prefab: GameObject to instantiate
    /// - RelativeOffset: Position offset from spawn point
    /// - AngularRotation: Initial rotation
    /// - RelativeScale: Scale multiplier
    /// - Apex: Time when effect "hits" (for damage sync)
    /// - Duration: Total lifetime
    /// - IsLooping: Continuous vs one-shot
    /// 
    /// USAGE:
    /// ```csharp
    /// var vfx = VisualEffectLibrary.Get("AcidSplash");
    /// VisualEffectManager.Play(vfx, position);
    /// ```
    /// 
    /// RELATED FILES:
    /// - VisualEffectAsset.cs: VFX data structure
    /// - VisualEffectManager.cs: VFX spawning/playback
    /// - VisualEffectInstance.cs: VFX behavior
    /// </summary>
    public static class VisualEffectLibrary
    {
        private static Dictionary<string, VisualEffectAsset> visualEffects;
        private static bool isLoaded = false;

        public static Dictionary<string, VisualEffectAsset> VisualEffects
        {
            get
            {
                if (!isLoaded)
                    Load();
                return visualEffects;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;

            GameObject LoadPrefab(string key) => AssetHelper.LoadAsset<GameObject>(key);

            visualEffects = new Dictionary<string, VisualEffectAsset>
                {
                    {
                        "AcidSplash",
                        new VisualEffectAsset
                        {
                            Name = "AcidSplash",
                            Prefab = LoadPrefab("VisualEffects/AcidSplash"),
                            RelativeOffset = new Vector3(0f, 0.01f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0.1f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "AirSlash",
                        new VisualEffectAsset
                        {
                            Name = "AirSlash",
                            Prefab = LoadPrefab("VisualEffects/AirSlash"),
                            RelativeOffset = new Vector3(0.01f, -0.15f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.15f, 0.15f, 0.15f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BloodClaw",
                        new VisualEffectAsset
                        {
                            Name = "BloodClaw",
                            Prefab = LoadPrefab("VisualEffects/BloodClaw"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.15f, 0.15f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSlash1",
                        new VisualEffectAsset
                        {
                            Name = "BlueSlash1",
                            Prefab = LoadPrefab("VisualEffects/BlueSlash1"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0.1f),
                            Apex = 0.12f,
                            Duration = 0.35f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSlash2",
                        new VisualEffectAsset
                        {
                            Name = "BlueSlash2",
                            Prefab = LoadPrefab("VisualEffects/BlueSlash2"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0.1f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSlash3",
                        new VisualEffectAsset
                        {
                            Name = "BlueSlash3",
                            Prefab = LoadPrefab("VisualEffects/BlueSlash3"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(30f, 30f, 0f),
                            RelativeScale = new Vector3(0.08f, 0.08f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSlash4",
                        new VisualEffectAsset
                        {
                            Name = "BlueSlash4",
                            Prefab = LoadPrefab("VisualEffects/BlueSlash4"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.2f, 0.2f, 2f),
                            Apex = 0.08f,
                            Duration = 0.30f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSword",
                        new VisualEffectAsset
                        {
                            Name = "BlueSword",
                            Prefab = LoadPrefab("VisualEffects/BlueSword"),
                            RelativeOffset = new Vector3(0f, 0.05f, 0f),
                            AngularRotation = new Vector3(30f, 30f, 0f),
                            RelativeScale = new Vector3(0.12f, 0.08f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueSword4X",
                        new VisualEffectAsset
                        {
                            Name = "BlueSword4X",
                            Prefab = LoadPrefab("VisualEffects/BlueSword4X"),
                            RelativeOffset = new Vector3(-0.05f, -0.1f, 0f),
                            AngularRotation = new Vector3(30f, 30f, 0f),
                            RelativeScale = new Vector3(0.08f, 0.08f, 0f),
                            Apex = 0f,
                            Duration = 3f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueYellowSword",
                        new VisualEffectAsset
                        {
                            Name = "BlueYellowSword",
                            Prefab = LoadPrefab("VisualEffects/BlueYellowSword"),
                            RelativeOffset = new Vector3(0.03f, 0.01f, 0f),
                            AngularRotation = new Vector3(60f, 0f, 0f),
                            RelativeScale = new Vector3(0.07f, 0.07f, 0.07f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BlueYellowSword3X",
                        new VisualEffectAsset
                        {
                            Name = "BlueYellowSword3X",
                            Prefab = LoadPrefab("VisualEffects/BlueYellowSword3X"),
                            RelativeOffset = new Vector3(0.02f, -0.05f, 0f),
                            AngularRotation = new Vector3(60f, 0f, 0f),
                            RelativeScale = new Vector3(0.07f, 0.07f, 0.07f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "BuffLife",
                        new VisualEffectAsset
                        {
                            Name = "BuffLife",
                            Prefab = LoadPrefab("VisualEffects/BuffLife"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.16f, 0.16f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "DoubleClaw",
                        new VisualEffectAsset
                        {
                            Name = "DoubleClaw",
                            Prefab = LoadPrefab("VisualEffects/DoubleClaw"),
                            RelativeOffset = new Vector3(-0.03f, -0.1f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.12f, 0.12f, 0f),
                            Apex = 0.22f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "FireRain",
                        new VisualEffectAsset
                        {
                            Name = "FireRain",
                            Prefab = LoadPrefab("VisualEffects/FireRain"),
                            RelativeOffset = new Vector3(0.03f, -0.05f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0f),
                            Apex = 0f,
                            Duration = 4f,
                            IsLooping = false
                        }
                    },
                    {
                        "GodRays",
                        new VisualEffectAsset
                        {
                            Name = "GodRays",
                            Prefab = LoadPrefab("VisualEffects/GodRays"),
                            RelativeOffset = new Vector3(0f, -0.25f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.07f, 0.07f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "GoldBuff",
                        new VisualEffectAsset
                        {
                            Name = "GoldBuff",
                            Prefab = LoadPrefab("VisualEffects/GoldBuff"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.08f, 0.08f, 0.08f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "GreenBuff",
                        new VisualEffectAsset
                        {
                            Name = "GreenBuff",
                            Prefab = LoadPrefab("VisualEffects/GreenBuff"),
                            RelativeOffset = new Vector3(0.02f, -0.25f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.08f, 0.08f, 0.08f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "HexShield",
                        new VisualEffectAsset
                        {
                            Name = "HexShield",
                            Prefab = LoadPrefab("VisualEffects/HexShield"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.16f, 0.16f, 0.16f),
                            Apex = 0f,
                            Duration = 6f,
                            IsLooping = false
                        }
                    },
                    {
                        "LevelUp",
                        new VisualEffectAsset
                        {
                            Name = "LevelUp",
                            Prefab = LoadPrefab("VisualEffects/LevelUp"),
                            RelativeOffset = new Vector3(0f, -0.15f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.3f, 0.3f, 0f),
                            Apex = 0f,
                            Duration = 3f,
                            IsLooping = false
                        }
                    },
                    {
                        "LightningExplosion",
                        new VisualEffectAsset
                        {
                            Name = "LightningExplosion",
                            Prefab = LoadPrefab("VisualEffects/LightningExplosion"),
                            RelativeOffset = new Vector3(0f, -0.1f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0f),
                            Apex = 0f,
                            Duration = 3f,
                            IsLooping = false
                        }
                    },
                    {
                        "LightningStrike",
                        new VisualEffectAsset
                        {
                            Name = "LightningStrike",
                            Prefab = LoadPrefab("VisualEffects/LightningStrike"),
                            RelativeOffset = new Vector3(-0.07f, 0.1f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.05f, 0.05f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "MoonFeather",
                        new VisualEffectAsset
                        {
                            Name = "MoonFeather",
                            Prefab = LoadPrefab("VisualEffects/MoonFeather"),
                            RelativeOffset = new Vector3(0f, -0.02f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(4f, 4f, 0f),
                            Apex = 0f,
                            Duration = 3f,
                            IsLooping = false
                        }
                    },
                    {
                        "OrangeSlash",
                        new VisualEffectAsset
                        {
                            Name = "OrangeSlash",
                            Prefab = LoadPrefab("VisualEffects/OrangeSlash"),
                            RelativeOffset = new Vector3(-0.12f, 0.01f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.03f, 0.03f, 0.03f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "PinkSpark",
                        new VisualEffectAsset
                        {
                            Name = "PinkSpark",
                            Prefab = LoadPrefab("VisualEffects/PinkSpark"),
                            RelativeOffset = new Vector3(0f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.04f, 0.04f, 0.04f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "PuffyExplosion",
                        new VisualEffectAsset
                        {
                            Name = "PuffyExplosion",
                            Prefab = LoadPrefab("VisualEffects/PuffyExplosion"),
                            RelativeOffset = new Vector3(-0.02f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.2f, 0.2f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "RayBlast",
                        new VisualEffectAsset
                        {
                            Name = "RayBlast",
                            Prefab = LoadPrefab("VisualEffects/RayBlast"),
                            RelativeOffset = new Vector3(0.02f, -0.02f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.1f, 0.1f, 0f),
                            Apex = 0f,
                            Duration = 3f,
                            IsLooping = false
                        }
                    },
                    {
                        "RedSlash2X",
                        new VisualEffectAsset
                        {
                            Name = "RedSlash2X",
                            Prefab = LoadPrefab("VisualEffects/RedSlash2X"),
                            RelativeOffset = new Vector3(0.05f, -0.07f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.08f, 0.08f, 0f),
                            Apex = 0f,
                            Duration = 1f,
                            IsLooping = false
                        }
                    },
                    {
                        "RedSword",
                        new VisualEffectAsset
                        {
                            Name = "RedSword",
                            Prefab = LoadPrefab("VisualEffects/RedSword"),
                            RelativeOffset = new Vector3(-0.06f, 0.05f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 142f),
                            RelativeScale = new Vector3(0.2f, 0.2f, 0.2f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "RotaryKnife",
                        new VisualEffectAsset
                        {
                            Name = "RotaryKnife",
                            Prefab = LoadPrefab("VisualEffects/RotaryKnife"),
                            RelativeOffset = new Vector3(0.03f, -0.05f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.25f, 0.25f, 0f),
                            Apex = 0f,
                            Duration = 1f,
                            IsLooping = false
                        }
                    },
                    {
                        "ToxicCloud",
                        new VisualEffectAsset
                        {
                            Name = "ToxicCloud",
                            Prefab = LoadPrefab("VisualEffects/ToxicCloud"),
                            RelativeOffset = new Vector3(-0.02f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.15f, 0.15f, 0.15f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },
                    {
                        "YellowHit",
                        new VisualEffectAsset
                        {
                            Name = "YellowHit",
                            Prefab = LoadPrefab("VisualEffects/YellowHit"),
                            RelativeOffset = new Vector3(-0.02f, 0f, 0f),
                            AngularRotation = new Vector3(0f, 0f, 0f),
                            RelativeScale = new Vector3(0.2f, 0.2f, 0f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = false
                        }
                    },

                    // Looping VFX
                    {
                        "BlueGlow",
                        new VisualEffectAsset
                        {
                            Name = "BlueGlow",
                            Prefab = LoadPrefab("VisualEffects/BlueGlow"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "Bubble",
                        new VisualEffectAsset
                        {
                            Name = "Bubble",
                            Prefab = LoadPrefab("VisualEffects/Bubble"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "Feather",
                        new VisualEffectAsset
                        {
                            Name = "Feather",
                            Prefab = LoadPrefab("VisualEffects/Feather"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "Fireball",
                        new VisualEffectAsset
                        {
                            Name = "Fireball",
                            Prefab = LoadPrefab("VisualEffects/Fireball"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "Flame",
                        new VisualEffectAsset
                        {
                            Name = "Flame",
                            Prefab = LoadPrefab("VisualEffects/Flame"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "GoldSparkle",
                        new VisualEffectAsset
                        {
                            Name = "GoldSparkle",
                            Prefab = LoadPrefab("VisualEffects/GoldSparkle"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                             RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "GreenSparkle",
                        new VisualEffectAsset
                        {
                            Name = "GreenSparkle",
                            Prefab = LoadPrefab("VisualEffects/GreenSparkle"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(2f, 2f, 0f),
                            IsLooping = true
                        }
                    },
                    {
                        "IceSparkle",
                        new VisualEffectAsset
                        {
                            Name = "IceSparkle",
                            Prefab = LoadPrefab("VisualEffects/IceSparkle"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            IsLooping = true
                        }
                    },
                    {
                        "PinkDust",
                        new VisualEffectAsset
                        {
                            Name = "PinkDust",
                            Prefab = LoadPrefab("VisualEffects/PinkDust"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(1, 1, 1),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = true
                        }
                    },
                    {
                        "RosePetal",
                        new VisualEffectAsset
                        {
                            Name = "RosePetal",
                            Prefab = LoadPrefab("VisualEffects/RosePetal"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(0.1f, 0.1f, 0.1f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = true
                        }
                    },
                    {
                        "StarSparkle",
                        new VisualEffectAsset
                        {
                            Name = "StarSparkle",
                            Prefab = LoadPrefab("VisualEffects/StarSparkle"),
                            RelativeOffset = Vector3.zero,
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(0.1f, 0.1f, 0.1f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = true
                        }
                    },
                    {
                        "TechSword",
                        new VisualEffectAsset
                        {
                            Name = "TechSword",
                            Prefab = LoadPrefab("VisualEffects/TechSword"),
                            RelativeOffset = new Vector3(0.02f, 0, 0),
                            AngularRotation = Vector3.zero,
                            RelativeScale = new Vector3(0.08f, 0.08f, 0.08f),
                            Apex = 0f,
                            Duration = 2f,
                            IsLooping = true
                        }
                    },
                };

            isLoaded = true;
        }

        public static VisualEffectAsset Get(string name)
        {
            if (!isLoaded) Load();
            var data = visualEffects.ContainsKey(name) ? visualEffects[name] : null;
            if (data == null)
                Debug.LogError($"Unable to retrieve visual effect for `{name}`");
            return data != null ? new VisualEffectAsset(data) : null;
        }
    }
}
