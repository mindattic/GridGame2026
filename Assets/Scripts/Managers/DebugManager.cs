using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Sequences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
public class DebugManager : MonoBehaviour
{
    //DEBUG: No gaurentee these values exist, define and use inside tests...
    ActorInstance hero1 => g.Actors.Heroes.Skip(0).Take(1).First();
    ActorInstance hero2 => g.Actors.Heroes.Skip(1).Take(1).First();
    ActorInstance hero3 => g.Actors.Heroes.Skip(2).Take(1).First();
    ActorInstance hero4 => g.Actors.Heroes.Skip(3).Take(1).First();

    //Fields
    [SerializeField] private TMP_Dropdown Dropdown;
    public bool showActorNameTag = false;
    public bool showActorFrame = false;
    public bool showTutorials = false;
    public bool isHeroInvincible = false;
    public bool isEnemyInvincible = false;
    public bool isTimerInfinite = false;
    public bool isEnemyStunned = false;

    // Small helper to spawn a VFX for available heroes (guards null heroes)
    /// <summary>Creates the visual effect.</summary>
    private void SpawnVisualEffect(VisualEffectAsset vfx)
    {
        var h1 = hero1;
        var h2 = hero2;
        if (h1 != null)
            g.VisualEffectManager.Spawn(vfx, h1.Position);
        if (h2 != null)
            g.VisualEffectManager.Spawn(vfx, h2.Position);
    }

    // Gain a small, random chunk of XP for a random hero (wired in DebugWindow DebugOptions -> AddExperience)
    /// <summary>Add experience.</summary>
    public void AddExperience()
    {
        var hero = RNG.Hero;
        if (hero == null) return;

        var nextLevel = ExperienceHelper.NextLevel(hero.Stats.Level);
        var xp = Mathf.Max(1, (nextLevel * RNG.Float(0.25f, 0.33f)).ToInt());
        ExperienceHelper.Gain(hero, xp);

        g.CombatTextManager.Spawn($"+{xp} XP", hero.Position, "Heal");
        g.AudioManager.Play("Click");
    }

    // New: Gain exact XP for the selected/random hero (utility)
    /// <summary>Add experience.</summary>
    public void AddExperience(int amount)
    {
        var hero = RNG.Hero;
        if (hero == null || amount <= 0) return;

        ExperienceHelper.Gain(hero, amount);
        g.CombatTextManager.Spawn($"+{amount} XP", hero.Position, "Heal");
        g.AudioManager.Play("Click");
    }

    // TODO: Should be controlled by CoinManager
    // Wired in DebugWindow DebugOptions -> SpawnCoins
    //public void SpawnCoins()
    //{
    //    var target = hero1 ?? RNG.Hero;
    //    if (target == null) return;

    //    // Default burst of 10
    //    SpawnCoins(10);
    //}

    // New: spawn an exact number of coins at hero1 (or a random hero) using CoinManager
    /// <summary>Creates the coins.</summary>
    public void SpawnCoins()
    {
        var target = hero1 ?? RNG.Hero;
        var amount = RNG.Int(10, 20);
        if (target == null || amount <= 0) return;

        // Optional VFX gate, then spawn coins
        //if (VfxLibrary.VisualEffects.TryGetValue("YellowHit", out var vfx))
        //    g.VfxManager.Spawn(vfx, target.Position);

        g.CoinManager.SpawnBurst(target.Position, amount);
    }

    /// <summary>
    /// Lays out a single horizontal pincer lane for quick debugging.
    /// Spawns six slimes, destroys all other enemies, teleports up to two heroes and the six slimes
    /// to fixed positions, and moves all other playing actors to random unoccupied tiles.
    /// By keeping the newly spawned slimes alive while removing other enemies,
    /// the wave does not advance and the stage does not restart.
    /// </summary>
    public void ArrangeSingleCombo()
    {
        // Spawn six slimes for this debug layout and keep references
        var keptSlimes = new List<ActorInstance>(6);
        for (int i = 0; i < 6; i++)
            keptSlimes.Add(SpawnSlime());

        // Destroy all existing enemies except the six slimes we just spawned
        foreach (var enemy in g.Actors.Enemies.ToArray())
        {
            if (enemy == null) continue;
            if (keptSlimes.Contains(enemy)) continue;

            UnityEngine.Object.Destroy(enemy.gameObject);
        }

        // Horizontal lane positions
        hero1?.Teleport(new Vector2Int(3, 1));
        keptSlimes[0]?.Teleport(new Vector2Int(3, 2));
        keptSlimes[1]?.Teleport(new Vector2Int(3, 3));
        keptSlimes[2]?.Teleport(new Vector2Int(3, 4));
        keptSlimes[3]?.Teleport(new Vector2Int(3, 5));
        keptSlimes[4]?.Teleport(new Vector2Int(3, 6));
        keptSlimes[5]?.Teleport(new Vector2Int(3, 7));
        hero2?.Teleport(new Vector2Int(3, 8));

        // Build alignment group
        var group = new List<ActorInstance> { hero1, hero2 };
        group.AddRange(keptSlimes.Where(s => s != null));

        // Move every other playing actor to an unoccupied location
        foreach (var actor in g.Actors.All)
        {
            if (actor == null) continue;
            if (!actor.IsPlaying) continue;
            if (group.Contains(actor)) continue;

            actor.Teleport(RNG.UnoccupiedLocation);
        }
    }


    /// <summary>Arrange double combo.</summary>
    public void ArrangeDoubleCombo()
    {
        // Spawn either slimes used by this debug layout
        for (int i = 0; i < 8; i++)
            SpawnSlime();

        // Collect up to 9 enemies, some may be missing
        var enemies = g.Actors.Enemies.Take(8).ToArray();

        // Utility to teleport only when the actor exists
        void SafeTeleport(ActorInstance a, Vector2Int pos)
        {
            if (a != null) a.Teleport(pos);
        }

        // Heroes may be assigned in SpawnSlime; guard in case any are missing
        SafeTeleport(hero1, new Vector2Int(1, 1));
        SafeTeleport(enemies[0], new Vector2Int(1, 2));
        SafeTeleport(enemies[1], new Vector2Int(1, 3));
        SafeTeleport(enemies[2], new Vector2Int(1, 4));
        SafeTeleport(enemies[3], new Vector2Int(1, 5));
        SafeTeleport(hero2, new Vector2Int(1, 6));
        SafeTeleport(enemies[4], new Vector2Int(2, 6));
        SafeTeleport(enemies[5], new Vector2Int(3, 6));
        SafeTeleport(enemies[6], new Vector2Int(4, 6));
        SafeTeleport(enemies[7], new Vector2Int(5, 6));

        // Build the alignment group without nulls
        var group = new List<ActorInstance> { hero1, hero2, hero3, hero4 };
        group.AddRange(enemies.Where(e => e != null));
        group = group.Where(x => x != null).ToList();

        // Move every other playing actor to an unoccupied location
        foreach (var actor in g.Actors.All)
        {
            if (actor == null) continue;
            if (!actor.IsPlaying) continue;
            if (group.Contains(actor)) continue;

            actor.Teleport(RNG.UnoccupiedLocation);
        }
    }

    /// <summary>Arrange triple combo.</summary>
    public void ArrangeTripleCombo()
    {
        // Spawn nine slimes used by this debug layout
        for (int i = 0; i < 9; i++)
            SpawnSlime();

        // Collect up to 9 enemies, some may be missing
        var enemies = g.Actors.Enemies.Take(9).ToArray();

        // Utility to teleport only when the actor exists
        void SafeTeleport(ActorInstance a, Vector2Int pos)
        {
            if (a != null) a.Teleport(pos);
        }

        // Heroes may be assigned in SpawnSlime; guard in case any are missing
        SafeTeleport(hero1, new Vector2Int(1, 1));
        SafeTeleport(enemies[0], new Vector2Int(1, 2));
        SafeTeleport(enemies[1], new Vector2Int(1, 3));
        SafeTeleport(hero2, new Vector2Int(1, 4));
        SafeTeleport(enemies[2], new Vector2Int(2, 4));
        SafeTeleport(enemies[3], new Vector2Int(3, 4));
        SafeTeleport(enemies[4], new Vector2Int(4, 4));
        SafeTeleport(enemies[5], new Vector2Int(5, 4));
        SafeTeleport(hero3, new Vector2Int(6, 4));
        SafeTeleport(enemies[6], new Vector2Int(6, 5));
        SafeTeleport(enemies[7], new Vector2Int(6, 6));
        SafeTeleport(enemies[8], new Vector2Int(6, 7));
        SafeTeleport(hero4, new Vector2Int(6, 8));

        // Build the alignment group without nulls
        var group = new List<ActorInstance> { hero1, hero2, hero3, hero4 };
        group.AddRange(enemies.Where(e => e != null));
        group = group.Where(x => x != null).ToList();

        // Move every other playing actor to an unoccupied location
        foreach (var actor in g.Actors.All)
        {
            if (actor == null) continue;
            if (!actor.IsPlaying) continue;
            if (group.Contains(actor)) continue;

            actor.Teleport(RNG.UnoccupiedLocation);
        }
    }


    /// <summary>
    /// Arranges a surround combo for debug testing.
    /// Spawns a slime in the center and positions up to four heroes
    /// around it (above, right, below, left).
    /// </summary>
    public void ArrangeSurroundCombo()
    {
        var center = new Vector2Int(3, 3);
        var above = new Vector2Int(3, 2);
        var right = new Vector2Int(4, 3);
        var below = new Vector2Int(3, 4);
        var left = new Vector2Int(2, 3);

        // Ensure at least one slime exists
        SpawnSlime();

        var slime = g.Actors.Enemies.FirstOrDefault(x => x != null && x.characterClass == CharacterClass.Slime00);
        if (slime == null)
        {
            Debug.LogError("ArrangeSurroundCombo: No slime found to place in center.");
            return;
        }

        // Safe teleport helper
        void SafeTeleport(ActorInstance actor, Vector2Int pos)
        {
            if (actor != null) actor.Teleport(pos);
        }

        // Place slime and heroes
        SafeTeleport(slime, center);
        SafeTeleport(hero1, above);
        SafeTeleport(hero2, right);
        SafeTeleport(hero3, below);
        SafeTeleport(hero4, left);
    }



    /// <summary>Bump.</summary>
    public void Bump()
    {
        var hero = RNG.Hero;
        hero.Teleport(RNG.UnoccupiedLocation);

        // 3) try to find an attacker already adjacent
        var enemy = Geometry.GetAdjacentOpponent(hero);
        if (!enemy.Exists())
            enemy = RNG.Enemy;

        var location = Geometry.GetClosestUnoccupiedAdjacentTileByLocation(hero.location).location;
        if (!location.Exists())
            location = Geometry.GetAdjacentLocationInDirection(hero.location, RNG.AdjacentDirection);

        enemy.Teleport(location);
        hero.Animation.Bump(enemy);
    }

    /// <summary>Dodge.</summary>
    public void Dodge()
    {
        hero1.Animation.Dodge();
    }

    /// <summary>Kill enemies.</summary>
    public void KillEnemies()
    {
        var actors = g.Actors.Enemies.Where(x => x != null && x.IsPlaying).ToList();
        StartCoroutine(KillRoutine(actors));
    }

    /// <summary>Kill heroes.</summary>
    public void KillHeroes()
    {
        var actors = g.Actors.Heroes.Where(x => x != null && x.IsPlaying).ToList();
        StartCoroutine(KillRoutine(actors));
    }

    /// <summary>Coroutine that executes the kill sequence.</summary>
    private IEnumerator KillRoutine(List<ActorInstance> playingActors)
    {
        // Capture the currently playing actors so we only wait on these
        if (playingActors.Count < 1)
            yield break;

        // Apply lethal damage
        foreach (var actor in playingActors)
        {
            var attacker = actor.IsHero ? RNG.Enemy : RNG.Hero;
            var attackResult = new AttackResult(attacker, actor, 9999, HitOutcome.Critical);
            actor.Damage(attackResult);
        }

        // Let the centralized death processor finish deaths (spawns coins, notifies stage, etc.)
        yield return DeathHelper.ProcessRoutine();

        // Ensure they have transitioned to dead/inactive
        yield return new WaitUntil(() => playingActors.All(e => e == null || e.IsDead));

        //DEBUG IS this the best way to trigger the steps leading to death?
        // Nudge stage flow in case some deaths happened outside normal flow
        g.StageManager.OnActorDeath();
    }

    /// <summary>Goto post battle screen.</summary>
    public void GotoPostBattleScreen()
    {
        // Seed XP for participating heroes and route to PostBattleScreen for accumulation
        var save = ProfileHelper.CurrentProfile?.CurrentSave;

        // Prefer the party list from the save; fall back to active heroes in scene
        var participants = (save?.Party?.Members?.Select(m => m.CharacterClass)
                                .Where(ch => ch != CharacterClass.None)
                                .ToList())
                           ?? new List<CharacterClass>();

        if (participants.Count == 0)
        {
            participants = g.Actors.Heroes
                .Where(h => h != null && h.characterClass != CharacterClass.None)
                .Select(h => h.characterClass)
                .Distinct()
                .ToList();
        }

        // Start a new XP session (optional if already started by StageManager)
        ExperienceTracker.StartSession(participants);

        // Grant a small amount of XP to each participant
        foreach (var ch in participants)
        {
            // Example debug amount: 100 +/- 25
            int amount = RNG.Int(75, 125);
            ExperienceTracker.AddParticipant(ch);
            ExperienceTracker.AddXP(ch, amount);
        }

        // Jump to PostBattle screen so the UI can display and then apply gains
        scene.Fade.ToPostBattleScreen();
    }

    /// <summary>Portrait2 d slide in.</summary>
    public void Portrait2DSlideIn()
    {
        var hero = RNG.Hero;
        var direction = RNG.AdjacentDirection;
        g.PortraitManager.SlideIn2D(hero, direction);
    }

    /// <summary>Portrait3 d slide in.</summary>
    public void Portrait3DSlideIn()
    {
        var hero = RNG.Hero;
        var direction = RNG.AdjacentDirection;
        g.PortraitManager.SlideIn3D(hero, direction);
    }

    /// <summary>Portrait pop in.</summary>
    public void PortraitPopIn()
    {
        var hero = RNG.Hero;
        g.SequenceManager.Add(new PortraitPopInSequence(hero));
        g.SequenceManager.Add(new PortraitPopOutSequence(hero));
        StartCoroutine(g.SequenceManager.ExecuteRoutine());
    }

    /// <summary>Creates the damage text.</summary>
    public void SpawnDamageText()
    {
        var hero = RNG.Hero;
        var text = $"{RNG.Int(1, 100)}";
        g.CombatTextManager.Spawn(text, hero.Position, "Damage");
    }

    /// <summary>Creates the heal text.</summary>
    public void SpawnHealText()
    {
        var hero = RNG.Hero;
        var text = $"{RNG.Int(1, 100)}";
        g.CombatTextManager.Spawn(text, hero.Position, "Heal");
    }


    /// <summary>Shake.</summary>
    public void Shake()
    {
        var intensity = RNG.ShakeIntensityLevel();
        var duration = RNG.Float(Interval.HalfSecond, Interval.TwoSeconds);
        hero1.Animation.Shake(intensity, duration);
    }

    /// <summary>Spin.</summary>
    public void Spin()
    {
        hero1.Animation.Spin360();
    }

    /// <summary>Creates the support lines.</summary>
    public void SpawnSupportLines()
    {
        foreach (var attacker in g.Actors.Heroes)
        {
            var supporters = g.PincerAttackManager.FindSupporters(attacker);
            foreach (var supporter in supporters)
            {
                var newest = g.SupportLineManager.Spawn(supporter, attacker);
                newest.isStatic = true;
            }
        }
    }

    /// <summary>Creates the synergy lines.</summary>
    public void SpawnSynergyLines()
    {
        foreach (var attacker in g.Actors.Heroes)
        {
            var supporters = g.PincerAttackManager.FindSupporters(attacker);
            foreach (var supporter in supporters)
            {
                g.SynergyLineManager.Spawn(supporter, attacker);
            }
        }
    }

    /// <summary>Creates the tooltip1.</summary>
    public void SpawnTooltip1()
    {
        var tt = new TooltipSettings()
        {
            message = "Tap here to confirm",
            target = hero1.transform,
            placement = TooltipPlacement.Top,
            useFade = true,
            useTypewriter = true,
            autoDestroy = true,
            followPointer = false,
            autoDestroyDelay = 2.5f,
        };

        Tooltip.Show(tt);
    }

    /// <summary>Creates the tooltip2.</summary>
    public void SpawnTooltip2()
    {
        var tt = new TooltipSettings()
        {
            message = "Tap here to confirm",
            target = hero1.transform,
            placement = TooltipPlacement.Top,
            useFade = false,
            useTypewriter = false,
            autoDestroy = true,
            followPointer = false,
            autoDestroyDelay = 2.5f,
        };

        Tooltip.Show(tt);
    }

    /// <summary>Trigger enemy move attack.</summary>
    public void TriggerEnemyMoveAttack()
    {
        var attackingEnemies = g.Actors.Enemies.Where(x => x.IsPlaying).ToList();
        attackingEnemies.ForEach(x => x.SetReady());

        if (g.TurnManager.IsHeroTurn)
            g.TurnManager.NextTurn();           // switch to attacker turn

    }

    /// <summary>Trigger enemy attack.</summary>
    public void TriggerEnemyAttack()
    {
        if (g.TurnManager.IsHeroTurn)
            g.TurnManager.NextTurn();           // switch to attacker turn
    }


    /// <summary>Title test.</summary>
    public void TitleTest()
    {
        var text = DateTime.UtcNow.Ticks.ToString();

    }

    /// <summary>Tutorial test.</summary>
    public void TutorialTest()
    {
        var tutorial = TutorialLibrary.Tutorials["Tutorial1"];
        g.TutorialPopup.Load(tutorial);
    }

    /// <summary>Creates the slime.</summary>
    public ActorInstance SpawnSlime()
    {
        return g.StageManager.AddEnemy(CharacterClass.Slime00);
    }

    /// <summary>Creates the bat.</summary>
    public ActorInstance SpawnBat()
    {
        return g.StageManager.AddEnemy(CharacterClass.Bat00);
    }

    /// <summary>Creates the scorpion.</summary>
    public ActorInstance SpawnScorpion()
    {
        return g.StageManager.AddEnemy(CharacterClass.Scorpion);
    }

    /// <summary>Creates the yeti.</summary>
    public ActorInstance SpawnYeti()
    {
        return g.StageManager.AddEnemy(CharacterClass.Yeti);
    }

    /// <summary>Creates the soldier.</summary>
    public ActorInstance SpawnSoldier()
    {
        return SpawnRandomByGroup(ActorTag.Soldier | ActorTag.Soldier);
    }


    /// <summary>Creates the random enemy.</summary>
    public void SpawnRandomEnemy()
    {
        var r = RNG.Int(1, 10);
        if (r <= 7) SpawnSlime();
        else if (r == 8) SpawnBat();
        else if (r == 9) SpawnScorpion();
        else if (r == 10) SpawnYeti();
    }

    /// <summary>
    /// Spawns a random enemy whose ActorData matches all requested groups.
    /// Example: SpawnRandomByGroup(ActorGroup.Soldier | ActorGroup.Elite)
    /// </summary>
    public ActorInstance SpawnRandomByGroup(ActorTag requiredGroups)
    {
        var actorData = ActorLibrary.Actors
            .Where(x => x.Value.InGroups(requiredGroups)).ToList()
            .Shuffle().FirstOrDefault().Value;

        if (actorData == null) return null;

        return g.StageManager.AddEnemy(actorData.CharacterClass);
    }


    /// <summary>Fireball.</summary>
    public void Fireball()
    {
        var startPosition = hero1.Position;
        var target = hero2;

        // Use ProjectileManager helper which sets MotionStyle and pacing
        g.ProjectileManager.EnqueueFireball(startPosition, target);
        g.SequenceManager.Execute();
    }

    /// <summary>Heal.</summary>
    public void Heal()
    {
        var source = hero1.Position;
        var target = hero2;

        // Use ProjectileManager helper which sets MotionStyle and pacing
        g.ProjectileManager.EnqueueHeal(source, target);
        g.SequenceManager.Execute();
    }

    /// <summary>Homing spiral.</summary>
    public void HomingSpiral()
    {
        var source = hero1.Position;
        var target = hero2;

        // Use ProjectileManager helper which sets MotionStyle and pacing
        g.ProjectileManager.EnqueueHomingSpiral(source, target);
        g.SequenceManager.Execute();
    }


    /// <summary>Randomize background.</summary>
    public void RandomizeBackground()
    {
        g.Background.Randomize();
    }


    /// <summary>Trigger next turn.</summary>
    public void TriggerNextTurn()
    {
        g.TurnManager.NextTurn();
    }

    /// <summary>Vfx test_blue slash1.</summary>
    public void VFXTest_BlueSlash1()
    {
        var targetEnemy = g.Actors.Enemies.FirstOrDefault();
        if (targetEnemy == null)
            return;

        var attackResult = new AttackResult(hero1, targetEnemy, 3, HitOutcome.Normal);
        if (attackResult.HitType == HitOutcome.Critical)
        {
            var crit = VisualEffectLibrary.VisualEffects["YellowHit"];
            g.VisualEffectManager.Spawn(crit, hero1.Position);
            attackResult.Damage = (int)Math.Round(attackResult.Damage * 1.5f);
        }

        var vfx = VisualEffectLibrary.VisualEffects["BlueSlash1"];
        g.VisualEffectManager.Spawn(vfx, hero1.Position, hero1.DamageRoutine(attackResult));
    }

    /// <summary>Vfx test_blue slash2.</summary>
    public void VFXTest_BlueSlash2()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueSlash2"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue slash3.</summary>
    public void VFXTest_BlueSlash3()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueSlash3"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue slash4.</summary>
    public void VFXTest_BlueSlash4()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueSlash4"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue sword.</summary>
    public void VFXTest_BlueSword()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueSword"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue sword4 x.</summary>
    public void VFXTest_BlueSword4X()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueSword4X"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blood claw.</summary>
    public void VFXTest_BloodClaw()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BloodClaw"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_level up.</summary>
    public void VFXTest_LevelUp()
    {
        var vfx = VisualEffectLibrary.VisualEffects["LevelUp"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_yellow hit.</summary>
    public void VFXTest_YellowHit()
    {
        var vfx = VisualEffectLibrary.VisualEffects["YellowHit"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_double claw.</summary>
    public void VFXTest_DoubleClaw()
    {
        var vfx = VisualEffectLibrary.VisualEffects["DoubleClaw"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_lightning explosion.</summary>
    public void VFXTest_LightningExplosion()
    {
        var vfx = VisualEffectLibrary.VisualEffects["LightningExplosion"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_buff life.</summary>
    public void VFXTest_BuffLife()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BuffLife"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_rotary knife.</summary>
    public void VFXTest_RotaryKnife()
    {
        var vfx = VisualEffectLibrary.VisualEffects["RotaryKnife"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_air slash.</summary>
    public void VFXTest_AirSlash()
    {
        var vfx = VisualEffectLibrary.VisualEffects["AirSlash"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_fire rain.</summary>
    public void VFXTest_FireRain()
    {
        var vfx = VisualEffectLibrary.VisualEffects["FireRain"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_ray blast.</summary>
    public void VFXTest_RayBlast()
    {
        var vfx = VisualEffectLibrary.VisualEffects["RayBlast"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_lightning strike.</summary>
    public void VFXTest_LightningStrike()
    {
        var vfx = VisualEffectLibrary.VisualEffects["LightningStrike"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_puffy explosion.</summary>
    public void VFXTest_PuffyExplosion()
    {
        var vfx = VisualEffectLibrary.VisualEffects["PuffyExplosion"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_red slash2 x.</summary>
    public void VFXTest_RedSlash2X()
    {
        var vfx = VisualEffectLibrary.VisualEffects["RedSlash2X"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_god rays.</summary>
    public void VFXTest_GodRays()
    {
        var vfx = VisualEffectLibrary.VisualEffects["GodRays"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_acid splash.</summary>
    public void VFXTest_AcidSplash()
    {
        var vfx = VisualEffectLibrary.VisualEffects["AcidSplash"];
        SpawnVisualEffect(vfx);
    }
    /// <summary>Vfx test_green buff.</summary>
    public void VFXTest_GreenBuff()
    {
        var vfx = VisualEffectLibrary.VisualEffects["GreenBuff"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_gold buff.</summary>
    public void VFXTest_GoldBuff()
    {
        var vfx = VisualEffectLibrary.VisualEffects["GoldBuff"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_hex shield.</summary>
    public void VFXTest_HexShield()
    {
        var vfx = VisualEffectLibrary.VisualEffects["HexShield"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_toxic cloud.</summary>
    public void VFXTest_ToxicCloud()
    {
        var vfx = VisualEffectLibrary.VisualEffects["ToxicCloud"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_orange slash.</summary>
    public void VFXTest_OrangeSlash()
    {
        var vfx = VisualEffectLibrary.VisualEffects["OrangeSlash"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_moon feather.</summary>
    public void VFXTest_MoonFeather()
    {
        var vfx = VisualEffectLibrary.VisualEffects["MoonFeather"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_pink spark.</summary>
    public void VFXTest_PinkSpark()
    {
        var vfx = VisualEffectLibrary.VisualEffects["PinkSpark"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue yellow sword.</summary>
    public void VFXTest_BlueYellowSword()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueYellowSword"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_blue yellow sword3 x.</summary>
    public void VFXTest_BlueYellowSword3X()
    {
        var vfx = VisualEffectLibrary.VisualEffects["BlueYellowSword3X"];
        SpawnVisualEffect(vfx);
    }

    /// <summary>Vfx test_red sword.</summary>
    public void VFXTest_RedSword()
    {
        var vfx = VisualEffectLibrary.VisualEffects["RedSword"];
        SpawnVisualEffect(vfx);
    }



    /// <summary>Vfx test_tech sword.</summary>
    public void VFXTest_TechSword()
    {
        var vfx = VisualEffectLibrary.VisualEffects["TechSword"];
        SpawnVisualEffect(vfx);
    }

}

}
