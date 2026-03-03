using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.IO;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

/// <summary>
/// Editor tool to mark assets as Addressable programmatically.
/// Run from Tools menu after adding new sprites/materials.
/// </summary>
public class AddressableMarker : Editor
{
    [MenuItem("Tools/Create SpriteUnlitDefault Material")]
    /// <summary>Creates the sprite unlit default material.</summary>
    public static void CreateSpriteUnlitDefaultMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            Debug.LogError("Could not find Sprite-Unlit-Default shader");
            return;
        }

        var mat = new Material(shader);
        mat.name = "SpriteUnlitDefault";

        // Ensure directory exists
        if (!Directory.Exists("Assets/Materials"))
            Directory.CreateDirectory("Assets/Materials");

        AssetDatabase.CreateAsset(mat, "Assets/Materials/SpriteUnlitDefault.mat");
        AssetDatabase.SaveAssets();
        Debug.Log("Created SpriteUnlitDefault.mat at Assets/Materials/");
    }

    [MenuItem("Tools/Mark Actor Sprites as Addressable")]
    /// <summary>Mark actor sprites as addressable.</summary>
    public static void MarkActorSpritesAsAddressable()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. Please create Addressables settings first.");
            return;
        }

        // Get or create the default group
        var group = settings.DefaultGroup;
        if (group == null)
        {
            Debug.LogError("No default Addressable group found.");
            return;
        }

        // Define all assets to mark as addressable
        var assetsToMark = new Dictionary<string, string>
        {
            // Actor sprites
            { "Assets/Sprites/Actor/Masks/mask-4.png", "Sprites/Actor/Masks/mask-4" },
            { "Assets/Sprites/Actor/Masks/mask-7.png", "Sprites/Actor/Masks/mask-7" },
            { "Assets/Sprites/Actor/Base/base-4.png", "Sprites/Actor/Base/base-4" },
            { "Assets/Sprites/Actor/Back/back-2.png", "Sprites/Actor/Back/back-2" },
            { "Assets/Sprites/Actor/Frames/frame-4.png", "Sprites/Actor/Frames/frame-4" },
            { "Assets/Sprites/Actor/Thumbnails/thumbnail-fade.png", "Sprites/Actor/Thumbnails/thumbnail-fade" },
            { "Assets/Sprites/Actor/Gradient/Gradient.png", "Sprites/Actor/Gradient/Gradient" },
            { "Assets/Sprites/Statuses/status-none.png", "Sprites/Statuses/status-none" },

            // Health bar sprites
            { "Assets/Sprites/HealthBar/health-bar-5.png", "Sprites/HealthBar/health-bar-5" },
            { "Assets/Sprites/HealthBar/health-bar-back-3.png", "Sprites/HealthBar/health-bar-back-3" },
            { "Assets/Sprites/HealthBar/health-bar-3.png", "Sprites/HealthBar/health-bar-3" },
            { "Assets/Sprites/ActionBar/action-bar-2.png", "Sprites/ActionBar/action-bar-2" },

            // Radial sprites
            { "Assets/Sprites/ActionBar/ring-back-1.png", "Sprites/ActionBar/ring-back-1" },
            { "Assets/Sprites/ActionBar/ring-1.png", "Sprites/ActionBar/ring-1" },

            // Armor sprites
            { "Assets/Sprites/Actor/Armor/armor-north.png", "Sprites/Actor/Armor/armor-north" },
            { "Assets/Sprites/Actor/Armor/armor-east.png", "Sprites/Actor/Armor/armor-east" },
            { "Assets/Sprites/Actor/Armor/armor-south.png", "Sprites/Actor/Armor/armor-south" },
            { "Assets/Sprites/Actor/Armor/armor-west.png", "Sprites/Actor/Armor/armor-west" },

            // Indicator sprites (root Sprites folder)
            { "Assets/Sprites/active-indicator.png", "Sprites/active-indicator" },
            { "Assets/Sprites/focus-indicator.png", "Sprites/focus-indicator" },
            { "Assets/Sprites/target-indicator.png", "Sprites/target-indicator" },

            // Materials
            { "Assets/Materials/RadialFill.mat", "Materials/RadialFill" },
            { "Assets/Materials/SpriteUnlitDefault.mat", "Materials/SpriteUnlitDefault" },
        };

        int successCount = 0;
        int failCount = 0;

        foreach (var kvp in assetsToMark)
        {
            string assetPath = kvp.Key;
            string address = kvp.Value;

            // Get the GUID
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"Asset not found: {assetPath}");
                failCount++;
                continue;
            }

            // Check if already addressable
            var existingEntry = settings.FindAssetEntry(guid);
            if (existingEntry != null)
            {
                // Update address if different
                if (existingEntry.address != address)
                {
                    existingEntry.address = address;
                    Debug.Log($"Updated address: {assetPath} -> {address}");
                }
                else
                {
                    Debug.Log($"Already addressable: {assetPath}");
                }
                successCount++;
                continue;
            }

            // Create new entry
            var entry = settings.CreateOrMoveEntry(guid, group);
            if (entry != null)
            {
                entry.address = address;
                Debug.Log($"Marked as addressable: {assetPath} -> {address}");
                successCount++;
            }
            else
            {
                Debug.LogError($"Failed to create entry: {assetPath}");
                failCount++;
            }
        }

        // Save settings
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"=== Addressable Marking Complete ===");
        Debug.Log($"Success: {successCount}, Failed: {failCount}");
        
        if (failCount > 0)
        {
            Debug.LogWarning("Some assets failed. Check if the files exist at the specified paths.");
        }
    }

    [MenuItem("Tools/Build Addressables (Editor)")]
    /// <summary>Creates the addressables.</summary>
    public static void BuildAddressables()
    {
        // For Editor play mode, just refresh the asset database
        // Addressables in "Use Asset Database" mode don't need a build
        AssetDatabase.Refresh();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            // Set to "Use Asset Database" mode for Editor testing
            settings.ActivePlayModeDataBuilderIndex = 0; // Usually index 0 is "Use Asset Database"
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("Addressables refreshed for Editor play mode!");
        Debug.Log("Note: For actual builds, use Window > Asset Management > Addressables > Groups > Build");
    }
}
