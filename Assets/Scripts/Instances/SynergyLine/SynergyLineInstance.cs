// --- File: Assets/Scripts/Instances/SynergyLine/SynergyLineInstance.cs ---
// Wispy multi-strand line between two actors.
// Each instance keeps its own tunables. Per-strand behavior lives in SynergyLineStrand.

using Assets.Scripts.Factories;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using g = Assets.Helpers.GameHelper;

public class SynergyLineInstance : MonoBehaviour
{
    // Group
    [SerializeField] private int waveformCount = 4;
    [SerializeField] private float baseRadius = 0.07f;
    [SerializeField] private float baseWidth = 0.012f;

    // Fade
    [SerializeField] private float fadeInTime = 0.20f;
    [SerializeField] private float fadeOutTime = 0.20f; // Shorter than BoardOverlay (0.25) so this finishes first

    // Sorting bias for multi-wave ordering
    [SerializeField] private int orderOffsetPerWave = 1;
    [SerializeField] private int extraFrontBias = -2;

    // Geometry resolution for each strand line
    [SerializeField] private int strandSegmentCount = 32;

    // Runtime
    private readonly List<SynergyLineStrand> strands = new List<SynergyLineStrand>(8);
    public ActorInstance supporter;
    public ActorInstance attacker;
    private Renderer aRenderer;
    private Renderer bRenderer;
    private SortingGroup aGroup;
    private SortingGroup bGroup;

    // Anchors that follow tiles, not actor transforms
    private Transform aAnchor;
    private Transform bAnchor;

    private bool playing;
    private bool despawnRequested;
    private Coroutine runningCoroutine;

    // Cached per-strand weights for reconfigure
    private float[] wNormPerStrand;

    /// <summary>
    /// Create strand anchors and cache the strand prefab.
    /// </summary>
    private void Awake()
    {
        var aGo = new GameObject("SynergyAnchor_A");
        var bGo = new GameObject("SynergyAnchor_B");
        aGo.transform.SetParent(transform, false);
        bGo.transform.SetParent(transform, false);
        aAnchor = aGo.transform;
        bAnchor = bGo.transform;
    }

    /// <summary>
    /// Entry point for creating the visual link between two actors.
    /// </summary>
    public void Spawn(ActorInstance supporter, ActorInstance attacker)
    {
        this.supporter = supporter;
        this.attacker = attacker;

        if (this.supporter == null || this.attacker == null)
        {
            Debug.LogError("SynergyLineInstance.Spawn received null supporter or attacker.");
            return;
        }

        // Cache renderers and sorting groups to derive where to place the line
        aGroup = this.supporter.GetComponent<SortingGroup>();
        bGroup = this.attacker.GetComponent<SortingGroup>();
        aRenderer = this.supporter.GetComponentInChildren<Renderer>();
        bRenderer = this.attacker.GetComponentInChildren<Renderer>();

        // Build weights from both actors. Order: Strength, Vitality, Agility, Stamina, Intelligence, Wisdom, Luck.
        VectorStats weights = new VectorStats(
            this.supporter.Stats.Strength + this.attacker.Stats.Strength,
            this.supporter.Stats.Vitality + this.attacker.Stats.Vitality,
            this.supporter.Stats.Agility + this.attacker.Stats.Agility,
            this.supporter.Stats.Speed + this.attacker.Stats.Speed,
            this.supporter.Stats.Stamina + this.attacker.Stats.Stamina,
            this.supporter.Stats.Intelligence + this.attacker.Stats.Intelligence,
            this.supporter.Stats.Wisdom + this.attacker.Stats.Wisdom,
            this.supporter.Stats.Luck + this.attacker.Stats.Luck
        );

        Configure(weights);
        StartLoop();
    }

    /// <summary>
    /// Requests a graceful fade out and destruction.
    /// </summary>
    public void Despawn(float fadeSeconds = -1f)
    {
        despawnRequested = true;
    }

    /// <summary>
    /// Gathers rendering components from endpoints, prepares strands, normalizes weights, and applies settings.
    /// </summary>
    public void Configure(VectorStats weights)
    {
        if (supporter == null || attacker == null)
        {
            Debug.LogError("SynergyLineInstance.Configure missing supporter or attacker. Did you call Spawn first?");
            return;
        }

        // Normalize 7 weights to [0..1] and mirror to waveformCount slots
        float[] w = new float[7]
        {
            Mathf.Max(0.0001f, weights.Strength),
            Mathf.Max(0.0001f, weights.Vitality),
            Mathf.Max(0.0001f, weights.Agility),
            Mathf.Max(0.0001f, weights.Stamina),
            Mathf.Max(0.0001f, weights.Intelligence),
            Mathf.Max(0.0001f, weights.Wisdom),
            Mathf.Max(0.0001f, weights.Luck),
        };

        float max = 0f;
        for (int i = 0; i < 7; i++) if (w[i] > max) max = w[i];
        if (max <= 0f) max = 1f;

        if (wNormPerStrand == null || wNormPerStrand.Length != waveformCount)
            wNormPerStrand = new float[waveformCount];

        for (int i = 0; i < waveformCount; i++)
            wNormPerStrand[i] = w[i % 7] / max;

        ApplySettingsToStrands();
    }

    /// <summary>
    /// Starts the coroutine loop that drives fade, animation, and lifetime.
    /// </summary>
    private void StartLoop()
    {
        if (playing) return;
        playing = true;
        despawnRequested = false;
        runningCoroutine = StartCoroutine(LoopRoutine());
    }

    /// <summary>
    /// Handles fade in, steady update, fade out, and cleanup.
    /// </summary>
    private IEnumerator LoopRoutine()
    {
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeInTime);
            SetFadeAll(k);
            TickAll();
            yield return null;
        }

        while (!despawnRequested)
        {
            TickAll();
            yield return null;
        }

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / fadeOutTime);
            SetFadeAll(k);
            TickAll();
            yield return null;
        }

        ClearAll();
        playing = false;
        Destroy(gameObject);
    }

    /// <summary>
    /// Applies configuration to each strand, including sorting and visual parameters.
    /// </summary>
    private void ApplySettingsToStrands()
    {
        if (supporter == null || attacker == null)
        {
            Debug.LogError("SynergyLineInstance.ApplySettingsToStrands missing supporter or attacker.");
            return;
        }

        // Set anchors to current tiles before configuring, so positions are correct from frame 0
        UpdateAnchorsToTiles();

        float phaseStep = (Mathf.PI * 2f) / Mathf.Max(1, waveformCount);

        string layerName;
        int baseOrder;
        ResolveSortingForSynergyLayer(out layerName, out baseOrder);

        EnsureStrands(waveformCount);

        for (int i = 0; i < waveformCount; i++)
        {
            float wNorm = wNormPerStrand != null && i < wNormPerStrand.Length ? wNormPerStrand[i] : 0.5f;

            float widthForStrand = Mathf.Lerp(0.75f, 1.25f, wNorm) * baseWidth;
            float radiusForStrand = Mathf.Lerp(0.75f, 1.25f, wNorm) * baseRadius;
            float phase = phaseStep * i;

            var strand = strands[i];
            // Important: pass anchors, not actor transforms
            strand.Configure(
                aAnchor,
                bAnchor,
                widthForStrand,
                radiusForStrand,
                phase,
                layerName,
                baseOrder + extraFrontBias + (i * orderOffsetPerWave),
                i,
                wNorm,
                strandSegmentCount
            );

            strand.SetFade(0f);
            strand.Tick();
        }

        for (int i = waveformCount; i < strands.Count; i++)
            strands[i].gameObject.SetActive(false);
    }

    /// <summary>
    /// Update both anchors to the world positions of each actor's currentTile.
    /// Never touches the actors' own transforms.
    /// </summary>
    private void UpdateAnchorsToTiles()
    {
        if (supporter != null && supporter.currentTile != null)
        {
            Vector3 pa = supporter.currentTile.position;
            pa.z = 0f;
            aAnchor.position = pa;
        }

        if (attacker != null && attacker.currentTile != null)
        {
            Vector3 pb = attacker.currentTile.position;
            pb.z = 0f;
            bAnchor.position = pb;
        }
    }

    /// <summary>
    /// Fade helper across all strands.
    /// </summary>
    private void SetFadeAll(float k)
    {
        int n = Mathf.Min(waveformCount, strands.Count);
        for (int i = 0; i < n; i++) strands[i].SetFade(k);
    }

    /// <summary>
    /// Per frame update:
    /// 1) Move anchors to current tiles, so bumps and moves snap endpoints to tiles.
    /// 2) Reaffirm sorting to remain below the lowest of the two actors.
    /// 3) Tick every active strand.
    /// </summary>
    private void TickAll()
    {
        // Keep endpoint anchors pinned to tiles
        UpdateAnchorsToTiles();

        // Ensure the layer remains correct while playing
        string layerName;
        int baseOrder;
        ResolveSortingForSynergyLayer(out layerName, out baseOrder);

        int n = Mathf.Min(waveformCount, strands.Count);
        for (int i = 0; i < n; i++)
        {
            strands[i].SetSortingLayer(layerName);
            strands[i].Tick();
        }
    }

    /// <summary>
    /// Clear all strand geometry and visuals.
    /// </summary>
    private void ClearAll()
    {
        int n = Mathf.Min(waveformCount, strands.Count);
        for (int i = 0; i < n; i++) strands[i].Clear();
    }

    /// <summary>
    /// Ensure we have enough strand instances.
    /// </summary>
    private void EnsureStrands(int count)
    {
        while (strands.Count < count)
        {
            // Use factory instead of Instantiate(prefab)
            var instGO = SynergyStrandFactory.Create(transform);
            instGO.name = "Waveform_" + strands.Count;
            instGO.SetActive(true);

            var seg = instGO.GetComponent<SynergyLineStrand>();
            if (seg == null) seg = instGO.AddComponent<SynergyLineStrand>();

            strands.Add(seg);
        }
    }

    /// <summary>
    /// Resolve a sorting layer so the synergy line always renders below the lower of the two actors.
    /// If either actor is on ActorBelow, use SupportLineBelow; otherwise SupportLineAbove.
    /// </summary>
    private void ResolveSortingForSynergyLayer(out string layerName, out int order)
    {
        string aLayer = null;
        string bLayer = null;

        if (supporter != null && supporter.SortingGroup != null)
            aLayer = supporter.SortingGroup.sortingLayerName;
        else if (aGroup != null)
            aLayer = aGroup.sortingLayerName;

        if (attacker != null && attacker.SortingGroup != null)
            bLayer = attacker.SortingGroup.sortingLayerName;
        else if (bGroup != null)
            bLayer = bGroup.sortingLayerName;

        bool anyBelow = string.Equals(aLayer, Assets.Helpers.SortingHelper.Layer.ActorBelow)
                        || string.Equals(bLayer, Assets.Helpers.SortingHelper.Layer.ActorBelow);

        // If any endpoint is below, keep the line below; otherwise keep it just under ActorAbove
        layerName = anyBelow
            ? Assets.Helpers.SortingHelper.Layer.SupportLineBelow
            : Assets.Helpers.SortingHelper.Layer.SupportLineAbove;

        // Keep a small base order so per-wave offsets work but we remain inside this layer
        order = 0;
    }

    /// <summary>
    /// Update the sorting layer for all active strands without changing their relative order.
    /// </summary>
    public void SetSorting(string sortingLayer)
    {
        int n = Mathf.Min(waveformCount, strands.Count);
        for (int i = 0; i < n; i++)
        {
            strands[i].SetSortingLayer(sortingLayer);
        }
    }
}
