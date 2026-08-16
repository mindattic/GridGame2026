#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

/// <summary>
/// ADDRESSABLESGROUPFIXER - Repairs schema-less Addressable groups (US-124 final gate).
///
/// <para>The player build's Addressables content pass warns "X does not have any associated
/// AddressableAssetGroupSchemas — data from this group will not be included" for the Fonts and
/// TrailEffects groups, then dies with an opaque NullReferenceException inside
/// BuildScriptPackedMode.ProcessGroup. A group created without schemas can't be packed; this
/// gives every schema-less group the standard BundledAssetGroupSchema (+ content-update
/// schema), copying the default group's settings. Idempotent.</para>
///
/// <para>Menu: Tools/Audio/Repair Addressable Group Schemas.
/// Batch: -executeMethod AddressablesGroupFixer.RepairSchemalessGroups.</para>
/// </summary>
public static class AddressablesGroupFixer
{
    [MenuItem("Tools/Audio/Repair Addressable Group Schemas")]
    public static void RepairSchemalessGroups()
    {
#if UNITY_2020_2_OR_NEWER
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) { Debug.LogWarning("[AddressablesGroupFixer] No Addressables settings."); return; }

        int repaired = 0;
        foreach (var group in settings.groups)
        {
            if (group == null || group.ReadOnly) continue;
            if (group.Schemas != null && group.Schemas.Count > 0) continue;

            group.AddSchema<BundledAssetGroupSchema>();
            group.AddSchema<ContentUpdateGroupSchema>();
            EditorUtility.SetDirty(group);
            Debug.Log($"[AddressablesGroupFixer] Added schemas to group '{group.Name}'.");
            repaired++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AddressablesGroupFixer] Repaired {repaired} schema-less group(s).");
#endif
    }
}
#endif
