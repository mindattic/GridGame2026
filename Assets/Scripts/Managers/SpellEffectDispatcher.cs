using System.Collections;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data;
using Scripts.Instances.Actor;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Utilities;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Managers
{
    /// <summary>
    /// SPELLEFFECTDISPATCHER - Orchestrates a single spell cast end-to-end.
    ///
    /// <para>Stages, all driven from <see cref="SpellDefinition"/>:</para>
    /// <list type="number">
    ///   <item><b>Cast flash</b> at caster (CastVfxName).</item>
    ///   <item><b>Projectile</b> travels caster→target along the motion curve (ProjectileVfxName +
    ///   <see cref="ProjectileMotion"/>). Skipped if Motion = None (Heal-style, resolves at caster).</item>
    ///   <item><b>Impact</b> at target (ImpactVfxName).</item>
    ///   <item><b>Linger</b> aura on target (LingerVfxName) — e.g., a flame aura while Burning ticks.</item>
    ///   <item><b>Debuff</b> applied via <see cref="BuffSystem.Apply"/> using <c>Buffs.ById</c>.</item>
    ///   <item><b>Damage/Heal</b> applied (V1 placeholder: log; future hooks into Formulas).</item>
    /// </list>
    ///
    /// <para>Returns a coroutine so callers can await the full sequence if they need to.</para>
    /// </summary>
    public static class SpellEffectDispatcher
    {
        // (No auto-pick overload — targets always come from TargetingMode now.)

        public static void Cast(SpellDefinition spell, ActorInstance caster, ActorInstance target)
        {
            if (spell == null) return;

            // Fix #4: a null caster means no plausible origin position — bail loudly instead of
            // launching projectiles from world origin (which reads as VFX from the middle of the board).
            if (caster == null)
            {
                Debug.LogWarning($"[SpellDispatch] '{spell.Ability?.Name}' has no caster; skipping.");
                return;
            }

            MonoBehaviour runner = g.VisualEffectManager;
            if (runner == null) runner = g.ManaPoolManager;
            if (runner == null) { Debug.LogWarning("[SpellDispatch] No MonoBehaviour runner available."); return; }
            runner.StartCoroutine(Routine(spell, caster, target));
        }

        private static IEnumerator Routine(SpellDefinition spell, ActorInstance caster, ActorInstance target)
        {
            Vector3 casterPos = caster != null ? caster.transform.position : Vector3.zero;
            Vector3 targetPos = target != null ? target.transform.position : casterPos;

            // 1) Cast flash at the caster.
            PlayVfx(spell.CastVfxName, casterPos);

            // 2) Projectile (if any).
            if (spell.Motion != ProjectileMotion.None && !string.IsNullOrEmpty(spell.ProjectileVfxName))
            {
                yield return AnimateProjectile(spell, casterPos, target);
            }

            // 3) Impact at the (current) target position.
            if (target != null) targetPos = target.transform.position;
            PlayVfx(spell.ImpactVfxName, targetPos);

            // 4) Optional linger on target — parented so it follows if the actor moves.
            if (!string.IsNullOrEmpty(spell.LingerVfxName) && target != null)
            {
                var lingerAsset = VisualEffectLibrary.Get(spell.LingerVfxName);
                if (lingerAsset != null && g.VisualEffectManager != null)
                    g.VisualEffectManager.SpawnInstance(lingerAsset, targetPos, target.transform);
            }

            // 5) Apply debuff via the central BuffSystem (if any).
            if (!string.IsNullOrEmpty(spell.DebuffId) && target != null
                && Buffs.ById.TryGetValue(spell.DebuffId, out var buff))
            {
                // US-014: Sleep lands harder on a Warm target — extend its duration by
                // SleepWhenWarmMultiplier. Sleep has no separate success roll today, so the
                // bonus applies to duration; revisit if a success-chance roll is ever added.
                if (buff.Id == Buffs.Sleep.Id && BuffSystem.Has(target, Buffs.Warm.Id))
                {
                    int boosted = Mathf.RoundToInt(buff.DefaultDuration * Buffs.SleepWhenWarmMultiplier);
                    BuffSystem.Apply(target, buff, boosted);
                    var ctmS = g.CombatTextManager;
                    if (ctmS != null) ctmS.Spawn("Deep Sleep!", target.transform.position, "Miss");
                }
                else
                {
                    BuffSystem.Apply(target, buff);
                }

                // Dedicated, cadenced callout: "Slime A is poisoned", etc. + chiptune debuff cue.
                Scripts.Canvas.AnnouncementWindow.Announce($"{target.characterClass} is {buff.Id}");
                g.AudioManager?.Play("Debuff");
            }

            // 5.5) US-028 Quicken/Hasten — slide the target's timeline icon FORWARD (toward the
            // trigger), the inverse of pushback. Overtaking is emergent (turn order = arrival order).
            if (spell.HastenU > 0f && target != null && target.IsPlaying)
            {
                g.TimelineBar?.HastenIcon(target, spell.HastenU);
                var ctmH = g.CombatTextManager;
                if (ctmH != null) ctmH.Spawn("Quickened!", target.transform.position, "Heal");
            }

            // 5.6) US-077 Scan — reveal the target's stats (announced) and flag the enemy class as
            // Seen in the Bestiary (unblocks US-093's seen-gated reveal). No damage.
            if (spell.RevealsStats && target != null && target.IsPlaying && target.Stats != null)
            {
                if (target.IsEnemy)
                    Scripts.Helpers.ProfileHelper.CurrentProfile?.CurrentSave?.Bestiary?.MarkSeen(target.characterClass);
                var s = target.Stats;
                Scripts.Canvas.AnnouncementWindow.Announce(
                    $"{target.characterClass}:  HP {s.HP:0}/{s.MaxHP:0}   STR {s.Strength:0}  VIT {s.Vitality:0}  AGI {s.Agility:0}  INT {s.Intelligence:0}");
                g.CombatTextManager?.Spawn("Scanned!", target.transform.position, "Heal");
                g.AudioManager?.Play("Select");
            }

            // 6) Damage / heal / cleanse. Fix #9: target may have died / left the board between
            //    cast-start and impact — bail if no longer playing.
            if (target != null && target.IsPlaying && target.Stats != null && target.Stats.HP > 0f)
            {
                if (spell.RemovesDebuffs)
                {
                    int n = BuffSystem.RemoveAllDebuffs(target);
                    Debug.Log($"[Spell] {spell.Ability.Name} cleansed {n} debuff(s) from {target.name}.");
                }

                // Fix #8: Fire damage on a Wet target instantly evaporates the Wet (steam) — the
                // two debuffs can't logically coexist. Burning still applies afterward if rolled.
                if (spell.DamageType == DamageType.Fire && BuffSystem.Has(target, "wet"))
                {
                    BuffSystem.RemoveAllDebuffsMatching(target, "wet");
                    var ctm = g.CombatTextManager;
                    if (ctm != null) ctm.Spawn("Steam!", target.transform.position, "Damage");
                }

                // Steal / Mug — per-target LCK+AGI roll → one random orb to the team bank on success.
                if (spell.StealsMana) TryStealFrom(caster, target);

                // Lightning: 30% chance to blind on impact (independent of base damage roll).
                if (spell.DamageType == DamageType.Lightning && UnityEngine.Random.value < 0.30f)
                {
                    BuffSystem.Apply(target, Buffs.Blinded);
                    var ctmL = g.CombatTextManager;
                    if (ctmL != null) ctmL.Spawn("Blinded!", target.transform.position, "Miss");
                }

                if (spell.BaseDamage > 0f) ApplyDamage(target, spell);
                if (spell.BaseHeal   > 0f) ApplyHeal  (target, spell.BaseHeal, spell.Ability.Name);
            }
        }

        /// <summary>Compute final damage = base × elementalResistance × buffMultiplier × wet-lightning bonus, then subtract from HP.</summary>
        private static void ApplyDamage(ActorInstance target, SpellDefinition spell)
        {
            if (target == null || target.Stats == null) return;

            float dmg = spell.BaseDamage;

            // Elemental resistance from the target's ActorData (1.0 = neutral, 0.5 resistant, 2.0 weak).
            var data = Scripts.Libraries.ActorLibrary.Get(target.characterClass);
            float resMult = data != null ? data.ResistanceMultiplier(spell.DamageType) : 1f;
            // US-043: fold equipped items' ResistanceModifiers into the per-class resistance.
            resMult *= EquipmentResistanceMultiplier(target, spell.DamageType);
            dmg *= resMult;

            // Lightning × Wet bonus (existing constant in Buffs.cs).
            if (spell.DamageType == DamageType.Lightning && BuffSystem.Has(target, "wet"))
                dmg *= Buffs.LightningWhenWetMultiplier;

            // Defensive buff multiplier (Protection's 15% DR etc.).
            dmg *= BuffSystem.GetIncomingDamageMultiplier(target);

            // Fix #1: a spell that DEALS damage should always do at least 1 — high resistance can
            // round 0.x → 0, which reads as "did nothing" and feels broken. Immune targets (res=0)
            // genuinely do 0; clamp only kicks in when base > 0 and res > 0.
            int final = Mathf.RoundToInt(dmg);
            if (spell.BaseDamage > 0f && resMult > 0f) final = Mathf.Max(1, final);
            else final = Mathf.Max(0, final);

            target.Stats.HP = Mathf.Clamp(target.Stats.HP - final, 0, target.Stats.MaxHP);
            BuffSystem.OnDamaged(target);

            // Fix #1: pop combat text at the target so the damage reads on-screen.
            var ctm = g.CombatTextManager;
            if (ctm != null) ctm.Spawn(final.ToString(), target.transform.position, "Damage");

            Debug.Log($"[Spell] {spell.Ability.Name} ({spell.DamageType}) hits {target.name} for {final} (base {spell.BaseDamage:0.#}, res ×{resMult:0.##}).");
        }

        /// <summary>US-043: product of every equipped item's <c>ResistanceModifiers[type]</c> on the
        /// target (heroes only — enemies carry no equipment), combined multiplicatively with the
        /// per-class resistance in <see cref="ApplyDamage"/>. Returns 1.0 when the target has no gear
        /// or no matching modifier. Public so the Debug Window can report effective resistances.</summary>
        public static float EquipmentResistanceMultiplier(ActorInstance target, DamageType type)
        {
            if (target == null) return 1f;
            var save = Scripts.Helpers.ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment?.Heroes == null) return 1f;

            Scripts.Models.HeroEquipmentSave heroSave = null;
            foreach (var h in save.Equipment.Heroes)
                if (h != null && h.CharacterClass == target.characterClass) { heroSave = h; break; }
            if (heroSave == null) return 1f;

            float mult = 1f;
            foreach (var id in new[] { heroSave.WeaponId, heroSave.ArmorId, heroSave.Relic1Id, heroSave.Relic2Id, heroSave.Relic3Id })
            {
                if (string.IsNullOrEmpty(id)) continue;
                var item = Scripts.Data.Items.ItemLibrary.Get(id);
                if (item?.ResistanceModifiers != null && item.ResistanceModifiers.TryGetValue(type, out var m))
                    mult *= m;
            }
            return mult;
        }

        /// <summary>Roll a steal attempt against <paramref name="target"/> using
        /// <paramref name="caster"/>'s LCK + half AGI. Success → one random-color orb into the
        /// team bank + "Steal!" combat text. Failure → quiet "Miss" pop. Used by Steal and Mug.</summary>
        private static void TryStealFrom(ActorInstance caster, ActorInstance target)
        {
            if (caster == null || caster.Stats == null || target == null) return;

            float chance = Mathf.Clamp01((caster.Stats.Luck + caster.Stats.Agility * 0.5f) / 50f);
            bool success = UnityEngine.Random.value < chance;

            var ctm = g.CombatTextManager;
            if (!success)
            {
                if (ctm != null) ctm.Spawn("Miss", target.transform.position, "Miss");
                return;
            }

            var color = RandomStealColor();
            g.ManaBank?.Add(color, 1);

            if (ctm != null) ctm.Spawn($"Steal! +{color}", target.transform.position, "Heal");
            Debug.Log($"[Spell] {caster.name} stole 1 {color} orb from {target.name} (chance {chance:P0}).");
        }

        private static ManaType RandomStealColor()
        {
            int total = System.Enum.GetValues(typeof(ManaType)).Length;
            return (ManaType)UnityEngine.Random.Range(0, total);
        }

        private static void ApplyHeal(ActorInstance target, float amount, string spellName)
        {
            if (target == null || target.Stats == null) return;
            int gain = Mathf.Max(0, Mathf.RoundToInt(amount));
            target.Stats.HP = Mathf.Clamp(target.Stats.HP + gain, 0, target.Stats.MaxHP);

            // Fix #2: green heal popup so the player sees the bump.
            var ctm = g.CombatTextManager;
            if (ctm != null) ctm.Spawn($"+{gain}", target.transform.position, "Heal");

            Debug.Log($"[Spell] {spellName} heals {target.name} for {gain}.");
        }

        private static IEnumerator AnimateProjectile(SpellDefinition spell, Vector3 from, ActorInstance target)
        {
            var asset = VisualEffectLibrary.Get(spell.ProjectileVfxName);
            if (asset == null || g.VisualEffectManager == null) yield break;

            // Spawn detached so we control its transform directly.
            var instance = g.VisualEffectManager.SpawnInstance(asset, from, null);
            if (instance == null) yield break;
            var tr = instance.transform;

            float duration = Mathf.Max(0.05f, spell.ProjectileSeconds);
            float elapsed = 0f;
            Vector3 to = target != null ? target.transform.position : from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 dest = target != null ? target.transform.position : to;
                tr.position = ProjectileMotionEval.Evaluate(spell.Motion, from, dest, target != null ? target.transform : null, t);
                yield return null;
            }

            // Route through VisualEffectManager so it owns lifetime (unregisters its GUID-keyed
            // entry too). The old Despawn(asset.Name) was a no-op — instances are keyed by a
            // unique GUID, not the asset name — so it leaked a dictionary entry per projectile.
            if (instance != null)
            {
                if (g.VisualEffectManager != null)
                    g.VisualEffectManager.Despawn(instance);
                else if (instance.gameObject != null)
                    GameObject.Destroy(instance.gameObject);
            }
        }

        // Brief cast/impact flashes are spawned-and-forgotten. A LOOPING asset, though, never
        // auto-despawns (VisualEffectInstance keeps it alive until despawned by reference), so a
        // looping prefab used as a one-shot flash sticks on the actor forever — which reads as
        // "the spell's VFX parked on the caster." Bound looping flashes to a short lifetime; let
        // non-looping assets self-complete via their Duration as before. Intentional persistent
        // auras (LingerVfx) are spawned separately, parented to the target, and are unaffected.
        private const float FlashSeconds = 0.6f;

        private static void PlayVfx(string name, Vector3 position)
        {
            if (string.IsNullOrEmpty(name) || g.VisualEffectManager == null) return;
            var asset = VisualEffectLibrary.Get(name);
            if (asset == null) return;

            if (asset.IsLooping)
            {
                var instance = g.VisualEffectManager.SpawnInstance(asset, position, null);
                if (instance != null)
                    g.VisualEffectManager.StartCoroutine(DespawnAfter(instance, FlashSeconds));
            }
            else
            {
                g.VisualEffectManager.Spawn(asset, position);
            }
        }

        private static IEnumerator DespawnAfter(Scripts.Instances.VisualEffectInstance instance, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (instance != null && g.VisualEffectManager != null)
                g.VisualEffectManager.Despawn(instance);
        }

    }
}
