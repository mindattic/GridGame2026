#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

/// <summary>
/// AUDIOADDRESSABLEREGISTRAR - Registers every audio file under Assets/MusicTracks and
/// Assets/SoundEffects as an Addressable with the address pattern the libraries expect
/// ("MusicTracks/&lt;name&gt;" / "SoundEffects/&lt;name&gt;"), idempotently (US-137 / GG-A5).
/// Run after dropping new royalty-free audio into either folder — and add the matching
/// attribution row in Data/AudioCredits.cs.
///
/// Menu: Tools/Audio/Register Audio Addressables.
/// Batch: -executeMethod AudioAddressableRegistrar.RegisterAll.
/// </summary>
public static class AudioAddressableRegistrar
{
    [MenuItem("Tools/Audio/Register Audio Addressables")]
    public static void RegisterAll()
    {
#if UNITY_2020_2_OR_NEWER
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) { Debug.LogWarning("[AudioAddressableRegistrar] No Addressables settings."); return; }

        int registered = 0;
        foreach (var folder in new[] { "Assets/MusicTracks", "Assets/SoundEffects" })
        {
            if (!Directory.Exists(folder)) continue;
            string prefix = Path.GetFileName(folder);
            foreach (var file in Directory.GetFiles(folder))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".mp3" && ext != ".ogg" && ext != ".wav") continue;

                var assetPath = file.Replace('\\', '/');
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                string address = $"{prefix}/{Path.GetFileNameWithoutExtension(file)}";
                var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                if (entry.address != address) { entry.address = address; registered++; }
            }
        }
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioAddressableRegistrar] Registered/updated {registered} audio addressables.");
#endif
    }
}
#endif
