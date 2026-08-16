// PINCERDETECTORTESTS — EditMode pure-logic tests for Scripts.Services.PincerDetector.
// PincerDetector takes the actor list as a parameter (no g. switchboard), so these tests
// build bare ActorInstance components without a scene. AddComponent does NOT invoke Awake
// in EditMode (ActorInstance has no [ExecuteAlways]), so the heavy render/animation
// initialization never runs — we only set the fields the detector reads: team, location,
// Stats.HP (IsPlaying = active + HP > 0), and the default 1x1 Footprint.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class PincerDetectorTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private ActorInstance Spawn(Team team, int x, int y)
        {
            var go = new GameObject($"{team}@{x},{y}");
            spawned.Add(go);
            var actor = go.AddComponent<ActorInstance>();
            actor.team = team;
            actor.location = new Vector2Int(x, y);
            actor.Stats.MaxHP = 10f;
            actor.Stats.HP = 10f;
            return actor;
        }

        [Test]
        public void Detects_horizontal_pincer()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 3),
                Spawn(Team.Enemy, 2, 3),
                Spawn(Team.Hero, 3, 3),
            };

            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(1, result.pair.Count, "Exactly one pincer expected.");
            Assert.AreEqual(1, result.pair[0].opponents.Count);
            Assert.AreEqual(Team.Enemy, result.pair[0].opponents[0].team);
        }

        [Test]
        public void Detects_vertical_pincer_with_two_enemies_in_line()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 2, 1),
                Spawn(Team.Enemy, 2, 2),
                Spawn(Team.Enemy, 2, 3),
                Spawn(Team.Hero, 2, 4),
            };

            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(1, result.pair.Count);
            Assert.AreEqual(2, result.pair[0].opponents.Count, "Both enemies in the line are flanked.");
        }

        [Test]
        public void Gap_in_line_is_not_a_pincer()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 3),
                Spawn(Team.Enemy, 2, 3),
                // (3,3) empty — gap breaks the line.
                Spawn(Team.Hero, 4, 3),
            };

            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(0, result.pair.Count, "A gap in the line must break the pincer.");
        }

        [Test]
        public void Ally_in_line_is_not_a_pincer()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 3),
                Spawn(Team.Enemy, 2, 3),
                Spawn(Team.Hero, 3, 3),
                Spawn(Team.Hero, 4, 3),
            };

            // Heroes at 1 and 4 sandwich enemy+ally — invalid. Heroes at 1 and 3 sandwich
            // just the enemy — valid. Exactly one pincer total.
            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(1, result.pair.Count, "Only the ally-free line forms a pincer.");
        }

        [Test]
        public void Adjacent_heroes_are_not_a_pincer()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 3),
                Spawn(Team.Hero, 2, 3),
                Spawn(Team.Enemy, 4, 6),
            };

            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(0, result.pair.Count);
        }

        [Test]
        public void Diagonal_alignment_is_not_a_pincer()
        {
            var actors = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 1),
                Spawn(Team.Enemy, 2, 2),
                Spawn(Team.Hero, 3, 3),
            };

            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(0, result.pair.Count, "Diagonal pincers do not exist (GG-LAW-2).");
        }

        [Test]
        public void Dead_enemy_does_not_form_a_pincer_line()
        {
            var heroes = new List<ActorInstance>
            {
                Spawn(Team.Hero, 1, 3),
                Spawn(Team.Hero, 3, 3),
            };
            var enemy = Spawn(Team.Enemy, 2, 3);
            enemy.Stats.HP = 0f; // dead — IsPlaying false — tile is effectively a gap

            var actors = new List<ActorInstance>(heroes) { enemy };
            var result = PincerDetector.Detect(actors, Team.Hero, null);

            Assert.AreEqual(0, result.pair.Count);
        }
    }
}
