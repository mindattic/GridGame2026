using UnityEditor;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

public partial class DebugWindow
{
    /// <summary>
    /// Live demos for in-progress work — lets you exercise things from the editor instead of
    /// hunting for the right gameplay trigger. World-space UI render checks + the glyph economy.
    /// Add a button here that calls a DebugManager.Demo_* method.
    /// </summary>
    private void RenderDemos()
    {
        GUILayout.Space(8);
        GUILayout.Label("— Demos —", EditorStyles.boldLabel);

        // World-space UI render checks.
        RenderButtonRow(
            ("Show ActionTitle", () => g.DebugManager.Demo_ShowActionTitle()),
            ("Show CastConfirm", () => g.DebugManager.Demo_ShowCastConfirm()),
            ("Hide CastConfirm", () => g.DebugManager.Demo_HideCastConfirm()),
            ("Log GlyphBank", () => g.DebugManager.Demo_LogGlyphBank())
        );

        // Glyph economy — draw glyphs (as if disrupting enemy charges).
        RenderButtonRow(
            ("+ Colorless", () => g.DebugManager.Demo_AddGlyph_Colorless()),
            ("+ Physical", () => g.DebugManager.Demo_AddGlyph_Physical()),
            ("+ Magic", () => g.DebugManager.Demo_AddGlyph_Magic()),
            ("+ Fire", () => g.DebugManager.Demo_AddGlyph_Fire())
        );

        // Glyph economy — spend on recipes.
        RenderButtonRow(
            ("Cast Heal+ (Colorless+Magic)", () => g.DebugManager.Demo_TryCast_Heal2()),
            ("Cast Meteor Slam (Fire+2 Phys)", () => g.DebugManager.Demo_TryCast_MeteorSlam()),
            ("Clear Glyphs", () => g.DebugManager.Demo_ClearGlyphs())
        );
    }
}
