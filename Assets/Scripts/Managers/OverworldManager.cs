using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using scene = Scripts.Helpers.SceneHelper;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// OVERWORLDMANAGER - Overworld scene controller.
/// 
/// PURPOSE:
/// Orchestrates the world map exploration scene including
/// input handling, camera control, and random encounters.
/// 
/// RESPONSIBILITIES:
/// - Initialize world layers (terrain, surface, canopy)
/// - Handle camera mode switching
/// - Process tap vs drag input
/// - Trigger random encounters
/// - Manage offscreen arrow indicator
/// 
/// CAMERA MODES:
/// - FollowHero: Camera tracks hero
/// - FreePan: Player can pan camera
/// 
/// RANDOM ENCOUNTERS:
/// Accumulates time while moving and triggers
/// battle transition when threshold reached.
/// 
/// RELATED FILES:
/// - OverworldHero.cs: Hero controller
/// - Mode7CameraController.cs: Camera control
/// - OffscreenArrowIndicator.cs: UI indicator
/// </summary>
public class OverworldManager : MonoBehaviour
{
    // World layers
    private SpriteRenderer terrainSR;
    private SpriteRenderer surfaceSR;
    private SpriteRenderer canopySR;

    private OverworldHero hero; // current leader

    // Camera mode UI
    private Button cameraModeButton;
    private Image cameraModeImage;
    private TextMeshProUGUI cameraModeLabel;

    // Offscreen arrow (now handled by its own component)
    private OffscreenArrowIndicator offscreenArrow;

    [SerializeField] private bool hasRandomEncounters = false;

    // Random encounter
    private float encounterTimer;                       // accumulates only while moving
    private const float encounterIntervalSeconds = 3f;  // trigger threshold
    private bool movedThisFrame;                        // set by HandleHeroMoved each frame
    private bool isLoadingEncounter;                    // prevent double loads

    // Tap vs Drag detection
    private bool pointerDownAllowed; // true if not over UI (except joystick)
    private Vector2 pointerDownPos;
    private float pointerDownTime;
    private const float tapMaxTime = 0.30f;
    private const float tapMaxSqrDistance = 12f * 12f;

    // Camera mode and panning
    private OverworldCameraMode cameraMode = OverworldCameraMode.FollowHero;
    private bool isPanning;
    private Vector2 panStartScreen;
    private Vector3 panStartCameraTarget;
    private Vector3 cameraTarget;
    private float panLerpSpeed = 10f; // higher = snappier

    private Camera cam;
    private Transform mapRoot; // parent for map components

    // Mode7 controller (optional)
    private Mode7CameraController mode7;
    private bool mode7WasEnabled; // restore when leaving FreeCamera
    private bool Mode7Active { get { return mode7 != null && mode7.enabled && mode7.enableMode7; } }

    ScreenShatter screenShatter;
    ZoomEffect zoomEffect;


    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        cam = Camera.main;
        if (cam != null) mode7 = cam.GetComponent<Mode7CameraController>();

        if (!ProfileHelper.HasProfiles())
            return;

        if (!ProfileHelper.HasCurrentProfile)
        {
            Debug.LogError("No current profile selected.");
            scene.Fade.ToProfileCreate();
            return;
        }

        if (!ProfileHelper.HasCurrentSave)
        {
            Debug.LogError("No current save selected.");
            scene.Fade.ToSaveFileSelect();
            return;
        }


        // Camera mode button + icon (optional wiring from scene)
        cameraModeButton = GameObject.Find(GameObjectHelper.Overworld.Canvas.CameraModeButton)?.GetComponent<Button>();
        cameraModeImage = GameObject.Find(GameObjectHelper.Overworld.Canvas.CameraModeImage)?.GetComponent<Image>();
        cameraModeLabel = GameObject.Find(GameObjectHelper.Overworld.Canvas.CameraModeLabel)?.GetComponent<TextMeshProUGUI>();



        screenShatter = GameObject.Find(GameObjectHelper.Overworld.BattleTransition)?.GetComponent<ScreenShatter>();
        zoomEffect = GameObject.Find(GameObjectHelper.Overworld.BattleTransition)?.GetComponent<ZoomEffect>();

        // Find Map root
        mapRoot = GameObject.Find("Map").transform;

        // Load map data from profile
        var overworld = ProfileHelper.CurrentProfile.CurrentSave.Overworld;
        var data = MapLibrary.Get(overworld.MapName);

        // Ensure world-space layers exist as SpriteRenderers (preserve scene scale)
        terrainSR = GameObject.Find(GameObjectHelper.Overworld.Map.Terrain).GetComponent<SpriteRenderer>();
        surfaceSR = GameObject.Find(GameObjectHelper.Overworld.Map.Surface).GetComponent<SpriteRenderer>();
        canopySR = GameObject.Find(GameObjectHelper.Overworld.Map.Canopy).GetComponent<SpriteRenderer>();

        // Resolve a default hero reference (by name/path), but leader will be chosen from inspector or fallback
        var defaultHero = GameObject.Find(GameObjectHelper.Overworld.Map.Hero).GetComponent<OverworldHero>();
        defaultHero.transform.position = new Vector3(overworld.HeroX, overworld.HeroY, defaultHero.transform.position.z);
        defaultHero.SetFacing(overworld.HeroDirection);

        // Gather all heroes in a stable order and bind world
        var allHeroes = GetOrderedHeroes();
        foreach (var h in allHeroes)
        {
            if (h == null) continue;
            h.BindWorld(terrainSR, cam);
        }

        // Choose leader: prefer one marked IsLeader in inspector; otherwise fallback to defaultHero
        var initialLeader = allHeroes.FirstOrDefault(h => h != null && h.IsLeader) ?? defaultHero;

        // Assign chain: leader follows cursor, next follows previous, wrapping around
        AssignPartyChain(initialLeader, allHeroes);

        // Track current leader and subscribe for movement
        hero = initialLeader;
        if (hero != null) hero.OnHeroMoved += HandleHeroMoved;

        // Apply initial Y-sort to all heroes
        foreach (var h in allHeroes)
        {
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr != null) PartySortHelper.ApplyActorYSort(sr, PartySortHelper.GlobalScale);
        }

        // Wire offscreen indicator target now that we have hero
        offscreenArrow = GameObject.Find(GameObjectHelper.Overworld.Canvas.OffscreenArrow).GetComponent<OffscreenArrowIndicator>();
        offscreenArrow.WorldCamera = Camera.main;

        // Initialize UI state
        UpdateCameraModeUI();

        scene.FadeIn();
    }

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
        if (hero != null) hero.OnHeroMoved -= HandleHeroMoved;
        if (cameraModeButton != null) cameraModeButton.onClick.RemoveListener(CycleCameraMode);
    }

    // Called by UI button to cycle the active leader to the next party member
    /// <summary>Change leader.</summary>
    public void ChangeLeader()
    {
        var all = GetOrderedHeroes();
        if (all.Count == 0) return;

        int currentIndex = Mathf.Max(0, all.IndexOf(hero));
        int nextIndex = (currentIndex + 1) % all.Count;
        var nextLeader = all[nextIndex];
        if (nextLeader == null || nextLeader == hero) return;

        // Rebuild the party chain around the new leader
        AssignPartyChain(nextLeader, all);
        hero = nextLeader;

        // Update camera target immediately
        cameraTarget = hero.transform.position;

        // Ensure Y-sort layer is consistent
        foreach (var h in all)
        {
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr != null) PartySortHelper.ApplyActorYSort(sr, PartySortHelper.GlobalScale);
        }
    }

    /// <summary>Cycle camera mode.</summary>
    public void CycleCameraMode()
    {
        cameraMode = cameraMode == OverworldCameraMode.FollowHero ? OverworldCameraMode.FreeCamera : OverworldCameraMode.FollowHero;
        if (cameraMode == OverworldCameraMode.FollowHero && hero != null)
        {
            // Restore Mode7 state if present
            if (mode7 != null) mode7.enableMode7 = mode7WasEnabled;

            cameraTarget = hero.transform.position;
            isPanning = false;
        }
        else if (cameraMode == OverworldCameraMode.FreeCamera)
        {
            // Disable Mode7 so manual camera works
            if (mode7 != null)
            {
                mode7WasEnabled = mode7.enableMode7;
                mode7.enableMode7 = false;
            }

            // Stop hero
            if (hero != null)
            {
                hero.FullStop();
            }

            // Start free camera target from current camera position
            cameraTarget = cam != null ? cam.transform.position : cameraTarget;
        }
        UpdateCameraModeUI();
    }

    /// <summary>Updates the camera mode ui.</summary>
    private void UpdateCameraModeUI()
    {
        if (cameraModeImage == null || cameraModeLabel == null || cameraModeLabel == null)
            return;

        // Map camera mode to sprite+label (reusing existing sprites)
        var mapping = cameraMode switch
        {
            OverworldCameraMode.FollowHero => ("Camera00", "Follow"),
            OverworldCameraMode.FreeCamera => ("Camera01", "Free"),
            _ => ("Camera00", "Follow"),
        };
        cameraModeImage.sprite = SpriteLibrary.GUI[mapping.Item1];
        cameraModeLabel.text = mapping.Item2;

    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (terrainSR == null) return;

        // Touch (hold-to-move in directional mode) or pan camera in FreeCamera
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            bool overUiNow = IsOverUI(t.position) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId));

            if (t.phase == TouchPhase.Began)
            {
                pointerDownAllowed = !overUiNow;
                pointerDownPos = t.position;
                pointerDownTime = Time.unscaledTime;

                if (pointerDownAllowed && cameraMode == OverworldCameraMode.FreeCamera)
                {
                    isPanning = true;
                    panStartScreen = t.position;
                    panStartCameraTarget = cameraTarget;
                }

                if (pointerDownAllowed && cameraMode != OverworldCameraMode.FreeCamera && hero != null)
                    hero.BeginDirectionalFromScreen(t.position, null);
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (isPanning && cameraMode == OverworldCameraMode.FreeCamera)
                {
                    UpdatePanTarget(t.position);
                }
                else if (pointerDownAllowed && cameraMode != OverworldCameraMode.FreeCamera && hero != null)
                    hero.UpdateDirectionalFromScreen(t.position, null);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (isPanning)
                {
                    isPanning = false;
                }
                else if (cameraMode != OverworldCameraMode.FreeCamera && hero != null)
                {
                    hero.FullStop();
                }

            }
            return;
        }
    }

    /// <summary>Runs per-frame logic after all Update calls.</summary>
    private void LateUpdate()
    {
        // Only move camera when Mode7 is not actively driving it
        if (!Mode7Active)
        {
            if (cameraMode == OverworldCameraMode.FollowHero && hero != null)
            {
                cameraTarget = hero.transform.position;
            }

            // Clamp camera target to map bounds
            cameraTarget = ClampCameraTarget(cameraTarget);

            // Smoothly move camera
            var cur = cam.transform.position;
            var target = new Vector3(cameraTarget.x, cameraTarget.y, cur.z);
            cam.transform.position = Vector3.Lerp(cur, target, Mathf.Clamp01(Time.deltaTime * panLerpSpeed));
        }

        // Random encounter timer
        if (movedThisFrame)
        {
            encounterTimer += Time.deltaTime;
            if (encounterTimer >= encounterIntervalSeconds)
            {
                encounterTimer = 0f;
                TriggerRandomEncounter();
            }
        }
        else
        {
            encounterTimer = 0f;
        }
        movedThisFrame = false;

        // Offscreen arrow indicator wiring/toggle (fade-based)
        if (offscreenArrow != null)
        {
            offscreenArrow.WorldCamera = cam;
            offscreenArrow.Target = (cameraMode == OverworldCameraMode.FreeCamera && hero != null && !Mode7Active) ? hero.transform : null; // null when not in FreeCamera -> fade out
        }
    }


    /// <summary>Returns whether the is over ui condition is met.</summary>
    private bool IsOverUI(Vector2 screenPos)
    {
        // Treat any UI under the pointer as blocking, except the virtual joystick hierarchy.
        if (EventSystem.current == null) return false;
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        if (results == null || results.Count == 0) return false;

        foreach (var r in results)
        {
            return true;
        }
        return false;
    }



    // Follow hero while moving and flag movement for encounter timer
    /// <summary>Handle hero moved.</summary>
    private void HandleHeroMoved(Vector2 heroPos)
    {
        movedThisFrame = true;
    }

    // Trigger scene change to a random stage after sustained movement
    /// <summary>Trigger random encounter.</summary>
    private void TriggerRandomEncounter()
    {
        if (!hasRandomEncounters || isLoadingEncounter) return;
        if (StageLibrary.Stages == null || StageLibrary.Stages.Count == 0) return;

        string mapName = ProfileHelper.Overworld.MapName;

        // Persist overworld location and facing
        if (hero != null)
        {
            ProfileHelper.CurrentProfile.LatestSave.Overworld.MapName = mapName;
            ProfileHelper.CurrentProfile.LatestSave.Overworld.HeroX = hero.transform.position.x;
            ProfileHelper.CurrentProfile.LatestSave.Overworld.HeroY = hero.transform.position.y;
            ProfileHelper.CurrentProfile.LatestSave.Overworld.HeroDirection = hero.CurrentFacingName ?? "Idle";
            ProfileHelper.SaveOverworldPosition(new Vector2(hero.transform.position.x, hero.transform.position.y), mapName, hero.CurrentFacingName ?? "Idle");
            ProfileHelper.CurrentProfile.LatestSave.Stage.CurrentStage = RNG.Stage(mapName);
        }

        isLoadingEncounter = true;

        StartCoroutine(zoomEffect.Play(() =>
        {
            // Effect finished: allow encounters again
            isLoadingEncounter = false;

            // If you want to transition after the effect, uncomment:
             scene.Switch.ToGame();
        }));

    }

    // --- Party helpers ---
    /// <summary>Gets the ordered heroes.</summary>
    private List<OverworldHero> GetOrderedHeroes()
    {
        return GameObject.FindObjectsOfType<OverworldHero>(true)
            .Where(h => h != null)
            .OrderBy(h => h.transform.GetSiblingIndex()) // stable order as shown in hierarchy
            .ToList();
    }

    /// <summary>Assign party chain.</summary>
    private void AssignPartyChain(OverworldHero leaderHero, List<OverworldHero> all)
    {
        if (leaderHero == null || all == null || all.Count == 0) return;

        int n = all.Count;
        int li = Mathf.Max(0, all.IndexOf(leaderHero));

        // Set leader state
        for (int i = 0; i < n; i++)
        {
            var h = all[i];
            if (h == null) continue;
            bool isLeader = (i == li);
            h.SetAsLeader(isLeader);
        }

        // Assign chain followers: each non-leader follows the previous hero, wrapping around
        for (int offset = 1; offset < n; offset++)
        {
            int idx = (li + offset) % n;              // current follower
            int prevIdx = (li + offset - 1 + n) % n;  // their leader is previous in ring
            var follower = all[idx];
            var prev = all[prevIdx];
            if (follower != null)
            {
                follower.SetLeader(prev);
            }
        }
    }

    // --- Camera helpers ---
    /// <summary>Updates the pan target.</summary>
    private void UpdatePanTarget(Vector2 currentScreen)
    {
        if (cam == null) return;
        // Convert screen delta to world delta at the map plane Z so it works in both orthographic and perspective cameras
        float planeZ = terrainSR != null ? terrainSR.transform.position.z : 0f;
        Vector3 a = Mode7CameraController.ScreenToWorldOnZPlane(cam, panStartScreen, planeZ);
        Vector3 b = Mode7CameraController.ScreenToWorldOnZPlane(cam, currentScreen, planeZ);
        Vector3 worldDelta = b - a;
        // Move camera opposite to finger drag
        cameraTarget = panStartCameraTarget - new Vector3(worldDelta.x, worldDelta.y, 0f);
    }

    /// <summary>Clamp camera target.</summary>
    private Vector3 ClampCameraTarget(Vector3 target)
    {
        if (terrainSR == null || cam == null) return target;
        Bounds b = terrainSR.bounds;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;
        // If map smaller than view, just center clamp
        if (minX > maxX)
        {
            float cx = (b.min.x + b.max.x) * 0.5f;
            minX = maxX = cx;
        }
        if (minY > maxY)
        {
            float cy = (b.min.y + b.max.y) * 0.5f;
            minY = maxY = cy;
        }
        float x = Mathf.Clamp(target.x, minX, maxX);
        float y = Mathf.Clamp(target.y, minY, maxY);
        return new Vector3(x, y, target.z);
    }
}

}
