using System.Collections.Generic;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// SPELLLIBRARY - Common-RPG spell catalog using the holistic targeting triad
    /// (<see cref="TargetShape"/> + <see cref="TargetMode"/> + <see cref="TargetFilter"/>).
    ///
    /// <para>VFX names refer to entries in <c>VisualEffectLibrary</c>. After the VFX-prefab author
    /// menus run (see <c>Editor/VfxPrefabAuthor.cs</c>) and library registrations land, the names
    /// here can be swapped to the new custom prefabs (FlamingTwist, IcyWind, ShockBolt, …) without
    /// changing the gameplay shape.</para>
    /// </summary>
    public static class SpellLibrary
    {
        // ── Offensive — varied shapes per spell ──

        // Fire: single-target enemy, flaming twist projectile, burning debuff.
        public static readonly SpellDefinition Fire = new SpellDefinition(
            ability: ManaAbilities.Fireball,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "Flame", projectileVfx: "Fireball", motion: ProjectileMotion.Twist,
            impactVfx: "PuffyExplosion", lingerVfx: "Flame",
            debuffId: "burning", baseDamage: 18f, damageType: DamageType.Fire, projectileSeconds: 0.55f);

        // Ice: pick a tile, 3×3 square AOE of enemies hit by Frost.
        public static readonly SpellDefinition Ice = new SpellDefinition(
            ability: ManaAbilities.Frost,
            shape: TargetShape.Square, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: "IceSparkle", projectileVfx: "IceSparkle", motion: ProjectileMotion.Bezier,
            impactVfx: "BlueGlow", lingerVfx: "BlueGlow",
            debuffId: "frozen", baseDamage: 10f, damageType: DamageType.Ice, projectileSeconds: 0.6f);

        // Lightning: pick an enemy, hits the entire ROW (chains through everyone in that line).
        public static readonly SpellDefinition Lightning = new SpellDefinition(
            ability: ManaAbilities.Bolt,
            shape: TargetShape.Row, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "LightningExplosion",
            baseDamage: 14f, damageType: DamageType.Lightning, projectileSeconds: 0.35f);

        // Poison: pick a tile, CROSS pattern (center + 4 cardinals = 5 tiles). Toxic spread.
        public static readonly SpellDefinition Poison = new SpellDefinition(
            ability: ManaAbilities.Frost,
            shape: TargetShape.Cross, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 1,
            castVfx: "ToxicCloud", projectileVfx: "AcidSplash", motion: ProjectileMotion.Bezier,
            impactVfx: "AcidSplash", lingerVfx: "ToxicCloud",
            debuffId: "poisoned", baseDamage: 6f, damageType: DamageType.Poison, projectileSeconds: 0.55f);

        // ── Control / status ──

        // Sleep: pick a single enemy.
        public static readonly SpellDefinition Sleep = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "PinkDust", projectileVfx: "PinkDust", motion: ProjectileMotion.Homing,
            impactVfx: "PinkSpark",
            debuffId: "sleep", projectileSeconds: 0.6f);

        // Slow: pick an enemy, applies to their entire ROW (slows a whole rank).
        public static readonly SpellDefinition Slow = new SpellDefinition(
            ability: ManaAbilities.Frost,
            shape: TargetShape.Row, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "BlueGlow", projectileVfx: "BlueGlow", motion: ProjectileMotion.Bezier,
            impactVfx: "Bubble", lingerVfx: "Bubble",
            debuffId: "slowed", damageType: DamageType.Ice,
            projectileSeconds: 0.5f);

        // Silence: pick a single enemy.
        public static readonly SpellDefinition Silence = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "PinkSpark", projectileVfx: "PinkDust", motion: ProjectileMotion.Straight,
            impactVfx: "PinkSpark",
            debuffId: "silenced", damageType: DamageType.Arcane,
            projectileSeconds: 0.4f);

        // ── Support / utility ──

        // Heal: pick a single ally.
        public static readonly SpellDefinition Heal = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.AllyOnly,
            castVfx: "BuffLife", motion: ProjectileMotion.None,
            impactVfx: "GreenSparkle", lingerVfx: "BuffLife",
            baseHeal: 25f);

        // Mass Heal: all allies (no pick).
        public static readonly SpellDefinition MassHeal = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.AllAllies, mode: TargetMode.Auto, filter: TargetFilter.AllyOnly,
            castVfx: "BuffLife", motion: ProjectileMotion.None,
            impactVfx: "GreenSparkle", lingerVfx: "BuffLife",
            baseHeal: 12f);

        // Antidote: pick a single ally; cleanses ALL debuffs on impact.
        public static readonly SpellDefinition Antidote = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.AllyOnly,
            castVfx: "GoldSparkle", motion: ProjectileMotion.None,
            impactVfx: "GoldSparkle",
            removesDebuffs: true);

        // Scan: pick a single enemy; reveals stats (TBD).
        public static readonly SpellDefinition Scan = new SpellDefinition(
            ability: ManaAbilities.Heal,
            shape: TargetShape.SingleActor, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "GodRays", projectileVfx: "BlueGlow", motion: ProjectileMotion.Straight,
            impactVfx: "GodRays",
            projectileSeconds: 0.4f);

        // Meteor: pick a tile, big diamond AOE hits all enemies in radius 2.
        public static readonly SpellDefinition Meteor = new SpellDefinition(
            ability: ManaAbilities.Fireball,
            shape: TargetShape.Diamond, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly, radius: 2,
            castVfx: "FireRain", projectileVfx: "Fireball", motion: ProjectileMotion.Strike,
            impactVfx: "PuffyExplosion", lingerVfx: "Flame",
            debuffId: "burning", baseDamage: 22f, damageType: DamageType.Fire, projectileSeconds: 0.55f);

        // Shock Wave: pick an enemy; hits their entire COLUMN (vertical line).
        public static readonly SpellDefinition ShockWave = new SpellDefinition(
            ability: ManaAbilities.Bolt,
            shape: TargetShape.Column, mode: TargetMode.PickActor, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "LightningExplosion",
            baseDamage: 10f, damageType: DamageType.Lightning);

        // Cross-Hit: pick a tile, Plus shape (entire row + column = big board-wide +).
        public static readonly SpellDefinition CrossHit = new SpellDefinition(
            ability: ManaAbilities.Bolt,
            shape: TargetShape.Plus, mode: TargetMode.PickTile, filter: TargetFilter.EnemyOnly,
            castVfx: "RayBlast", projectileVfx: "LightningStrike", motion: ProjectileMotion.Strike,
            impactVfx: "LightningExplosion",
            baseDamage: 8f, damageType: DamageType.Lightning);

        /// <summary>All spells in display order — for debug menus / random-loadout picks.</summary>
        public static readonly IReadOnlyList<SpellDefinition> All = new[]
        {
            Fire, Ice, Lightning, Poison, Sleep, Slow, Silence,
            Heal, MassHeal, Antidote, Scan,
            Meteor, ShockWave, CrossHit
        };
    }
}
