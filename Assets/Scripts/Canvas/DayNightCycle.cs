using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// DAYNIGHTCYCLE - Visual day/night cycle effect.
    /// 
    /// PURPOSE:
    /// Applies a 4-phase day/night color cycle to the screen,
    /// tinting visuals based on time of day.
    /// 
    /// PHASES:
    /// ```
    /// Night (12am-6am) → Morning (6am-12pm) → Day (12pm-6pm) → Evening (6pm-12am)
    ///   [Deep Blue]        [Warm Dawn]       [Bright White]    [Reddish Dusk]
    /// ```
    /// 
    /// APPLY MODES:
    /// - OverlayImage: Full-screen color overlay
    /// - PerSpriteMultiply: Multiply color onto sprites
    /// - Both: Combined effect
    /// 
    /// CONFIGURATION:
    /// - cycleSeconds: Full cycle duration
    /// - *Fraction: Duration of each phase
    /// - *Color: Base tint for each phase
    /// 
    /// RELATED FILES:
    /// - OverworldManager.cs: Overworld scene
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public enum DayPhase { Morning = 0, Day = 1, Evening = 2, Night = 3 }
        public enum ApplyMode { OverlayImage, PerSpriteMultiply, Both }

        [Header("Target")]
        [Tooltip("Full-screen Image to tint the scene in OverlayImage/Both modes.")]
        public Image overlayImage;
        [Tooltip("How to apply the effect.")]
        public ApplyMode applyMode = ApplyMode.PerSpriteMultiply;

        [Header("Cycle")]
        [Tooltip("Total seconds for a full cycle.")]
        [Range(1f, 300f)] public float cycleSeconds = 1024f;
        [Tooltip("Automatically start playing on enable.")]
        public bool playOnEnable = true;
        [Tooltip("Loop the cycle.")]
        public bool loop = true;
        [Tooltip("Use unscaled time.")]
        public bool useUnscaledTime = true;
        [Tooltip("Initial time offset into the cycle (seconds).")]
        [Range(0f, 300f)] public float startOffsetSeconds = 0f;

        [Header("Phase Fractions")]
        [Range(0f, 1f)] public float morningFraction = 0.25f;
        [Range(0f, 1f)] public float dayFraction = 0.25f;
        [Range(0f, 1f)] public float eveningFraction = 0.25f;
        [Range(0f, 1f)] public float nightFraction = 0.25f;

        [Header("Base Phase Colors")]
        [Tooltip("Base tint for Morning (warm dawn light)")]
        public Color morningColor = new Color(1.0f, 0.62f, 0.45f, 0.20f);
        [Tooltip("Base tint for Day/Afternoon (mostly white light)")]
        public Color dayColor = new Color(1.0f, 1.0f, 1.0f, 0.06f);
        [Tooltip("Base tint for Evening/Dusk (reddish)")]
        public Color eveningColor = new Color(1.0f, 0.55f, 0.38f, 0.22f);
        [Tooltip("Base tint for Night (deep blue)")]
        public Color nightColor = new Color(0.25f, 0.35f, 0.70f, 0.40f);

        [Header("Color Variance per Day")]
        [Tooltip("Max percent variation applied to each phase color at the start of each day (0..0.15 = 0-15%).")]
        [Range(0f, 0.5f)] public float variancePercent = 0.15f;
        [Tooltip("Whether to apply variance to alpha as well as RGB.")]
        public bool varianceAffectsAlpha = true;

        [Header("Blending")]
        [Tooltip("Blend smoothing between phases. 0 = linear, 1 = very smooth.")]
        [Range(0f, 1f)] public float blendSmoothness = 1f;

        [Header("Per-Sprite Apply Settings")]
        [Tooltip("Auto-find all SpriteRenderers in the scene and tint them.")]
        public bool autoDiscoverSprites = true;
        [Tooltip("Refresh discovery this often (seconds). Set 0 to only discover on enable or manual calls.")]
        [Range(0f, 10f)] public float discoverInterval = 1.0f;
        [Tooltip("Include inactive SpriteRenderers during discovery.")]
        public bool includeInactive = false;
        [Tooltip("Only affect SpriteRenderers on these layers.")]
        public LayerMask spriteLayerMask = ~0; // Everything
        [Tooltip("If non-empty, ignore SpriteRenderers with this tag.")]
        public string excludeTag = "";
        [Tooltip("Intensity multiplier for per-sprite multiply effect (1 = default, >1 = more vibrant).")]
        [Range(0f, 5f)] public float multiplyVibrance = 1f;

        [Header("Inspector Controls (runtime)")]
        [Tooltip("Shows the current virtual time of day (derived from cycle progress).")]
        [SerializeField] private string virtualTime = "--:--";

        // Runtime state
        private float _startTime;
        private bool _isPlaying;
        private float _pausedT01; // frozen time when paused
        private float[] _fractionsNorm = new float[4];
        private float[] _durations = new float[4];
        private float[] _accumTimes = new float[4];
        private Color[] _phaseBase = new Color[4];
        private Color[] _phaseKeys = new Color[4]; // randomized per day
        private float _lastT01 = -1f; // detect wrap to new "day"

        // Rotate phase segmentation by -6 hours so that 00:00 falls into Night.
        // Internally we still store phases in the order Morning, Day, Evening, Night,
        // but visually they are aligned to real clock windows:
        // Night(0-6), Morning(6-12), Day/Afternoon(12-18), Evening/Dusk(18-24).
        private const float PhaseShift01 = 0.75f; // +0.75 == -6 hours in a 24h cycle

        private struct SpriteEntry
        {
            public SpriteRenderer sr;
            public Color baseColor; // original SpriteRenderer.color
        }

        private readonly List<SpriteEntry> _sprites = new List<SpriteEntry>(256);
        private readonly Dictionary<SpriteRenderer, int> _spriteIndex = new Dictionary<SpriteRenderer, int>();
        private float _nextDiscoverTime;

        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Gets or sets the normalized cycle position (0..1).
        /// On get, returns the current t value.
        /// On set, jumps the cycle to that position and resumes playing.
        /// </summary>
        public float CycleTime01
        {
            get => CurrentTime01();
            set
            {
                float t = Mathf.Clamp01(value);
                float total = Mathf.Max(0.01f, cycleSeconds);
                _startTime = TimeNow() - (t * total);
                _pausedT01 = t;
                ApplyColor(EvaluateColor(t));
            }
        }

        /// <summary>Resolves the overlay image and initializes phase color and fraction data.</summary>
        private void Awake()
        {
            if (overlayImage == null) overlayImage = GetComponent<Image>();
            CacheBaseColors();
            NormalizeFractions();
            transform.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        }

        /// <summary>Discovers sprites if needed, and starts or previews the cycle.</summary>
        private void OnEnable()
        {
            if (applyMode != ApplyMode.OverlayImage)
            {
                DiscoverSprites(true);
                _nextDiscoverTime = TimeNow() + Mathf.Max(0f, discoverInterval);
            }

            if (playOnEnable) StartCycle();
            else
            {
                RandomizePhaseKeys();
                ApplyColor(EvaluateColor(CurrentTime01()));
            }
        }

        /// <summary>Stops the cycle and restores original sprite colors.</summary>
        private void OnDisable()
        {
            _isPlaying = false;
            // Restore original colors when we stop, but only if we were altering sprites
            if (applyMode != ApplyMode.OverlayImage)
            {
                for (int i = 0; i < _sprites.Count; i++)
                {
                    var e = _sprites[i];
                    if (e.sr != null)
                    {
                        var c = e.baseColor;
                        e.sr.color = c;
                    }
                }
            }
        }

        /// <summary>Starts the day/night cycle from the configured offset, randomizing phase colors.</summary>
        public void StartCycle()
        {
            NormalizeFractions();
            _startTime = TimeNow() - Mathf.Repeat(startOffsetSeconds, Mathf.Max(0.01f, cycleSeconds));
            _pausedT01 = CurrentTime01();
            _isPlaying = true;
            RandomizePhaseKeys();
            // apply initial color immediately
            ApplyColor(EvaluateColor(CurrentTime01()));
        }

        /// <summary>Pauses the cycle, freezing at the current time.</summary>
        public void Pause()
        {
            _pausedT01 = CurrentTime01();
            _isPlaying = false;
        }

        /// <summary>Resumes the cycle from the paused time position.</summary>
        public void Resume()
        {
            // Resume from paused T by shifting the start time so CurrentTime01() returns paused value at this moment
            float total = Mathf.Max(0.01f, cycleSeconds);
            _startTime = TimeNow() - (_pausedT01 * total);
            _isPlaying = true;
        }

        /// <summary>Updates the total cycle duration and recalculates phase fractions.</summary>
        public void SetCycleSeconds(float seconds)
        {
            cycleSeconds = Mathf.Max(0.01f, seconds);
            NormalizeFractions();
        }

        /// <summary>Jumps to a specific phase at the given position within it and pauses.</summary>
        public void GoToPhase(DayPhase phase, float position01WithinPhase = 0.5f)
        {
            position01WithinPhase = Mathf.Clamp01(position01WithinPhase);
            float total = Mathf.Max(0.01f, cycleSeconds);
            int idx = (int)phase;
            float segStart = (idx == 0) ? 0f : _accumTimes[idx - 1];
            float segDur = _durations[idx];

            // We want to land in the chosen phase after the visual alignment (PhaseShift01).
            // Compute the desired t on the aligned timeline, then convert back to base t01.
            float desiredAlignedT01 = (segStart + segDur * position01WithinPhase) / total;
            float baseT01 = Mathf.Repeat(desiredAlignedT01 - PhaseShift01, 1f);

            _pausedT01 = baseT01;
            _isPlaying = false;

            // apply immediately
            Color c = EvaluateColor(_pausedT01);
            ApplyColor(c);
            if (applyMode != ApplyMode.OverlayImage)
                ApplyToSprites(c);
        }

        /// <summary>Drives sprite discovery, day-wrap detection, phase color evaluation, and color application each frame.</summary>
        private void Update()
        {
            // Always update virtual time string to reflect current t
            UpdateVirtualTimeString(CurrentTime01());

            if (!_isPlaying) return;

            if (applyMode != ApplyMode.OverlayImage && autoDiscoverSprites && discoverInterval > 0f && TimeNow() >= _nextDiscoverTime)
            {
                DiscoverSprites(false);
                _nextDiscoverTime = TimeNow() + discoverInterval;
            }

            float t01 = CurrentTime01();

            // Detect wrap-around to a new day (t goes from ~1 back to 0)
            if (_lastT01 >= 0f && t01 < _lastT01)
            {
                // New day -> randomize phase keys
                RandomizePhaseKeys();
            }
            _lastT01 = t01;

            Color phaseColor = EvaluateColor(t01);
            ApplyColor(phaseColor);
            if (applyMode != ApplyMode.OverlayImage)
            {
                ApplyToSprites(phaseColor);
            }

            // Write snapshot for cross-scene use (Hub, PostBattle)
            DayNightState.T01 = t01;
            DayNightState.OverlayColor = phaseColor;
            DayNightState.HasSnapshot = true;
        }

        /// <summary>Time now.</summary>
        private float TimeNow() => useUnscaledTime ? Time.unscaledTime : Time.time;

        /// <summary>Current time01.</summary>
        private float CurrentTime01()
        {
            if (!_isPlaying) return _pausedT01;
            if (cycleSeconds <= 0.0001f) return 0f;
            float elapsed = TimeNow() - _startTime;
            if (!loop) elapsed = Mathf.Min(elapsed, cycleSeconds);
            float t = (elapsed % cycleSeconds) / cycleSeconds;
            return Mathf.Clamp01(t);
        }

        /// <summary>Returns the current DayPhase based on the cycle progress.</summary>
        public DayPhase GetCurrentPhase()
        {
            float total = Mathf.Max(0.01f, cycleSeconds);
            // Align segmentation with real-world windows (Night starts at 00:00).
            float tAlignedSec = Mathf.Repeat(CurrentTime01() + PhaseShift01, 1f) * total;

            if (tAlignedSec <= _accumTimes[0]) return DayPhase.Morning;
            if (tAlignedSec <= _accumTimes[1]) return DayPhase.Day;
            if (tAlignedSec <= _accumTimes[2]) return DayPhase.Evening;
            return DayPhase.Night;
        }

        /// <summary>
        /// Returns the base T01 value at the midpoint of the given phase.
        /// Useful for targeting a phase during sleep transitions.
        /// </summary>
        public float GetPhaseMidpointT01(DayPhase phase)
        {
            NormalizeFractions();
            float total = Mathf.Max(0.01f, cycleSeconds);
            int idx = (int)phase;
            float segStart = (idx == 0) ? 0f : _accumTimes[idx - 1];
            float segDur = _durations[idx];
            float alignedT01 = (segStart + segDur * 0.5f) / total;
            return Mathf.Repeat(alignedT01 - PhaseShift01, 1f);
        }

        /// <summary>Copies the inspector phase colors into the base color array.</summary>
        private void CacheBaseColors()
        {
            _phaseBase[(int)DayPhase.Morning] = morningColor;
            _phaseBase[(int)DayPhase.Day] = dayColor;
            _phaseBase[(int)DayPhase.Evening] = eveningColor;
            _phaseBase[(int)DayPhase.Night] = nightColor;
        }

        /// <summary>Normalizes phase fractions to sum to 1 and computes duration and accumulation arrays.</summary>
        private void NormalizeFractions()
        {
            float sum = Mathf.Max(0.0001f, morningFraction + dayFraction + eveningFraction + nightFraction);
            _fractionsNorm[0] = Mathf.Max(0f, morningFraction) / sum;
            _fractionsNorm[1] = Mathf.Max(0f, dayFraction) / sum;
            _fractionsNorm[2] = Mathf.Max(0f, eveningFraction) / sum;
            _fractionsNorm[3] = Mathf.Max(0f, nightFraction) / sum;

            float total = Mathf.Max(0.01f, cycleSeconds);
            for (int i = 0; i < 4; i++)
            {
                _durations[i] = total * _fractionsNorm[i];
            }
            _accumTimes[0] = _durations[0];
            _accumTimes[1] = _accumTimes[0] + _durations[1];
            _accumTimes[2] = _accumTimes[1] + _durations[2];
            _accumTimes[3] = _accumTimes[2] + _durations[3]; // should equal total
        }

        /// <summary>Applies random HSV variance to each phase base color for the current day.</summary>
        private void RandomizePhaseKeys()
        {
            CacheBaseColors(); // in case user edited at runtime
            for (int i = 0; i < 4; i++)
            {
                _phaseKeys[i] = ApplyVariance(_phaseBase[i], variancePercent, varianceAffectsAlpha);
            }
        }

        /// <summary>Applies the variance.</summary>
        private static Color ApplyVariance(Color baseCol, float variance, bool affectAlpha)
        {
            variance = Mathf.Max(0f, variance);
            // Convert to HSV for more natural tints
            Color.RGBToHSV(baseCol, out float h, out float s, out float v);
            float hVar = variance * 0.1f; // limit hue drift (smaller than sat/val)
            float sVar = variance;
            float vVar = variance;
            float aVar = affectAlpha ? variance : 0f;

            float Rand(float range) => (range <= 0f) ? 0f : Random.Range(-range, range);

            h = Mathf.Repeat(h + Rand(hVar), 1f);
            s = Mathf.Clamp01(s * (1f + Rand(sVar)));
            v = Mathf.Clamp01(v * (1f + Rand(vVar)));
            float a = Mathf.Clamp01(baseCol.a * (1f + Rand(aVar)));

            Color outCol = Color.HSVToRGB(h, s, v);
            outCol.a = a;
            return outCol;
        }

        /// <summary>Evaluates the blended phase color at the given normalized cycle time.</summary>
        private Color EvaluateColor(float t01)
        {
            // Align t01 to a phase timeline where Night begins at 00:00.
            float total = Mathf.Max(0.01f, cycleSeconds);
            float tAligned01 = Mathf.Repeat(t01 + PhaseShift01, 1f);
            float tSec = tAligned01 * total;

            // Determine which segment we are in (Morning, Day/Afternoon, Evening/Dusk, Night)
            int seg = 0; float segStart = 0f; float segEnd = _accumTimes[0];
            if (tSec <= _accumTimes[0]) { seg = 0; segStart = 0f; segEnd = _accumTimes[0]; }
            else if (tSec <= _accumTimes[1]) { seg = 1; segStart = _accumTimes[0]; segEnd = _accumTimes[1]; }
            else if (tSec <= _accumTimes[2]) { seg = 2; segStart = _accumTimes[1]; segEnd = _accumTimes[2]; }
            else { seg = 3; segStart = _accumTimes[2]; segEnd = _accumTimes[3]; }

            int next = (seg + 1) % 4;
            float u = (segEnd - segStart) > 0f ? Mathf.InverseLerp(segStart, segEnd, tSec) : 0f;

            // Smooth blending
            if (blendSmoothness > 0f)
            {
                // Map to smoothstep based on blendSmoothness (0..1)
                // blendSmoothness=1 => strong smoothstep; 0 => linear
                u = Mathf.SmoothStep(0f, 1f, Mathf.Lerp(u, Mathf.SmoothStep(0f, 1f, u), blendSmoothness));
            }

            // Phases stored as [Morning, Day, Evening, Night] but aligned to clock windows via tAligned01.
            return Color.LerpUnclamped(_phaseKeys[seg], _phaseKeys[next], u);
        }

        /// <summary>Applies the evaluated color to the overlay image (or hides it for sprite-only mode).</summary>
        private void ApplyColor(Color c)
        {
            if (overlayImage == null) return;
            if (applyMode == ApplyMode.PerSpriteMultiply)
            {
                // Force overlay hidden when not using it
                var oc = overlayImage.color;
                overlayImage.color = new Color(oc.r, oc.g, oc.b, 0f);
                return;
            }
            overlayImage.color = c;
        }

        /// <summary>Applies the to sprites.</summary>
        private void ApplyToSprites(Color tint)
        {
            // Convert overlay-style color to a multiplicative tint around white using alpha as intensity
            Color mul = MakeMultiplier(tint, multiplyVibrance);
            for (int i = 0; i < _sprites.Count; i++)
            {
                var e = _sprites[i];
                var sr = e.sr;
                if (sr == null) continue;

                // multiply RGB, keep original alpha
                var baseC = e.baseColor;
                Color outC = new Color(baseC.r * mul.r, baseC.g * mul.g, baseC.b * mul.b, baseC.a);
                sr.color = outC;
            }
        }

        /// <summary>Creates the multiplier.</summary>
        private static Color MakeMultiplier(Color overlayTint, float vibrance)
        {
            // Interpret overlayTint.a as intensity of the tint relative to white (1). Do not affect alpha.
            float a = Mathf.Clamp01(overlayTint.a);
            // Non-linear mapping to increase perceived intensity without hard clipping when vibrance > 1
            float t = (vibrance <= 0f) ? 0f : 1f - Mathf.Pow(1f - a, Mathf.Max(0.001f, vibrance));
            Color noA = new Color(overlayTint.r, overlayTint.g, overlayTint.b, 1f);
            Color mul = Color.Lerp(Color.white, noA, t);
            mul.a = 1f;
            return mul;
        }

        /// <summary>Discover sprites.</summary>
        public void DiscoverSprites(bool clearExisting)
        {
            if (clearExisting)
            {
                _sprites.Clear();
                _spriteIndex.Clear();
            }

            var srs = includeInactive ? Resources.FindObjectsOfTypeAll<SpriteRenderer>() : GameObject.FindObjectsOfType<SpriteRenderer>();
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                if (sr == null) continue;
                if (!includeInactive && !sr.gameObject.activeInHierarchy) continue;
                if (((1 << sr.gameObject.layer) & spriteLayerMask.value) == 0) continue;
                if (!string.IsNullOrEmpty(excludeTag) && sr.CompareTag(excludeTag)) continue;
                if (_spriteIndex.ContainsKey(sr)) continue;

                var entry = new SpriteEntry { sr = sr, baseColor = sr.color };

                _spriteIndex[sr] = _sprites.Count;
                _sprites.Add(entry);
            }
        }

        /// <summary>Updates the virtual time string.</summary>
        private void UpdateVirtualTimeString(float t01)
        {
            // Map 0..1 to a 24-hour clock
            float totalSeconds = 24f * 60f * 60f * t01;
            int seconds = Mathf.FloorToInt(totalSeconds);
            int hour24 = (seconds / 3600) % 24;
            int minute = (seconds % 3600) / 60;
            bool pm = hour24 >= 12;
            int hour12 = hour24 % 12; if (hour12 == 0) hour12 = 12;
            // e.g., 08:31pm or 6:45am (choose no-leading zero hour)
            string hourStr = hour12.ToString();
            string minStr = minute.ToString("D2");
            string suffix = pm ? "pm" : "am";
            virtualTime = string.Concat(hourStr, ":", minStr, suffix);
        }

#if UNITY_EDITOR
        /// <summary>Editor callback when inspector values change.</summary>
        private void OnValidate()
        {
            if (cycleSeconds < 0.01f) cycleSeconds = 0.01f;
            if (overlayImage == null) overlayImage = GetComponent<Image>();
            NormalizeFractions();
            CacheBaseColors();
            // Preview in editor if playing
            Color c = EvaluateColor(CurrentTime01());
            ApplyColor(c);
            if (applyMode != ApplyMode.OverlayImage)
            {
                // Make sure we have a list when previewing changes in editor
                if (_sprites.Count == 0 && autoDiscoverSprites)
                {
                    DiscoverSprites(true);
                }
                ApplyToSprites(c);
            }
            UpdateVirtualTimeString(CurrentTime01());
        }
#endif
    }
}
