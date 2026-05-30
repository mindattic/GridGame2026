namespace Scripts.Models
{
    /// <summary>How the projectile travels from caster to target.</summary>
    public enum ProjectileMotion
    {
        None,       // no projectile — cast resolves at caster (Heal, Scan, Antidote)
        Straight,
        Bezier,     // arcing toss
        Homing,     // tracks live target
        Spiral,     // corkscrew
        Twist,      // gentle weave (Fireball)
        Strike,     // top-down (Lightning crashes from above)
    }

    /// <summary>
    /// SPELLDEFINITION - The full shape of a single spell: cost (ManaAbility), targeting
    /// (TargetShape + TargetMode + TargetFilter + Radius), VFX chain (cast / projectile / impact /
    /// linger names from VisualEffectLibrary), gameplay outcome (debuff id + base damage/heal).
    ///
    /// <para>Targeting is intentionally three-axis: a Square AOE can be picked by clicking an
    /// actor OR a tile; AllEnemies skips picking entirely; Self skips picking and ignores filter.
    /// Mix &amp; match to express most RPG idioms.</para>
    /// </summary>
    public sealed class SpellDefinition
    {
        public ManaAbility Ability { get; }

        // Targeting triad — Shape (geometry) + Mode (how to pick) + Filter (who counts) + Radius.
        public TargetShape Shape { get; }
        public TargetMode Mode { get; }
        public TargetFilter Filter { get; }
        public int Radius { get; }

        // VFX hooks — VisualEffectLibrary names (null = skip that stage).
        public string CastVfxName { get; }
        public string ProjectileVfxName { get; }
        public ProjectileMotion Motion { get; }
        public string ImpactVfxName { get; }
        public string LingerVfxName { get; }

        // Outcome
        public string DebuffId { get; }
        public float BaseDamage { get; }
        public float BaseHeal { get; }
        public DamageType DamageType { get; }
        public float ProjectileSeconds { get; }
        /// <summary>If true, the spell removes ALL active debuffs from the target on impact (Antidote, Cleanse).</summary>
        public bool RemovesDebuffs { get; }

        /// <summary>If true, each target gets a per-target steal roll (LCK + 0.5 × AGI) / 50 → on
        /// success, one random-color orb is added to the team's ManaBank. Steal alone deals no
        /// damage; Mug pairs <c>StealsMana = true</c> with <c>BaseDamage &gt; 0</c>.</summary>
        public bool StealsMana { get; }

        /// <summary>If true, the spell is a Teleport — picks an empty tile and instantly relocates
        /// the caster there. After the move, the AbilityBar checks for any new pincer the caster
        /// just completed and resolves it. Bypasses the SpellEffectDispatcher entirely (no damage,
        /// no VFX projectile — handled in AbilityBar.HandleTeleport).</summary>
        public bool IsTeleport { get; }

        public SpellDefinition(
            ManaAbility ability,
            TargetShape shape,
            TargetMode mode,
            TargetFilter filter = TargetFilter.Any,
            int radius = 0,
            string castVfx = null,
            string projectileVfx = null,
            ProjectileMotion motion = ProjectileMotion.None,
            string impactVfx = null,
            string lingerVfx = null,
            string debuffId = null,
            float baseDamage = 0f,
            float baseHeal = 0f,
            DamageType damageType = DamageType.Physical,
            float projectileSeconds = 0.5f,
            bool removesDebuffs = false,
            bool stealsMana = false,
            bool isTeleport = false)
        {
            Ability = ability;
            Shape = shape;
            Mode = mode;
            Filter = filter;
            Radius = radius;
            CastVfxName = castVfx;
            ProjectileVfxName = projectileVfx;
            Motion = motion;
            ImpactVfxName = impactVfx;
            LingerVfxName = lingerVfx;
            DebuffId = debuffId;
            BaseDamage = baseDamage;
            BaseHeal = baseHeal;
            DamageType = damageType;
            ProjectileSeconds = projectileSeconds;
            RemovesDebuffs = removesDebuffs;
            StealsMana = stealsMana;
            IsTeleport = isTeleport;
        }
    }
}
