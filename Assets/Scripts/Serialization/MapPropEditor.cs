using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
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
using Scripts.Utilities;
#endif

namespace Scripts.Serialization
{
    // Implement on components to emit/apply per-instance override data.
    public interface IPropSerializable
    {
        // Return JSON representing component instance state you want to override from prefab.
        string SerializeToJson();
        // Apply JSON into this component instance.
        void DeserializeFromJson(string json);
    }

    [Serializable]
    public sealed class PropMap
    {
        public List<PropNode> roots = new List<PropNode>();
    }

    [Serializable]
    public sealed class PropComponentData
    {
        public string type; // AssemblyQualifiedName
        public string json; // Component-specific JSON
    }

    [Serializable]
    public sealed class PropNode
    {
        public string id; // stable instance id for diff/merge
        public string name;
        public string prefab; // Resources path without extension (e.g., "PropsLibrary/Tree_A")
        public bool active = true;
        public int layer = 0;
        public string tag = "Untagged";

        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale = Vector3.one;

        public List<PropComponentData> overrides = new List<PropComponentData>();
        public List<PropNode> children = new List<PropNode>();
    }

    // Stored on scene objects to keep a stable instance id for diff/merge
    public sealed class StableIdHolder : MonoBehaviour
    {
        public string value;
    }

    public static class PropMapIO
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string LibraryFolder = "PropsLibrary";
        private const string BakedFolder = "_Baked";

        // Runtime: Load JSON and rebuild under root.
        /// <summary>Load into.</summary>
        public static bool LoadInto(Transform propsRoot, string mapPath, bool clearExisting = true)
        {
            if (propsRoot == null)
            {
                Debug.LogError("PropMapIO.LoadInto: propsRoot is null.");
                return false;
            }

            TextAsset json = Resources.Load<TextAsset>(mapPath);
            if (json == null)
            {
                Debug.LogWarning($"PropMapIO.LoadInto: No JSON found at Resources path '{mapPath}'.");
                return false;
            }

            var map = JsonUtility.FromJson<PropMap>(json.text);
            if (map == null)
            {
                Debug.LogError($"PropMapIO.LoadInto: Failed to parse JSON at '{mapPath}'.");
                return false;
            }

            if (clearExisting)
                DestroyAllChildrenImmediate(propsRoot);

            foreach (var node in map.roots)
                InstantiateNode(node, propsRoot);

            return true;
        }

        /// <summary>Instantiate node.</summary>
        private static void InstantiateNode(PropNode node, Transform parent)
        {
            GameObject go;
            if (!string.IsNullOrEmpty(node.prefab))
            {
                var prefab = Resources.Load<GameObject>(node.prefab);
                if (prefab != null)
                {
                    go = UnityEngine.Object.Instantiate(prefab, parent, false);
                    go.name = node.name; // keep friendly name from JSON
                }
                else
                {
                    Debug.LogWarning($"PropMapIO: Prefab not found in Resources at '{node.prefab}'. Creating empty object instead.");
                    go = new GameObject(node.name);
                    go.transform.SetParent(parent, false);
                }
            }
            else
            {
                go = new GameObject(node.name);
                go.transform.SetParent(parent, false);
            }

            var t = go.transform;
            t.localPosition = node.localPosition;
            t.localRotation = node.localRotation;
            t.localScale = node.localScale;

            // Tag/layer safety
            if (!string.IsNullOrEmpty(node.tag) && UnityTagExists(node.tag))
                go.tag = node.tag;

            go.layer = node.layer;

            // Apply component overrides
            if (node.overrides != null && node.overrides.Count > 0)
            {
                foreach (var ov in node.overrides)
                {
                    var type = System.Type.GetType(ov.type);
                    if (type == null) continue;
                    var comp = go.GetComponent(type) as IPropSerializable;
                    comp?.DeserializeFromJson(ov.json);
                }
            }

            foreach (var child in node.children)
                InstantiateNode(child, t);

            go.SetActive(node.active);
        }

        /// <summary> destroy all children immediate..Groups[0].Value.ToUpper() estroy all children immediate.</summary>
        private static void DestroyAllChildrenImmediate(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var c = root.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(c.gameObject);
                else UnityEngine.Object.Destroy(c.gameObject);
#else
                UnityEngine.Object.Destroy(c.gameObject);
#endif
            }
        }

        /// <summary>Unity tag exists.</summary>
        private static bool UnityTagExists(string tag)
        {
            try { var _ = GameObject.FindWithTag(tag); return true; }
            catch { return false; }
        }

#if UNITY_EDITOR
        // Editor: Capture hierarchy to JSON under Assets/Resources/<mapPath>.json
        /// <summary>Save from.</summary>
        public static bool SaveFrom(Transform propsRoot, string mapPath, bool includeInactive = true, bool bakeOrphans = true)
        {
            if (propsRoot == null)
            {
                Debug.LogError("PropMapIO.SaveFrom: propsRoot is null.");
                return false;
            }

            var map = new PropMap();
            for (int i = 0; i < propsRoot.childCount; i++)
            {
                var child = propsRoot.GetChild(i);
                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;

                map.roots.Add(BuildNodeRecursive(child, bakeOrphans));
            }

            string pretty = JsonUtility.ToJson(map, true);
            string assetPath = ToResourcesAssetPath(mapPath);

            EnsureFolder(Path.GetDirectoryName(assetPath));
            File.WriteAllText(assetPath, pretty);
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"PropMapIO: Saved map JSON -> {assetPath}");
            return true;
        }

        /// <summary>Creates the node recursive.</summary>
        private static PropNode BuildNodeRecursive(Transform t, bool bakeOrphans)
        {
            var node = new PropNode
            {
                id = GetOrMakeStableId(t),
                name = t.gameObject.name,
                prefab = GetPrefabResourcePath(t.gameObject, bakeOrphans),
                active = t.gameObject.activeSelf,
                layer = t.gameObject.layer,
                tag = t.gameObject.tag,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            };

            // If we baked a prefab for this node, it already contains the full snapshot
            // (components and children). To avoid duplication and to ensure an exact copy,
            // do NOT serialize component overrides or children for this node.
            if (!string.IsNullOrEmpty(node.prefab))
            {
                return node;
            }

            // Capture overrides for components implementing IPropSerializable
            var comps = t.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c is Transform || c == null) continue;
                if (c is IPropSerializable ser)
                {
                    string json = ser.SerializeToJson();
                    if (!string.IsNullOrEmpty(json))
                    {
                        node.overrides.Add(new PropComponentData
                        {
                            type = c.GetType().AssemblyQualifiedName,
                            json = json
                        });
                    }
                }
            }

            // Recurse children only when not baked as a prefab
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                node.children.Add(BuildNodeRecursive(c, bakeOrphans));
            }

            return node;
        }

        /// <summary>Gets the prefab resource path.</summary>
        private static string GetPrefabResourcePath(GameObject go, bool bakeOrphans)
        {
            GameObject srcPrefab = null;
            try
            {
                srcPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(go);
            }
            catch { }

            if (srcPrefab != null)
            {
                string srcPath = UnityEditor.AssetDatabase.GetAssetPath(srcPrefab);
                return EnsurePrefabInResources(srcPath);
            }

            if (!bakeOrphans)
                return string.Empty;

            // Bake a one-off prefab for scene-only objects so they can be loaded in builds.
            string targetFolder = Path.Combine(ResourcesRoot, LibraryFolder, BakedFolder).Replace("\\", "/");
            EnsureFolder(targetFolder);

            string safeName = SanitizeFileName(go.name);
            string file = $"{safeName}_{Guid.NewGuid():N}.prefab";
            string assetPath = $"{targetFolder}/{file}";

            // Duplicate to avoid saving editor-only bits
            var temp = UnityEngine.Object.Instantiate(go);
            temp.name = go.name;
            StripEditorOnly(temp);

            UnityEditor.PrefabUtility.SaveAsPrefabAsset(temp, assetPath);
            UnityEngine.Object.DestroyImmediate(temp);

            UnityEditor.AssetDatabase.ImportAsset(assetPath);
            return ToResourcesPath(assetPath);
        }

        /// <summary>Ensure prefab in resources.</summary>
        private static string EnsurePrefabInResources(string srcAssetPath)
        {
            srcAssetPath = srcAssetPath.Replace("\\", "/");
            if (srcAssetPath.StartsWith($"{ResourcesRoot}/"))
            {
                // Already in Resources
                return ToResourcesPath(srcAssetPath);
            }

            // Copy into Resources/PropsLibrary to ensure it's loadable at runtime
            string fileName = Path.GetFileName(srcAssetPath);
            string dstFolder = Path.Combine(ResourcesRoot, LibraryFolder).Replace("\\", "/");
            EnsureFolder(dstFolder);

            string dstAssetPath = $"{dstFolder}/{fileName}";
            if (!File.Exists(dstAssetPath))
            {
                if (!UnityEditor.AssetDatabase.CopyAsset(srcAssetPath, dstAssetPath))
                    Debug.LogError($"PropMapIO: Failed to copy '{srcAssetPath}' to '{dstAssetPath}'.");
                else
                    UnityEditor.AssetDatabase.ImportAsset(dstAssetPath);
            }

            return ToResourcesPath(dstAssetPath);
        }

        /// <summary>To resources asset path.</summary>
        private static string ToResourcesAssetPath(string mapPath)
        {
            mapPath = mapPath.Trim('/').Trim('\\');
            string full = Path.Combine(ResourcesRoot, mapPath + ".json").Replace("\\", "/");
            return full;
        }

        /// <summary>To resources path.</summary>
        private static string ToResourcesPath(string assetPath)
        {
            assetPath = assetPath.Replace("\\", "/");
            if (!assetPath.StartsWith($"{ResourcesRoot}/"))
                return string.Empty;

            string rel = assetPath.Substring($"{ResourcesRoot}/".Length);
            if (rel.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                rel = rel.Substring(0, rel.Length - ".prefab".Length);

            return rel; // e.g., "PropsLibrary/Tree_A"
        }

        /// <summary>Ensure folder.</summary>
        private static void EnsureFolder(string folderAssetPath)
        {
            if (string.IsNullOrEmpty(folderAssetPath)) return;
            folderAssetPath = folderAssetPath.Replace("\\", "/");

            if (UnityEditor.AssetDatabase.IsValidFolder(folderAssetPath))
                return;

            string[] parts = folderAssetPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                    UnityEditor.AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        /// <summary>Sanitize file name.</summary>
        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>Strip editor only.</summary>
        private static void StripEditorOnly(GameObject go)
        {
            // Remove StableIdHolder and any other editor-only helpers from the baked prefab.
#if UNITY_EDITOR
            RemoveComponentsRecursively<StableIdHolder>(go.transform);
#endif
        }

#if UNITY_EDITOR
        private static void RemoveComponentsRecursively<T>(Transform root) where T : Component
        {
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                var comps = t.GetComponents<T>();
                for (int i = comps.Length - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(comps[i]);
                }
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
        }
#endif

        // Diff & merge helpers --------------------------------------------------------
        /// <summary>Merge maps.</summary>
        public static PropMap MergeMaps(PropMap original, PropMap incoming)
        {
            var byId = new Dictionary<string, PropNode>();
            foreach (var n in original.roots)
                IndexRecursive(n, byId);

            // Merge/replace roots by id; if new id, append
            foreach (var inRoot in incoming.roots)
            {
                if (!string.IsNullOrEmpty(inRoot.id) && byId.TryGetValue(inRoot.id, out var match))
                {
                    MergeNode(match, inRoot);
                }
                else
                {
                    original.roots.Add(inRoot);
                    IndexRecursive(inRoot, byId);
                }
            }

            return original;
        }

        /// <summary>Index recursive.</summary>
        private static void IndexRecursive(PropNode n, Dictionary<string, PropNode> index)
        {
            if (!string.IsNullOrEmpty(n.id) && !index.ContainsKey(n.id))
                index.Add(n.id, n);
            foreach (var c in n.children)
                IndexRecursive(c, index);
        }

        /// <summary>Merge node.</summary>
        private static void MergeNode(PropNode dst, PropNode src)
        {
            dst.name = src.name;
            dst.prefab = src.prefab;
            dst.active = src.active;
            dst.layer = src.layer;
            dst.tag = src.tag;
            dst.localPosition = src.localPosition;
            dst.localRotation = src.localRotation;
            dst.localScale = src.localScale;
            dst.overrides = src.overrides ?? new List<PropComponentData>();

            // Merge children by id
            var map = new Dictionary<string, PropNode>();
            foreach (var c in dst.children)
                if (!string.IsNullOrEmpty(c.id)) map[c.id] = c;

            var newChildren = new List<PropNode>();
            foreach (var sc in src.children)
            {
                if (!string.IsNullOrEmpty(sc.id) && map.TryGetValue(sc.id, out var dc))
                {
                    MergeNode(dc, sc);
                    newChildren.Add(dc);
                }
                else
                {
                    newChildren.Add(sc);
                }
            }
            dst.children = newChildren;
        }

        /// <summary>Gets the or make stable id.</summary>
        private static string GetOrMakeStableId(Transform t)
        {
            // Generate stable id based on path + a GUID component stored on a helper
            var id = t.GetComponent<StableIdHolder>();
            if (id == null)
            {
                id = t.gameObject.AddComponent<StableIdHolder>();
                id.value = System.Guid.NewGuid().ToString("N");
            }
            return id.value;
        }
#endif
    }

    // Attach anywhere in your scene during playtesting to enable Ctrl/Cmd+Alt+S save of Props JSON.
    public sealed class PropMapHotkeys : MonoBehaviour
    {
        [SerializeField] private MapPropEditorBootstrapper loader;
        [SerializeField] private KeyCode key = KeyCode.S;
        [SerializeField] private bool requireCtrl = true;
        [SerializeField] private bool requireAlt = true;

#if UNITY_EDITOR
        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            if (loader == null) return;

            if (Input.GetKeyDown(key))
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                if ((!requireCtrl || ctrl) && (!requireAlt || alt))
                {
                    var root = loader.GetType()
                        .GetField("propsRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(loader) as Transform;

                    var mapPath = loader.GetType()
                        .GetField("mapPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(loader) as string;

                    if (root != null && !string.IsNullOrEmpty(mapPath))
                    {
                        PropMapIO.SaveFrom(root, mapPath, includeInactive: true, bakeOrphans: true);
                        Debug.Log($"Saved Props to JSON via hotkey -> {mapPath}");
                    }
                }
            }
        }
#endif
    }
}
