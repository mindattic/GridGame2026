using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// TESTHOOKS - Static shims for the automated test harness (EditMode + PlayMode suites).
    /// <para>PURPOSE: Centralizes everything Assets/Tests needs to drive the game
    /// deterministically without reverse-engineering input state machines: profile isolation
    /// (never touch real saves), RNG seeding, pacing acceleration, deterministic actor
    /// placement, and read-only battle/economy state accessors. Realizes the TestHooks class
    /// proposed in Documentation/AltTester-Setup.md; complements the older AltDriver-era shims
    /// that live on GameHelper (TriggerPincerDropForHero, SequenceManagerIsExecuting).</para>
    /// <para>USAGE: Production code never calls this class. Tests call the setup members in
    /// [UnitySetUp]/[SetUp] and the matching teardown members in [TearDown] so a play session
    /// after a test run is completely unaffected.</para>
    /// <para>RELATED FILES: GameHelper.cs, FolderHelper.cs, RNG.cs,
    /// Assets/Tests/EditMode/*.cs, Assets/Tests/PlayMode/*.cs</para>
    /// </summary>
    public static class TestHooks
    {
        #region Profile isolation

        /// <summary>Redirects all profile IO under <paramref name="absolutePath"/> and reloads
        /// the profile registry from that (typically empty) root. Real saves are untouched.</summary>
        public static void UseIsolatedProfileRoot(string absolutePath)
        {
            FolderHelper.Folder.TestProfileRootOverride = absolutePath;
            ProfileHelper.Reload();
        }

        /// <summary>One-call test bootstrap: isolate profile IO under
        /// <paramref name="absoluteRoot"/>, create a throwaway profile, and ensure it has a
        /// selected CurrentSave (CreateProfile alone leaves CurrentSave null — the game
        /// normally resolves it later via EnsureCurrentSave / TitleScreen Continue).</summary>
        public static void CreateIsolatedProfile(string absoluteRoot, string profileName)
        {
            UseIsolatedProfileRoot(absoluteRoot);
            ProfileHelper.CreateProfile(profileName);
            ProfileHelper.EnsureCurrentSave(ProfileHelper.CurrentProfile);
        }

        /// <summary>Restores production profile IO and reloads the real profile registry.</summary>
        public static void ClearIsolatedProfileRoot()
        {
            FolderHelper.Folder.TestProfileRootOverride = null;
            ProfileHelper.Reload();
        }

        #endregion

        #region Determinism and pacing

        /// <summary>Seeds the game-wide RNG stream for a reproducible run.</summary>
        public static void SeedRng(int seed) => RNG.Seed(seed);

        /// <summary>Returns the RNG stream to its unseeded (time-based) production state.</summary>
        public static void UnseedRng() => RNG.Unseed();

        /// <summary>Accelerates every scaled-time delay (combat pacing flows through
        /// Wait.For / WaitForSeconds, which honor Time.timeScale — same mechanism GameManager
        /// itself uses via its gameSpeed field). Call with 1f to restore normal speed.</summary>
        public static void SetGameSpeed(float multiplier)
        {
            if (GameManager.instance != null)
                GameManager.instance.gameSpeed = multiplier;
            Time.timeScale = multiplier;
        }

        #endregion

        #region Deterministic actor placement / state

        /// <summary>Finds the first playing actor matching <paramref name="teamName"/> +
        /// <paramref name="characterClassName"/> (both parsed case-insensitively).</summary>
        public static ActorInstance FindActor(string characterClassName, string teamName)
        {
            if (!Enum.TryParse<CharacterClass>(characterClassName, ignoreCase: true, out var characterClass))
                return null;
            if (!Enum.TryParse<Team>(teamName, ignoreCase: true, out var team))
                return null;

            return g.Actors.All?.FirstOrDefault(a =>
                a != null && a.IsPlaying && a.team == team && a.characterClass == characterClass);
        }

        /// <summary>Snaps an actor (hero OR enemy — generalizes the hero-only
        /// GameHelper.TriggerPincerDropForHero shim) to tile (<paramref name="x"/>,
        /// <paramref name="y"/>) in both grid-space and world-space, without triggering any
        /// pincer scan. Returns false when no matching playing actor exists.</summary>
        public static bool PlaceActor(string characterClassName, string teamName, int x, int y)
        {
            var actor = FindActor(characterClassName, teamName);
            if (actor == null) return false;

            actor.location = new Vector2Int(x, y);
            var tile = g.TileMap?.GetTile(actor.location);
            if (tile != null) actor.Position = tile.position;
            return true;
        }

        /// <summary>Current HP of the matching actor, or float.MinValue when absent.</summary>
        public static float GetActorHp(string characterClassName, string teamName)
        {
            var actor = FindActor(characterClassName, teamName);
            return actor != null ? actor.Stats.HP : float.MinValue;
        }

        /// <summary>Sets the matching actor's HP directly (0 = dead for win/loss-path tests).
        /// Returns false when no matching playing actor exists.</summary>
        public static bool SetActorHp(string characterClassName, string teamName, float hp)
        {
            var actor = FindActor(characterClassName, teamName);
            if (actor == null) return false;
            actor.Stats.HP = Mathf.Clamp(hp, 0f, actor.Stats.MaxHP);
            return true;
        }

        /// <summary>Count of playing (active + alive) actors on the given team.</summary>
        public static int AliveCount(string teamName)
        {
            if (!Enum.TryParse<Team>(teamName, ignoreCase: true, out var team))
                return -1;
            return g.Actors.All?.Count(a => a != null && a.IsPlaying && a.team == team) ?? -1;
        }

        #endregion

        #region Economy / progression accessors

        /// <summary>The vendor-spendable currency on the live save (distinct from the lifetime
        /// coin ticker GameHelper.TotalCoins).</summary>
        public static int InventoryGold
        {
            get => ProfileHelper.CurrentProfile?.CurrentSave?.Inventory?.Gold ?? -1;
            set
            {
                var inv = ProfileHelper.CurrentProfile?.CurrentSave?.Inventory;
                if (inv != null) inv.Gold = Mathf.Max(0, value);
            }
        }

        /// <summary>Campaign progression marker on the live save (-1 = nothing cleared).</summary>
        public static int HighestClearedStageIndex
        {
            get => ProfileHelper.CurrentProfile?.CurrentSave?.Stage?.HighestClearedStageIndex ?? -1;
            set
            {
                var stage = ProfileHelper.CurrentProfile?.CurrentSave?.Stage;
                if (stage != null) stage.HighestClearedStageIndex = value;
            }
        }

        #endregion
    }
}
