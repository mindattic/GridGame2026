// TRAPANDLINETHREATTESTS — EditMode tests for US-138 (line-threat math) and US-139
// (trap state rules). Both systems keep their rules pure, so no scene is needed.

using NUnit.Framework;
using UnityEngine;
using Scripts.Data.Actor;
using Scripts.Managers;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class TrapAndLineThreatTests
    {
        [TearDown]
        public void TearDown() => TrapManager.Clear();

        // ── US-138: LineThreat ──

        [Test]
        public void Direction_picks_dominant_axis_x_on_ties()
        {
            Assert.AreEqual(new Vector2Int(1, 0), LineThreat.DirectionToward(new Vector2Int(2, 2), new Vector2Int(5, 3)));
            Assert.AreEqual(new Vector2Int(0, -1), LineThreat.DirectionToward(new Vector2Int(3, 6), new Vector2Int(4, 2)));
            Assert.AreEqual(new Vector2Int(1, 0), LineThreat.DirectionToward(new Vector2Int(2, 2), new Vector2Int(4, 4)), "Tie goes to X.");
            Assert.AreEqual(new Vector2Int(-1, 0), LineThreat.DirectionToward(new Vector2Int(5, 2), new Vector2Int(1, 2)));
        }

        [Test]
        public void Line_runs_from_beside_origin_to_the_edge()
        {
            // 6x8 board (1-based). Caster at (2,4) firing east → tiles (3..6, 4).
            var tiles = LineThreat.ComputeThreat(new Vector2Int(2, 4), new Vector2Int(5, 4), 6, 8);
            Assert.AreEqual(4, tiles.Count);
            Assert.AreEqual(new Vector2Int(3, 4), tiles[0]);
            Assert.AreEqual(new Vector2Int(6, 4), tiles[3]);
            CollectionAssert.DoesNotContain(tiles, new Vector2Int(2, 4), "The caster's own tile is never threatened.");
        }

        [Test]
        public void Edge_pinned_caster_yields_empty_line()
        {
            // Caster at the east edge firing east — no tiles.
            var tiles = LineThreat.TilesInLine(new Vector2Int(6, 4), new Vector2Int(1, 0), 6, 8);
            Assert.AreEqual(0, tiles.Count);
        }

        // ── US-139: TrapManager state ──

        [Test]
        public void Place_consume_and_clear_semantics()
        {
            var loc = new Vector2Int(3, 3);
            var trap = new TrapDefinition { DisplayName = "Venom Snare", Damage = 6f, BuffId = "poisoned" };

            Assert.IsFalse(TrapManager.HasTrapAt(loc));
            Assert.IsTrue(TrapManager.Place(loc, trap));
            Assert.IsTrue(TrapManager.HasTrapAt(loc));
            Assert.AreEqual(1, TrapManager.Count);

            Assert.IsTrue(TrapManager.TryConsume(loc, out var sprung));
            Assert.AreEqual("Venom Snare", sprung.DisplayName);
            Assert.IsFalse(TrapManager.HasTrapAt(loc), "A sprung trap is gone.");
            Assert.IsFalse(TrapManager.TryConsume(loc, out _), "No double-trigger.");

            TrapManager.Place(loc, trap);
            TrapManager.Clear();
            Assert.AreEqual(0, TrapManager.Count, "New battle wipes all traps.");
        }

        [Test]
        public void Rearming_a_tile_replaces_the_trap()
        {
            var loc = new Vector2Int(2, 2);
            TrapManager.Place(loc, new TrapDefinition { DisplayName = "Old", Damage = 1f });
            TrapManager.Place(loc, new TrapDefinition { DisplayName = "New", Damage = 9f });
            Assert.AreEqual(1, TrapManager.Count);
            TrapManager.TryConsume(loc, out var sprung);
            Assert.AreEqual("New", sprung.DisplayName);
        }
    }
}
