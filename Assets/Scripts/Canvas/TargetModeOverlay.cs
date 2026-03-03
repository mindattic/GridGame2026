using Scripts.Helpers;
using Scripts.Utilities;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Canvas
{
/// <summary>
/// TARGETMODEOVERLAY - Board darkening overlay for targeting modes.
/// 
/// PURPOSE:
/// Displays a semi-transparent overlay over the board when entering
/// ability targeting mode, providing visual focus on targets.
/// 
/// VISUAL EFFECT:
/// ```
/// Normal Mode:        Targeting Mode:
/// [Board visible]     [Board darkened]
///                          ↓
///                     [Targets highlighted]
/// ```
/// 
/// VISIBILITY CONTROL:
/// - Always active (uses alpha for visibility)
/// - Fades in when targeting mode starts
/// - Fades out when targeting ends
/// 
/// CONFIGURATION:
/// - minAlpha: Fully transparent (0)
/// - maxAlpha: Visible overlay alpha (0.33)
/// - duration: Fade time in seconds
/// - overlayColor: RGB color (default black)
/// 
/// PENDING MODE:
/// If mode change occurs while disabled, queues the change
/// and applies it when re-enabled.
/// 
/// RELATED FILES:
/// - AbilityManager.cs: Triggers targeting mode
/// - InputManager.cs: InputMode changes
/// - TileManager.cs: Tile highlighting
/// 
/// ACCESS: g.TargetModeOverlay
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TargetModeOverlay : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Components and state
    // ---------------------------------------------------------------------

    private SpriteRenderer spriteRenderer;   // Overlay sprite we fade
    private Coroutine runningCoroutine;      // Active fade routine if any

    // Fade parameters
    [SerializeField] private float minAlpha = 0f;          // Fully transparent
    [SerializeField] private float maxAlpha = 0.3333f;     // Visible overlay alpha
    [SerializeField] private float duration = 0.1f;        // Fade time (unscaled)

    // Overlay color (RGB); alpha is driven by fade
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 1f);

    // Rendering order (to ensure it draws above the board tiles)
    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 50;

    // If a mode arrives while this component is disabled, store and apply on enable
    private bool hasPendingMode;         // Tracks if we queued a mode
    private InputMode pendingMode;       // The queued mode value

    // ---------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------

    /// <summary>Caches the SpriteRenderer, sets initial transparent state, sizes to board, and signals readiness.</summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Deterministic starting visuals
        if (spriteRenderer != null)
        {
            // Apply sorting so we render on top of board tiles
            ApplySorting();

            var c = overlayColor; // use configured RGB, zero alpha
            c.a = 0f;
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }

        // Size and place over the board if possible
        TryFitToBoard();

        GameReady.Begin(this);
    }

    /// <summary>Applies any queued mode change that arrived while this component was disabled.</summary>
    private void OnEnable()
    {
        // If we were enabled after a pending mode, apply immediately
        if (hasPendingMode)
        {
            ApplyInstant(pendingMode);
            hasPendingMode = false;
        }
    }

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
        //if (g.InputManager != null)
        //    g.InputManager.OnInputModeChanged -= HandleModeChanged;
    }

    /// <summary>Subscribes to input mode changes, fits to board, applies sorting, and snaps to current state.</summary>
    public void Initialize()
    {
        // Subscribe to mode changes
        if (g.InputManager != null)
        {
            g.InputManager.OnInputModeChanged -= HandleModeChanged; // prevent double-subscribe
            g.InputManager.OnInputModeChanged += HandleModeChanged;
        }

        // Ensure we cover the board (sprite might have been assigned later)
        TryFitToBoard();

        // Apply desired sorting
        ApplySorting();

        // Snap to current state immediately
        if (hasPendingMode)
        {
            ApplyInstant(pendingMode);
            hasPendingMode = false;
        }
        else if (g.InputManager != null)
        {
            ApplyInstant(g.InputManager.InputMode);
        }
    }

#if UNITY_EDITOR
    /// <summary>Editor callback when inspector values change.</summary>
    private void OnValidate()
    {
        // Keep sorting and sizing up-to-date while editing
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySorting();
        TryFitToBoard();
    }
#endif

    // ---------------------------------------------------------------------
    // Event handling
    // ---------------------------------------------------------------------

    /// <summary>Responds to input mode changes by fading the overlay in or out.</summary>
    private void HandleModeChanged(InputMode mode)
    {
        if (!isActiveAndEnabled)
        {
            pendingMode = mode;
            hasPendingMode = true;
            return;
        }

        bool targetVisible = ShouldBeVisible(mode);

        StopFade();
        float from = GetAlpha();
        float to = targetVisible ? maxAlpha : minAlpha;
        runningCoroutine = StartCoroutine(FadeRoutine(from, to, duration));
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>Returns true if the overlay should be visible for the given input mode.</summary>
    private static bool ShouldBeVisible(InputMode mode)
    {
        return mode == InputMode.AnyTarget || mode == InputMode.LinearTarget;
    }

    /// <summary>Returns the current sprite renderer alpha.</summary>
    private float GetAlpha()
    {
        return spriteRenderer != null ? spriteRenderer.color.a : 0f;
    }

    /// <summary>Stops any active fade coroutine.</summary>
    private void StopFade()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
    }

    /// <summary>Immediately sets the overlay alpha based on the given input mode without fading.</summary>
    private void ApplyInstant(InputMode mode)
    {
        bool visible = ShouldBeVisible(mode);
        float targetAlpha = visible ? maxAlpha : minAlpha;

        if (spriteRenderer != null)
        {
            var c = overlayColor;
            c.a = targetAlpha;
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }
    }

    /// <summary>Applies the configured sorting layer and order to the sprite renderer.</summary>
    private void ApplySorting()
    {
        if (spriteRenderer == null) return;
        if (!string.IsNullOrEmpty(sortingLayerName))
            spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = orderInLayer;
    }

    /// <summary>
    /// Fit, position, and scale this sprite to exactly cover the board area.
    /// Uses the current sprite's bounds to compute the required transform scale.
    /// </summary>
    public void TryFitToBoard()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || g.Board == null)
            return;

        // Compute desired world-space width/height
        float width = g.Board.columnCount * g.TileSize;
        float height = g.Board.rowCount * g.TileSize;

        // Center the overlay over the board
        transform.position = new Vector3(g.Board.center.x, g.Board.center.y, transform.position.z);

        // Compute scale relative to sprite's local size
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size; // in world units at scale=1
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = width / spriteSize.x;
        scale.y = height / spriteSize.y;
        scale.z = 1f;
        transform.localScale = scale;
    }

    // ---------------------------------------------------------------------
    // Animation
    // ---------------------------------------------------------------------

    /// <summary>Lerps the overlay alpha from one value to another over the specified duration.</summary>
    private IEnumerator FadeRoutine(float from, float to, float seconds)
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.enabled = true;
        float elapsed = 0f;

        if (seconds <= 0f)
        {
            SetAlpha(to);
            runningCoroutine = null;
            yield break;
        }

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            float alpha = Mathf.Lerp(from, to, t);
            SetAlpha(alpha);
            yield return Wait.None();
        }

        SetAlpha(to);
        runningCoroutine = null;
    }

    /// <summary>Sets the sprite renderer alpha while preserving the configured overlay RGB color.</summary>
    private void SetAlpha(float a)
    {
        if (spriteRenderer == null) return;
        var c = overlayColor;
        c.a = a;
        spriteRenderer.color = c;
    }

#if UNITY_EDITOR
    [ContextMenu("Preview Show")]
    /// <summary>Editor preview show.</summary>
    private void EditorPreviewShow()
    {
        SetAlpha(maxAlpha);
    }

    [ContextMenu("Preview Hide")]
    /// <summary>Editor preview hide.</summary>
    private void EditorPreviewHide()
    {
        SetAlpha(minAlpha);
    }
#endif
}

}
