#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
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

namespace Scripts.Editor
{
public static class TimelinePrefabGenerator
{
 [MenuItem("Tools/Generate Timeline Prefabs")] 
 public static void Generate()
 {
 // Create TimelineTag prefab
 var tagGo = new GameObject("TimelineTag");
 var tagRect = tagGo.AddComponent<RectTransform>();
 var img = tagGo.AddComponent<Image>();
 img.color = new Color(0f,0.5f,0.5f,1f);
 var cg = tagGo.AddComponent<CanvasGroup>();
 var tag = tagGo.AddComponent<TimelineTag>();
 tag.Wire(img, cg);
 tagRect.sizeDelta = new Vector2(12,20);

 string folder = "Assets/Prefabs";
 if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
 string tagPath = folder + "/TimelineTag.prefab";
 var tagPrefab = PrefabUtility.SaveAsPrefabAsset(tagGo, tagPath);
 Object.DestroyImmediate(tagGo);

 // Create TimelineBar prefab (bar with left line and spawn zone)
 var barGo = new GameObject("TimelineBar");
 var barRect = barGo.AddComponent<RectTransform>();
 var bar = barGo.AddComponent<TimelineBarInstance>();
 barRect.sizeDelta = new Vector2(400,8);

 // Background line
 var bg = new GameObject("Line");
 bg.transform.SetParent(barGo.transform, false);
 var bgRect = bg.AddComponent<RectTransform>();
 var bgImage = bg.AddComponent<Image>();
 bgImage.color = Color.black;
 bgRect.anchorMin = new Vector2(0,0.5f);
 bgRect.anchorMax = new Vector2(1,0.5f);
 bgRect.pivot = new Vector2(0.5f,0.5f);
 bgRect.sizeDelta = new Vector2(0,2);

 // Left edge indicator (thin red tick)
 var left = new GameObject("Left");
 left.transform.SetParent(barGo.transform, false);
 var leftRect = left.AddComponent<RectTransform>();
 var leftImage = left.AddComponent<Image>();
 leftImage.color = Color.red;
 leftRect.anchorMin = new Vector2(0,0.5f);
 leftRect.anchorMax = new Vector2(0,0.5f);
 leftRect.pivot = new Vector2(0,0.5f);
 leftRect.sizeDelta = new Vector2(2,12);
 leftRect.anchoredPosition = new Vector2(0,0);

 // Spawn anchor on the right
 var spawn = new GameObject("Spawn");
 spawn.transform.SetParent(barGo.transform, false);
 var spawnRect = spawn.AddComponent<RectTransform>();
 spawnRect.anchorMin = new Vector2(1,0.5f);
 spawnRect.anchorMax = new Vector2(1,0.5f);
 spawnRect.pivot = new Vector2(1,0.5f);
 spawnRect.anchoredPosition = new Vector2(0,0);

 // Wire bar parts
 var so = new SerializedObject(bar);
 so.FindProperty("barRect").objectReferenceValue = barRect;
 so.FindProperty("spawnRect").objectReferenceValue = spawnRect;
 so.FindProperty("leftLine").objectReferenceValue = leftRect;
 so.ApplyModifiedPropertiesWithoutUndo();

 // Assign tag prefab reference
 var tagPrefabAsset = AssetDatabase.LoadAssetAtPath<TimelineTag>(tagPath);
 so = new SerializedObject(bar);
 so.FindProperty("tagPrefab").objectReferenceValue = tagPrefabAsset;
 so.ApplyModifiedPropertiesWithoutUndo();

 string barPath = folder + "/TimelineBar.prefab";
 PrefabUtility.SaveAsPrefabAsset(barGo, barPath);
 Object.DestroyImmediate(barGo);

 AssetDatabase.SaveAssets();
 AssetDatabase.Refresh();
 Debug.Log("Generated TimelineTag and TimelineBar prefabs in Assets/Prefabs.");
 }
}
#endif

}