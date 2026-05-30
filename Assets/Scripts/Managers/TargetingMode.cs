using System;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Services;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Managers
{
    /// <summary>
    /// TARGETINGMODE - The pre-cast picker flow. Before any orbs are deducted and before the cast
    /// bar starts, the player picks targets (or auto-resolves). On confirm, the caller gets the
    /// final list of <see cref="ActorInstance"/> the spell should hit; on cancel they get nothing
    /// and orbs aren't spent.
    ///
    /// <para>Dispatches by <see cref="SpellDefinition.Mode"/>:</para>
    /// <list type="bullet">
    ///   <item><b>Auto</b> — Self / AllEnemies / AllAllies. Resolves immediately.</item>
    ///   <item><b>PickActor</b> — gold rings appear over every actor that matches the filter;
    ///   on click, the anchor = picked actor's tile; shape resolves around it.</item>
    ///   <item><b>PickTile</b> — every board tile becomes a hover target; pending shape
    ///   highlights live as the cursor moves; click to confirm.</item>
    /// </list>
    /// </summary>
    public static class TargetingMode
    {
        public static bool IsActive { get; private set; }

        /// <summary>Dismiss any live targeting overlay and reset the IsActive flag. Called at the
        /// top of <see cref="Begin"/> so a stale flag (e.g. overlay destroyed by a scene reload
        /// without firing its callbacks) can't silently block every subsequent ability click.</summary>
        public static void DismissAnyActive()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<Scripts.Canvas.TargetPickerOverlay>();
            if (existing != null) existing.NotifyCancel();
            IsActive = false;
        }

        public static void Begin(
            SpellDefinition spell,
            ActorInstance caster,
            Action<List<ActorInstance>> onConfirm,
            Action onCancel)
        {
            if (spell == null || onConfirm == null) { onCancel?.Invoke(); return; }

            // If a previous targeting session is still alive (or its flag stuck), dismiss it first
            // and start fresh. The old behavior was to bail with onCancel — which silently locked
            // every subsequent ability click whenever IsActive got stuck (e.g., overlay destroyed
            // without going through NotifyCancel).
            DismissAnyActive();

            Debug.Log($"[TargetingMode] Begin '{spell.Ability?.Name}' mode={spell.Mode} shape={spell.Shape} filter={spell.Filter}");

            switch (spell.Mode)
            {
                case TargetMode.Auto:       ResolveAuto(spell, caster, onConfirm, onCancel); return;
                case TargetMode.PickActor:  BeginActorPick(spell, caster, onConfirm, onCancel); return;
                case TargetMode.PickTile:   BeginTilePick (spell, caster, onConfirm, onCancel); return;
            }
            onCancel?.Invoke();
        }

        // ── Auto modes ──

        private static void ResolveAuto(SpellDefinition spell, ActorInstance caster,
            Action<List<ActorInstance>> onConfirm, Action onCancel)
        {
            // Self → caster only; AllEnemies / AllAllies → resolver handles via CollectActors.
            if (spell.Shape == TargetShape.Self)
            {
                if (caster == null) { onCancel?.Invoke(); return; }
                onConfirm(new List<ActorInstance> { caster });
                return;
            }
            var anchor = caster != null ? caster.location : Vector2Int.zero;
            var (w, h) = BoardSize();
            var tiles = TargetShapeResolver.Resolve(anchor, spell.Shape, spell.Radius, w, h);
            var actors = TargetShapeResolver.CollectActors(tiles, spell.Shape, spell.Filter, caster);
            if (actors.Count == 0) { onCancel?.Invoke(); return; }
            onConfirm(actors);
        }

        // ── Pick-an-actor ──

        private static void BeginActorPick(SpellDefinition spell, ActorInstance caster,
            Action<List<ActorInstance>> onConfirm, Action onCancel)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { onCancel?.Invoke(); return; }

            // Eligible candidates = playing actors matching the filter.
            var candidates = new List<ActorInstance>();
            var all = g.Actors.All;
            if (all != null)
                foreach (var a in all)
                    if (a != null && a.IsPlaying && TargetShapeResolver.Matches(a, spell.Filter, caster))
                        candidates.Add(a);

            if (candidates.Count == 0) { onCancel?.Invoke(); return; }

            IsActive = true;
            Scripts.Factories.TargetPickerOverlayFactory.CreateActorPicker(
                canvas.transform, spell, caster, candidates,
                onPickedActor: picked =>
                {
                    IsActive = false;
                    var (w, h) = BoardSize();
                    var tiles = TargetShapeResolver.Resolve(picked.location, spell.Shape, spell.Radius, w, h);
                    var actors = TargetShapeResolver.CollectActors(tiles, spell.Shape, spell.Filter, caster);
                    if (actors.Count == 0) actors.Add(picked); // ensure at least the picked actor is hit
                    onConfirm(actors);
                },
                onCancelled: () =>
                {
                    IsActive = false;
                    onCancel?.Invoke();
                });
        }

        // ── Pick-a-tile ──

        private static void BeginTilePick(SpellDefinition spell, ActorInstance caster,
            Action<List<ActorInstance>> onConfirm, Action onCancel)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { onCancel?.Invoke(); return; }

            var (w, h) = BoardSize();
            IsActive = true;
            Scripts.Factories.TargetPickerOverlayFactory.CreateTilePicker(
                canvas.transform, spell, caster, w, h,
                onPickedTile: anchor =>
                {
                    IsActive = false;
                    var tiles = TargetShapeResolver.Resolve(anchor, spell.Shape, spell.Radius, w, h);
                    var actors = TargetShapeResolver.CollectActors(tiles, spell.Shape, spell.Filter, caster);
                    onConfirm(actors);
                },
                onCancelled: () =>
                {
                    IsActive = false;
                    onCancel?.Invoke();
                });
        }

        private static (int w, int h) BoardSize()
        {
            var board = g.Board;
            if (board == null) return (6, 8);
            return (board.columnCount, board.rowCount);
        }
    }
}
