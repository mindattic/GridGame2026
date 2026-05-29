using System.Collections;
using UnityEngine;
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
                BuffSystem.Apply(target, buff);
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

            // Fix #3: route through VisualEffectManager so it owns lifetime (pool/dedup). Falls
            // back to direct Destroy if the manager dropped its reference.
            if (instance != null)
            {
                if (g.VisualEffectManager != null && !string.IsNullOrEmpty(asset.Name))
                    g.VisualEffectManager.Despawn(asset.Name);
                if (instance != null && instance.gameObject != null) GameObject.Destroy(instance.gameObject);
            }
        }

        private static void PlayVfx(string name, Vector3 position)
        {
            if (string.IsNullOrEmpty(name) || g.VisualEffectManager == null) return;
            var asset = VisualEffectLibrary.Get(name);
            if (asset != null) g.VisualEffectManager.Spawn(asset, position);
        }

    }
}
