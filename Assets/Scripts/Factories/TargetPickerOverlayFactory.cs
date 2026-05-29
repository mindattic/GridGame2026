using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Scripts.Canvas;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Services;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// TARGETPICKEROVERLAYFACTORY - Builds the runtime UI for either an actor-pick session
    /// (<see cref="CreateActorPicker"/>) or a tile-pick session with live shape preview
    /// (<see cref="CreateTilePicker"/>).
    ///
    /// <para>Both sessions: translucent veil over the screen (click = cancel), per-candidate
    /// clickable highlight, ESC / right-click cancels (handled in <see cref="TargetPickerOverlay"/>).</para>
    /// </summary>
    public static class TargetPickerOverlayFactory
    {
        public const float RingDiameter = 80f;
        public const float RingYOffset  = 0.55f;
        public const float TileCellSize = 80f;
        public const float TileHighlightAlpha = 0.45f;

        // ── Actor-pick session (rings above eligible actors) ──

        public static TargetPickerOverlay CreateActorPicker(
            Transform canvas,
            SpellDefinition spell,
            ActorInstance caster,
            List<ActorInstance> candidates,
            Action<ActorInstance> onPickedActor,
            Action onCancelled)
        {
            var (root, overlay) = BuildRoot(canvas, onCancelled);

            foreach (var actor in candidates)
            {
                if (actor == null) continue;
                BuildActorRing(root.transform, actor, picked =>
                {
                    onPickedActor?.Invoke(picked);
                    overlay.NotifyPick();
                });
            }

            // For AOE shapes anchored to actors, show a live preview when hovering each ring.
            // (Preview also kicks in once on the first candidate so the player can see scope.)
            if (spell.Shape != TargetShape.SingleActor && candidates.Count > 0)
            {
                int boardW = Scripts.Helpers.GameHelper.Board?.columnCount ?? 6;
                int boardH = Scripts.Helpers.GameHelper.Board?.rowCount ?? 8;
                var preview = BuildPreviewLayer(root.transform);
                // Wire hover-show: when a ring is hovered, preview resolves around that actor.
                int rh = root.transform.childCount;
                for (int i = 1; i < rh; i++) // skip veil
                {
                    var ringGO = root.transform.GetChild(i).gameObject;
                    if (i - 1 >= candidates.Count) break;
                    var captured = candidates[i - 1];
                    AddHoverListeners(ringGO,
                        onEnter: () => preview.ShowAt(captured.location, spell.Shape, spell.Radius, boardW, boardH),
                        onExit:  () => preview.HideAll());
                }
            }

            return overlay;
        }

        // ── Tile-pick session (full grid, hover preview) ──

        public static TargetPickerOverlay CreateTilePicker(
            Transform canvas,
            SpellDefinition spell,
            ActorInstance caster,
            int boardWidth,
            int boardHeight,
            Action<Vector2Int> onPickedTile,
            Action onCancelled)
        {
            var (root, overlay) = BuildRoot(canvas, onCancelled);
            var preview = BuildPreviewLayer(root.transform);

            for (int x = 0; x < boardWidth; x++)
                for (int y = 0; y < boardHeight; y++)
                {
                    var anchor = new Vector2Int(x, y);
                    BuildTileCell(root.transform, anchor,
                        onHover: () => preview.ShowAt(anchor, spell.Shape, spell.Radius, boardWidth, boardHeight),
                        onUnhover: () => preview.HideAll(),
                        onClick: () =>
                        {
                            onPickedTile?.Invoke(anchor);
                            overlay.NotifyPick();
                        });
                }

            return overlay;
        }

        // ── Shared root + veil ──

        private static (GameObject root, TargetPickerOverlay overlay) BuildRoot(Transform canvas, Action onCancelled)
        {
            var rootGO = new GameObject("TargetPickerOverlay",
                typeof(RectTransform), typeof(TargetPickerOverlay));
            rootGO.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)rootGO.transform;
            rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var overlay = rootGO.GetComponent<TargetPickerOverlay>();
            overlay.OnCancelled += () => onCancelled?.Invoke();

            // Veil — catches background clicks → cancel.
            var veilGO = new GameObject("Veil", typeof(RectTransform), typeof(Image), typeof(Button));
            veilGO.layer = rootGO.layer;
            var vrt = (RectTransform)veilGO.transform;
            vrt.SetParent(rootGO.transform, false);
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            veilGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            veilGO.GetComponent<Button>().onClick.AddListener(() => overlay.NotifyCancel());

            return (rootGO, overlay);
        }

        private static void BuildActorRing(Transform parent, ActorInstance actor, Action<ActorInstance> onPicked)
        {
            var ringGO = new GameObject($"TargetRing_{actor.name}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(WorldFollow), typeof(TargetRingPulse));
            ringGO.layer = parent.gameObject.layer;
            var rrt = (RectTransform)ringGO.transform;
            rrt.SetParent(parent, false);
            rrt.sizeDelta = new Vector2(RingDiameter, RingDiameter);
            ringGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 0.85f);
            ringGO.GetComponent<WorldFollow>().Bind(actor.transform, new Vector3(0f, RingYOffset, 0f));

            var captured = actor;
            ringGO.GetComponent<Button>().onClick.AddListener(() => onPicked?.Invoke(captured));
        }

        private static void BuildTileCell(Transform parent, Vector2Int tile, Action onHover, Action onUnhover, Action onClick)
        {
            var cellGO = new GameObject($"TileCell_{tile.x}_{tile.y}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(WorldFollowFromTile), typeof(TilePickerCell));
            cellGO.layer = parent.gameObject.layer;
            var crt = (RectTransform)cellGO.transform;
            crt.SetParent(parent, false);
            crt.sizeDelta = new Vector2(TileCellSize, TileCellSize);

            // Invisible but raycastable — so it can hover/click but the player sees the preview, not the cell.
            var img = cellGO.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);

            cellGO.GetComponent<WorldFollowFromTile>().BindTile(tile);
            cellGO.GetComponent<TilePickerCell>().Bind(onHover, onUnhover);

            cellGO.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        // ── Preview layer (shared) ──

        private static TargetShapePreview BuildPreviewLayer(Transform parent)
        {
            var go = new GameObject("ShapePreview", typeof(RectTransform), typeof(TargetShapePreview));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go.GetComponent<TargetShapePreview>();
        }

        private static void AddHoverListeners(GameObject go, Action onEnter, Action onExit)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => onEnter?.Invoke());
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => onExit?.Invoke());
            trigger.triggers.Add(exit);
        }
    }
}
