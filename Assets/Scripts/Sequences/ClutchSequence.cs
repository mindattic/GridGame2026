using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using c = Scripts.Helpers.CanvasHelper;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// CLUTCHSEQUENCE - The Clutch! miracle save (US-025, game_bible.md §13.4 casting prose).
    ///
    /// <para>PURPOSE: When the rare LCK-driven Clutch outcome procs on an interrupted cast
    /// (<see cref="Scripts.Services.CastInterruptResolver"/> rolls it first, before the WIS poise
    /// shrug), the caster doesn't just shrug the hit — the in-flight spell <b>snaps to the trigger
    /// and resolves on the spot</b>. Designed so a dying healer can miraculously let off one last
    /// spell before collapsing. This sequence is the <b>juice</b>: a white screen flash + "Heal" SFX
    /// + "Clutch!" combat text, then <see cref="TimelineIcon.ForceResolve"/> drives the normal
    /// resolution path (identical to a natural u=1 arrival).</para>
    ///
    /// <para>The spell-icon is paused by the caller the instant Clutch procs so it can't reach the
    /// trigger on its own (which would resolve without the juice) before this sequence runs.
    /// A null icon is tolerated — the flash/SFX/text still play (used by the Debug demo).</para>
    ///
    /// RELATED FILES: CastInterruptResolver.cs, TimelineBarInstance.cs (InterruptCastsByOwner),
    /// TimelineIcon.cs (ForceResolve), DebugManager.cs (Demo_Clutch).
    /// </summary>
    public class ClutchSequence : SequenceEvent
    {
        private readonly TimelineIcon spellIcon;
        private readonly ActorInstance caster;

        /// <summary>The flash's peak alpha and total duration (seconds).</summary>
        private const float FlashPeakAlpha = 0.75f;
        private const float FlashDuration = 0.3f;

        public ClutchSequence(TimelineIcon spellIcon, ActorInstance caster)
        {
            this.spellIcon = spellIcon;
            this.caster = caster;
        }

        /// <summary>Coroutine that plays the Clutch juice, then force-resolves the cast.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // SFX + combat text fire together with the flash for one dramatic beat.
            g.AudioManager?.Play("Heal");
            if (caster != null)
                g.CombatTextManager?.Spawn("Clutch!", caster.Position, "Heal");

            yield return FlashRoutine();

            // Snap to the trigger and resolve via the normal cast-resolution closure.
            // Guarded internally (no-op if the icon already fired or was destroyed).
            spellIcon?.ForceResolve();
        }

        /// <summary>A quick full-screen white flash on the UI canvas, faded in then out.</summary>
        private IEnumerator FlashRoutine()
        {
            var canvas = c.Canvas;
            if (canvas == null) yield break;

            // Transient full-screen white Image. new GameObject (not Instantiate) keeps the
            // InstantiateBan guardrail satisfied — this is throwaway VFX, not a prefab.
            var go = new GameObject("ClutchFlash");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling(); // render above the HUD
            var img = go.AddComponent<Image>();
            img.raycastTarget = false; // never eat input
            img.color = new Color(1f, 1f, 1f, 0f);

            float half = FlashDuration * 0.5f;
            // Ramp up.
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, FlashPeakAlpha, t / half));
                yield return Wait.OneTick();
            }
            // Ramp down.
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(FlashPeakAlpha, 0f, t / half));
                yield return Wait.OneTick();
            }

            Object.Destroy(go);
        }
    }
}
