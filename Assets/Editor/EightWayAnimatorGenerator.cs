// File: Assets/Editor/EightWayAnimatorGenerator_CustomOrder_SpriteRenderer.cs
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
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
/// Builds 16 clips and a two-blend-tree Animator for an 8-way character using SpriteRenderer.
/// Format enforced:
///   Idle indices: D, DR, R, UR, U, UL, L, DL  (sprite index order 0..7)
///   Move rows:    D, DR, R, UR, U, UL, L, DL  (each row has 4 frames)
/// Assumptions:
///   Idle sheet is 8 sprites total (4x2).
///   Move sheet is 32 sprites total (4x8).
/// Clips target SpriteRenderer.m_Sprite so this works for world-space characters.
/// Blend trees are 2D Freeform Directional using parameters MoveX, MoveY, Speed.
/// </summary>
public sealed class EightWayAnimatorGenerator_CustomOrder_SpriteRenderer : EditorWindow
{
    // Input sheets
    private Texture2D idleSheet;
    private Texture2D moveSheet;

    // Output
    private string outputFolder = "Assets/Animations/Adam";
    private string controllerName = "Adam.controller";

    // Sampling
    private int moveSamples = 12;
    private int idleSamples = 1;

    // Standard 2D directional slots used by Freeform Directional blend tree
    private static readonly string[] StandardDirNames = { "U", "UR", "R", "DR", "D", "DL", "L", "UL" };
    private static readonly Vector2[] StandardVectors = new[]
    {
        new Vector2(0f, 1f),           // U
        new Vector2(0.707f, 0.707f),   // UR
        new Vector2(1f, 0f),           // R
        new Vector2(0.707f, -0.707f),  // DR
        new Vector2(0f, -1f),          // D
        new Vector2(-0.707f, -0.707f), // DL
        new Vector2(-1f, 0f),          // L
        new Vector2(-0.707f, 0.707f)   // UL
    };

    // Your enforced custom order
    private static readonly string[] IdleOrder = { "D", "DR", "R", "UR", "U", "UL", "L", "DL" };
    private static readonly string[] MoveRowOrder = { "D", "DR", "R", "UR", "U", "UL", "L", "DL" };

    // Clip file names
    private static readonly string[] IdleClipNames =
    {
        "Idle_D","Idle_DR","Idle_R","Idle_UR","Idle_U","Idle_UL","Idle_L","Idle_DL"
    };
    private static readonly string[] MoveClipNames =
    {
        "Move_D","Move_DR","Move_R","Move_UR","Move_U","Move_UL","Move_L","Move_DL"
    };

    [MenuItem("Tools/Generate 8-Way Animator (SpriteRenderer)")]
    /// <summary>Open.</summary>
    private static void Open()
    {
        var win = CreateInstance<EightWayAnimatorGenerator_CustomOrder_SpriteRenderer>();
        win.titleContent = new GUIContent("8-Way Animator (SpriteRenderer)");
        win.minSize = new Vector2(480, 320);
        win.ShowUtility();
    }

    // Window UI
    /// <summary>Draws immediate-mode GUI elements.</summary>
    private void OnGUI()
    {
        GUILayout.Label("Input Spritesheets", EditorStyles.boldLabel);
        idleSheet = (Texture2D)EditorGUILayout.ObjectField("Idle Sheet (8 slices)", idleSheet, typeof(Texture2D), false);
        moveSheet = (Texture2D)EditorGUILayout.ObjectField("Move Sheet (32 slices, 4x8)", moveSheet, typeof(Texture2D), false);

        GUILayout.Space(8);
        GUILayout.Label("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        controllerName = EditorGUILayout.TextField("Animator Name", controllerName);

        GUILayout.Space(8);
        GUILayout.Label("Clip Settings", EditorStyles.boldLabel);
        moveSamples = EditorGUILayout.IntSlider("Move Samples (fps)", moveSamples, 1, 24);
        idleSamples = EditorGUILayout.IntSlider("Idle Samples (fps)", idleSamples, 1, 12);

        GUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "Ordering:\n" +
            "Idle sprites index 0..7: D, DR, R, UR, U, UL, L, DL\n" +
            "Move rows top..bottom:   D, DR, R, UR, U, UL, L, DL\n" +
            "Clips target SpriteRenderer.m_Sprite. No placeholders.",
            MessageType.Info
        );

        if (GUILayout.Button("Generate Animator and Clips", GUILayout.Height(40)))
        {
            GenerateAll();
        }
    }

    // Validate input, create SpriteRenderer clips, and build the Animator
    /// <summary>Generate all.</summary>
    private void GenerateAll()
    {
        try
        {
            ValidateInput();

            var idleSprites = LoadSlicedSprites(idleSheet);
            var moveSprites = LoadSlicedSprites(moveSheet);

            if (idleSprites.Length != 8)
                throw new Exception("Idle sheet must have exactly 8 sliced sprites.");
            if (moveSprites.Length != 32)
                throw new Exception("Move sheet must have exactly 32 sliced sprites arranged as 4 columns by 8 rows.");

            EnsureFolder(outputFolder);

            // Idle clips: one sprite each, SpriteRenderer binding
            var idleClips = new AnimationClip[8];
            for (int i = 0; i < 8; i++)
            {
                var clip = CreateSpriteClipSingle_SpriteRenderer(idleSprites[i], idleSamples);
                var path = Path.Combine(outputFolder, IdleClipNames[i] + ".anim");
                AssetDatabase.CreateAsset(clip, path);
                idleClips[i] = clip;
            }

            // Move clips: 4 frames per row, SpriteRenderer binding
            var moveClips = new AnimationClip[8];
            for (int row = 0; row < 8; row++)
            {
                int start = row * 4;
                var frames = new[]
                {
                    moveSprites[start + 0],
                    moveSprites[start + 1],
                    moveSprites[start + 2],
                    moveSprites[start + 3]
                };

                var clip = CreateSpriteClip_SpriteRenderer(frames, moveSamples);
                var path = Path.Combine(outputFolder, MoveClipNames[row] + ".anim");
                AssetDatabase.CreateAsset(clip, path);
                moveClips[row] = clip;
            }

            // Animator with real clips wired into the blend trees
            var controllerPath = Path.Combine(outputFolder, controllerName);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var sm = controller.layers[0].stateMachine;

            var idleTree = CreateDirectionalBlendTree("IdleBT", controller, idleClips, IdleOrder);
            var idleState = sm.AddState("IdleBT");
            idleState.motion = idleTree;

            var moveTree = CreateDirectionalBlendTree("MoveBT", controller, moveClips, MoveRowOrder);
            var moveState = sm.AddState("MoveBT");
            moveState.motion = moveTree;

            sm.defaultState = idleState;

            var toMove = idleState.AddTransition(moveState);
            toMove.hasExitTime = false;
            toMove.duration = 0f;
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var toIdle = moveState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1001f, "Speed");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "Animator and SpriteRenderer clips generated.", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
            Debug.LogError(ex);
        }
    }

    /// <summary>Validate input.</summary>
    private void ValidateInput()
    {
        if (idleSheet == null) throw new Exception("Assign Idle Sheet.");
        if (moveSheet == null) throw new Exception("Assign Move Sheet.");
        if (string.IsNullOrWhiteSpace(outputFolder)) throw new Exception("Output folder is empty.");
        if (!controllerName.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Animator Name must end with .controller");
    }

    // Load sliced sprites sorted by trailing numeric index
    /// <summary>Load sliced sprites.</summary>
    private static Sprite[] LoadSlicedSprites(Texture2D sheet)
    {
        var path = AssetDatabase.GetAssetPath(sheet);
        var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        Array.Sort(all, (a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
        return all;
    }

    /// <summary>Extract index.</summary>
    private static int ExtractIndex(string spriteName)
    {
        var m = Regex.Match(spriteName, @"_(\d+)$");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var idx)) return idx;
        m = Regex.Match(spriteName, @"(\d+)$");
        if (m.Success && int.TryParse(m.Groups[1].Value, out idx)) return idx;
        return 0;
    }

    // Single-frame looping clip for SpriteRenderer.m_Sprite
    /// <summary>Creates the sprite clip single_sprite renderer.</summary>
    private static AnimationClip CreateSpriteClipSingle_SpriteRenderer(Sprite sprite, int samples)
    {
        var clip = new AnimationClip { frameRate = Mathf.Max(1, samples) };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[1];
        keys[0] = new ObjectReferenceKeyframe { time = 0f, value = sprite };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        SetLoop(clip, true);
        return clip;
    }

    // Multi-frame looping clip for SpriteRenderer.m_Sprite
    /// <summary>Creates the sprite clip_sprite renderer.</summary>
    private static AnimationClip CreateSpriteClip_SpriteRenderer(Sprite[] sprites, int samples)
    {
        var clip = new AnimationClip { frameRate = Mathf.Max(1, samples) };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        float dt = 1f / clip.frameRate;
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = dt * i, value = sprites[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        SetLoop(clip, true);
        return clip;
    }

    // Loop settings
    /// <summary>Sets the loop.</summary>
    private static void SetLoop(AnimationClip clip, bool loop)
    {
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        s.keepOriginalPositionY = true;
        s.keepOriginalPositionXZ = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);
    }

    // Build a 2D Freeform Directional blend tree using provided clips, mapped to standard vectors
    /// <summary>Creates the directional blend tree.</summary>
    private static BlendTree CreateDirectionalBlendTree(
        string name,
        AnimatorController controller,
        AnimationClip[] motions,
        string[] tokenOrder)
    {
        var tree = new BlendTree
        {
            name = name,
            hideFlags = HideFlags.HideInHierarchy,
            blendType = BlendTreeType.FreeformDirectional2D,
            useAutomaticThresholds = false,
            blendParameter = "MoveX",
            blendParameterY = "MoveY"
        };

        string[] standardNames = StandardDirNames;    // U, UR, R, DR, D, DL, L, UL
        Vector2[] standardPos = StandardVectors;

        for (int i = 0; i < tokenOrder.Length; i++)
        {
            int std = Array.IndexOf(standardNames, tokenOrder[i]);
            if (std < 0) continue;
            tree.AddChild(motions[i], standardPos[std]);
        }

        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    // Ensure target folder exists
    /// <summary>Ensure folder.</summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        var tokens = folder.Trim('/').Split('/');
        string current = tokens[0];
        if (!AssetDatabase.IsValidFolder(current))
            throw new Exception("Root folder must exist: " + current);

        for (int i = 1; i < tokens.Length; i++)
        {
            var next = current + "/" + tokens[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, tokens[i]);
            }
            current = next;
        }
    }
}
#endif
