using System.Collections.Generic;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// SPELLLIBRARY - Common-RPG spell catalog using the holistic targeting triad
    /// (<see cref="TargetShape"/> + <see cref="TargetMode"/> + <see cref="TargetFilter"/>).
    ///
    /// <para>VFX names refer to entries in <c>VisualEffectLibrary</c>. The custom per-spell prefabs
    /// (FlamingTwist, IcyWind, ShockBolt, …) authored by <c>Editor/VfxPrefabAuthor.cs</c> are now
    /// generated, registered, and referenced below — re-run <c>Tools/VFX/Author ALL Custom
    /// Prefabs</c> to regenerate them; gameplay shape is unaffected by VFX swaps.</para>
    /// </summary>
    public static class SpellLibrary
    {
        // ── Offensive — varied shapes per spell ──

        // Fire: single-target enemy, flaming twist projectile, burning debuff.
        public static readonly SpellDefinition Fire = new SpellDefinition(
            ability: ManaAbilities.Fireball,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "Flame", projectileVfx: "FlamingTwist", motion: ProjectileMotion.Twist,
            impactVfx: "PuffyExplosion", lingerVfx: "Flame",
            debuffId: "burning", baseDamage: 18f, damageType: DamageType.Fire, projectileSeconds: 0.55f);

        // Ice: pick a tile, 3×3 square AOE of enemies hit by Frost. CastVfx is intentionally null —
        // IceSparkle is a LOOPING prefab in VisualEffectLibrary, so playing it at the caster would
        // stick on them permanently. The icy persistent effect belongs on the TARGET (Linger).
        public static readonly SpellDefinition Ice = new SpellDefinition(
            ability: ManaAbilities.Frost,
            shape: TargetShape.Square, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: null, projectileVfx: "IceSparkle", motion: ProjectileMotion.Bezier,
            impactVfx: "IcyWind", lingerVfx: "IceSparkle",
            debuffId: "frozen", baseDamage: 10f, damageType: DamageType.Ice, projectileSeconds: 0.6f);

        // Lightning: pick an enemy, hits the entire ROW (chains through everyone in that line).
        public static readonly SpellDefinition Lightning = new SpellDefinition(
            ability: ManaAbilities.Bolt,
            shape: TargetShape.Row, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "ShockBolt",
            baseDamage: 14f, damageType: DamageType.Lightning, projectileSeconds: 0.35f);

        // Poison: pick a tile, CROSS pattern (center + 4 cardinals = 5 tiles). Toxic spread.
        public static readonly SpellDefinition Poison = new SpellDefinition(
            ability: ManaAbilities.Poison,
            shape: TargetShape.Cross, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: "ToxicCloud", projectileVfx: "AcidSplash", motion: ProjectileMotion.Bezier,
            impactVfx: "AcidSplash", lingerVfx: "PoisonCloud",
            debuffId: "poisoned", baseDamage: 6f, damageType: DamageType.Poison, projectileSeconds: 0.55f);

        // ── Control / status ──

        // Sleep: pick a single enemy.
        public static readonly SpellDefinition Sleep = new SpellDefinition(
            ability: ManaAbilities.Sleep,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "PinkDust", projectileVfx: "PinkDust", motion: ProjectileMotion.Homing,
            impactVfx: "SleepDust",
            debuffId: "sleep", projectileSeconds: 0.6f);

        // Slow: pick an enemy, applies to their entire ROW (slows a whole rank).
        public static readonly SpellDefinition Slow = new SpellDefinition(
            ability: ManaAbilities.Slow,
            shape: TargetShape.Row, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "BlueGlow", projectileVfx: "BlueGlow", motion: ProjectileMotion.Bezier,
            impactVfx: "SlowShimmer", lingerVfx: "Bubble",
            debuffId: "slowed", damageType: DamageType.Ice,
            projectileSeconds: 0.5f);

        // Quicken (US-028): pick any actor, slide its timeline icon FORWARD toward the trigger (the
        // inverse of pushback). No damage — a pure tempo tool. Cast on an enemy to bait its turn
        // early (act before a more dangerous ally / out of a forming pincer), or on a charging ally
        // to rush a cast. HastenU is the forward push in u; overtaking is emergent (turn order =
        // arrival-at-trigger). DamageType irrelevant (no damage).
        public static readonly SpellDefinition Quicken = new SpellDefinition(
            ability: ManaAbilities.Quicken,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.Any,
            castVfx: "GoldSparkle", projectileVfx: "BlueGlow", motion: ProjectileMotion.Straight,
            impactVfx: "GoldSparkle",
            projectileSeconds: 0.35f,
            hastenU: 0.30f);

        // Silence: pick a single enemy.
        public static readonly SpellDefinition Silence = new SpellDefinition(
            ability: ManaAbilities.Silence,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "PinkSpark", projectileVfx: "PinkDust", motion: ProjectileMotion.Straight,
            impactVfx: "SilenceMute",
            debuffId: "silenced", damageType: DamageType.Arcane,
            projectileSeconds: 0.4f);

        // ── Support / utility ──

        // Heal: pick a single ally.
        public static readonly SpellDefinition Heal = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.AllyOnly,
            castVfx: "BuffLife", motion: ProjectileMotion.None,
            impactVfx: "HealAura", lingerVfx: "BuffLife",
            baseHeal: 25f);

        // Mass Heal: all allies (no pick).
        public static readonly SpellDefinition MassHeal = new SpellDefinition(
            ability: ManaAbilities.MassHeal,
            shape: TargetShape.AllAllies, mode: TargetMode.Auto, filter: TargetFilter.AllyOnly,
            castVfx: "BuffLife", motion: ProjectileMotion.None,
            impactVfx: "HealAura", lingerVfx: "BuffLife",
            baseHeal: 12f);

        // Antidote: pick a single ally; cleanses ALL debuffs on impact.
        public static readonly SpellDefinition Antidote = new SpellDefinition(
            ability: ManaAbilities.Antidote,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.AllyOnly,
            castVfx: "GoldSparkle", motion: ProjectileMotion.None,
            impactVfx: "AntidoteSparkle",
            removesDebuffs: true);

        // Scan (US-077): pick a single enemy; reveals its stats + flags it Seen in the Bestiary.
        public static readonly SpellDefinition Scan = new SpellDefinition(
            ability: ManaAbilities.Scan,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "GodRays", projectileVfx: "BlueGlow", motion: ProjectileMotion.Straight,
            impactVfx: "ScanRays",
            projectileSeconds: 0.4f,
            revealsStats: true);

        // Meteor: pick a tile, big diamond AOE hits all enemies in radius 2.
        public static readonly SpellDefinition Meteor = new SpellDefinition(
            ability: ManaAbilities.Meteor,
            shape: TargetShape.Diamond, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 2,
            castVfx: "FireRain", projectileVfx: "Fireball", motion: ProjectileMotion.Strike,
            impactVfx: "PuffyExplosion", lingerVfx: "Flame",
            debuffId: "burning", baseDamage: 22f, damageType: DamageType.Fire, projectileSeconds: 0.55f);

        // Shock Wave: pick an enemy; hits their entire COLUMN (vertical line).
        public static readonly SpellDefinition ShockWave = new SpellDefinition(
            ability: ManaAbilities.ShockWave,
            shape: TargetShape.Column, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "LightningExplosion",
            baseDamage: 10f, damageType: DamageType.Lightning);

        // Steal: hits every adjacent (cardinal) enemy with a per-target LCK+AGI roll. On success
        // the team's ManaBank gains one random-color orb. Mode=Auto + Shape=Cross r=1 anchored on
        // caster means "self tile + 4 cardinals" → EnemyOnly filter strips the caster.
        public static readonly SpellDefinition Steal = new SpellDefinition(
            ability: ManaAbilities.Steal,
            shape: TargetShape.Cross, mode: TargetMode.Auto, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: "GoldSparkle", projectileVfx: "Feather", motion: ProjectileMotion.Homing,
            impactVfx: "GoldSparkle",
            damageType: DamageType.Arcane,
            projectileSeconds: 0.35f,
            stealsMana: true);

        // Mug: Steal + a real physical attack on each adjacent enemy. Same shape, adds damage.
        public static readonly SpellDefinition Mug = new SpellDefinition(
            ability: ManaAbilities.Mug,
            shape: TargetShape.Cross, mode: TargetMode.Auto, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: "BloodClaw", projectileVfx: "Shuriken", motion: ProjectileMotion.Straight,
            impactVfx: "PuffyExplosion", lingerVfx: "BloodClaw",
            baseDamage: 10f, damageType: DamageType.Physical,
            projectileSeconds: 0.30f,
            stealsMana: true);

        // Teleport: pick an empty tile, instantly relocate the caster, then check for an
        // incidental pincer the new position completes. Free, costs a turn. AbilityBar.HandleSkill
        // detects IsTeleport and runs a dedicated flow (bypasses SpellEffectDispatcher).
        public static readonly SpellDefinition Teleport = new SpellDefinition(
            ability: ManaAbilities.Teleport,
            shape: TargetShape.SingleTile, mode: TargetMode.PickTile, filter: TargetFilter.EmptyOnly,
            castVfx: "BlueGlow",
            impactVfx: "BlueGlow",
            isTeleport: true);

        // Cross-Hit: pick a tile, Plus shape (entire row + column = big board-wide +).
        public static readonly SpellDefinition CrossHit = new SpellDefinition(
            ability: ManaAbilities.CrossHit,
            shape: TargetShape.Plus, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "LightningExplosion",
            baseDamage: 8f, damageType: DamageType.Lightning);

        /// <summary>All spells in display order — for debug menus / random-loadout picks.</summary>
        public static readonly IReadOnlyList<SpellDefinition> All = new[]
        {
            Fire, Ice, Lightning, Poison, Sleep, Slow, Quicken, Silence,
            Heal, MassHeal, Antidote, Scan,
            Meteor, ShockWave, CrossHit,
            Steal, Mug, Teleport
        };
    }
}
