//using UnityEngine;
//using UnityEngine.UI;
//using UnityEditor;
//using TMPro;

///// <summary>
///// HUBSCENEVALIDATOR - Editor tool to scaffold Hub scene panels.
/////
///// PURPOSE:
///// Ensures every Hub section panel has the required child
///// GameObjects (containers, labels, buttons) that the runtime
///// SectionControllers resolve via transform.Find().
/////
///// USAGE:
///// Menu → Tools → Hub Scene → Validate &amp; Scaffold Panels
/////
///// Safe to run multiple times — skips children that already exist.
///// </summary>
//public static class HubSceneValidator
//{
//    [MenuItem("Tools/Hub Scene/Validate && Scaffold Panels")]
//    public static void ValidateAndScaffold()
//    {
//        int created = 0;
//        int found = 0;

//        // ── PartyPanel ──
//        created += ScaffoldPanel("PartyPanel", new[]
//        {
//            ("RosterList",  ChildKind.Container),
//            ("PartyList",   ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//        }, ref found);

//        // ── ShopPanel ──
//        created += ScaffoldPanel("ShopPanel", new[]
//        {
//            ("ItemList",    ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//        }, ref found);

//        // ── MedicalPanel ──
//        created += ScaffoldPanel("MedicalPanel", new[]
//        {
//            ("HeroList",    ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//        }, ref found);

//        // ── ResidencePanel ──
//        created += ScaffoldPanel("ResidencePanel", new[]
//        {
//            ("ActionList",  ChildKind.Container),
//            ("RecruitList", ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//        }, ref found);

//        // ── BlacksmithPanel ──
//        created += ScaffoldPanel("BlacksmithPanel", new[]
//        {
//            ("ItemList",    ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//            ("BuyTab",      ChildKind.Button),
//            ("SellTab",     ChildKind.Button),
//            ("CraftTab",    ChildKind.Button),
//            ("RepairTab",   ChildKind.Button),
//            ("SalvageTab",  ChildKind.Button),
//        }, ref found);

//        // ── TrainingPanel ──
//        created += ScaffoldPanel("TrainingPanel", new[]
//        {
//            ("HeroList",      ChildKind.Container),
//            ("TrainingList",  ChildKind.Container),
//            ("GoldLabel",     ChildKind.Label),
//            ("DetailLabel",   ChildKind.Label),
//            ("VendorPortrait", ChildKind.Image),
//        }, ref found);

//        // ── EquipPanel ──
//        created += ScaffoldPanel("EquipPanel", new[]
//        {
//            ("HeroList",    ChildKind.Container),
//            ("SlotList",    ChildKind.Container),
//            ("ItemPicker",  ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("StatsLabel",  ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//        }, ref found);

//        // ── InventoryPanel ──
//        created += ScaffoldPanel("InventoryPanel", new[]
//        {
//            ("ItemList",    ChildKind.Container),
//            ("GoldLabel",   ChildKind.Label),
//            ("DetailLabel", ChildKind.Label),
//            ("FilterAll",   ChildKind.Button),
//            ("FilterEquip", ChildKind.Button),
//            ("FilterCons",  ChildKind.Button),
//            ("FilterMats",  ChildKind.Button),
//            ("FilterQuest", ChildKind.Button),
//        }, ref found);

//        // ── BattlePrepPanel ──
//        created += ScaffoldPanel("BattlePrepPanel", new[]
//        {
//            ("PartyList",    ChildKind.Container),
//            ("ItemList",     ChildKind.Container),
//            ("GoldLabel",    ChildKind.Label),
//            ("DetailLabel",  ChildKind.Label),
//            ("BattleButton", ChildKind.Button),
//        }, ref found);

//        Debug.Log($"[HubSceneValidator] Done. {found} already existed, {created} created.");
//        if (created > 0)
//            EditorUtility.DisplayDialog("Hub Scene Scaffold",
//                $"Created {created} missing child object(s).\n{found} already existed.\n\nRemember to save the scene.",
//                "OK");
//        else
//            EditorUtility.DisplayDialog("Hub Scene Scaffold",
//                $"All {found} required children already exist.\nNo changes needed.",
//                "OK");
//    }

//    // ── Child types ──

//    private enum ChildKind { Container, Label, Button, Image }

//    // ── Core scaffolding ──

//    private static int ScaffoldPanel(string panelName, (string name, ChildKind kind)[] children, ref int foundCount)
//    {
//        var panelGO = GameObject.Find(panelName);
//        if (panelGO == null)
//        {
//            Debug.LogWarning($"[HubSceneValidator] Panel '{panelName}' not found in scene. Skipping.");
//            return 0;
//        }

//        var panelRT = panelGO.GetComponent<RectTransform>();
//        if (panelRT == null)
//        {
//            panelRT = panelGO.AddComponent<RectTransform>();
//            Undo.RegisterCreatedObjectUndo(panelGO, $"Add RectTransform to {panelName}");
//        }

//        int created = 0;
//        foreach (var (childName, kind) in children)
//        {
//            var existing = panelRT.Find(childName);
//            if (existing != null)
//            {
//                foundCount++;
//                continue;
//            }

//            var child = CreateChild(panelRT, childName, kind);
//            Undo.RegisterCreatedObjectUndo(child, $"Create {panelName}/{childName}");
//            created++;
//            Debug.Log($"[HubSceneValidator] Created: {panelName}/{childName} ({kind})");
//        }

//        return created;
//    }

//    private static GameObject CreateChild(RectTransform parent, string name, ChildKind kind)
//    {
//        var go = new GameObject(name);
//        go.layer = LayerMask.NameToLayer("UI");

//        var rt = go.AddComponent<RectTransform>();
//        rt.SetParent(parent, false);

//        // Default stretch-to-fill anchoring
//        rt.anchorMin = Vector2.zero;
//        rt.anchorMax = Vector2.one;
//        rt.offsetMin = Vector2.zero;
//        rt.offsetMax = Vector2.zero;

//        switch (kind)
//        {
//            case ChildKind.Container:
//                // Scrollable list container — VerticalLayoutGroup for HubItemRowFactory rows
//                var vlg = go.AddComponent<VerticalLayoutGroup>();
//                vlg.childAlignment = TextAnchor.UpperLeft;
//                vlg.childControlWidth = true;
//                vlg.childControlHeight = false;
//                vlg.childForceExpandWidth = true;
//                vlg.childForceExpandHeight = false;
//                vlg.spacing = 4f;

//                // ContentSizeFitter so the container grows with content
//                var csf = go.AddComponent<ContentSizeFitter>();
//                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
//                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
//                break;

//            case ChildKind.Label:
//                go.AddComponent<CanvasRenderer>();
//                var tmp = go.AddComponent<TextMeshProUGUI>();
//                tmp.text = "";
//                tmp.fontSize = 24;
//                tmp.color = Color.white;
//                tmp.alignment = TextAlignmentOptions.TopLeft;
//                tmp.enableWordWrapping = true;
//                tmp.raycastTarget = false;
//                break;

//            case ChildKind.Button:
//                go.AddComponent<CanvasRenderer>();
//                var img = go.AddComponent<Image>();
//                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
//                img.raycastTarget = true;
//                var btn = go.AddComponent<Button>();
//                btn.targetGraphic = img;

//                // Add a label child
//                var labelGO = new GameObject("Label");
//                labelGO.layer = LayerMask.NameToLayer("UI");
//                var labelRT = labelGO.AddComponent<RectTransform>();
//                labelRT.SetParent(rt, false);
//                labelRT.anchorMin = Vector2.zero;
//                labelRT.anchorMax = Vector2.one;
//                labelRT.offsetMin = Vector2.zero;
//                labelRT.offsetMax = Vector2.zero;
//                labelGO.AddComponent<CanvasRenderer>();
//                var btnTmp = labelGO.AddComponent<TextMeshProUGUI>();
//                btnTmp.text = name;
//                btnTmp.fontSize = 20;
//                btnTmp.color = Color.white;
//                btnTmp.alignment = TextAlignmentOptions.Center;
//                btnTmp.raycastTarget = false;

//                // Compact size for tab buttons
//                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//                rt.sizeDelta = new Vector2(140f, 48f);
//                break;

//            case ChildKind.Image:
//                go.AddComponent<CanvasRenderer>();
//                var portrait = go.AddComponent<Image>();
//                portrait.color = Color.white;
//                portrait.raycastTarget = false;
//                portrait.preserveAspect = true;

//                // Square portrait size
//                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
//                rt.pivot = new Vector2(0f, 1f);
//                rt.sizeDelta = new Vector2(96f, 96f);
//                rt.anchoredPosition = new Vector2(8f, -8f);
//                break;
//        }

//        return go;
//    }
//}
