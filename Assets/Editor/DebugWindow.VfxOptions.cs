using Assets.Helper;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // RenderVfxOptions renders a dropdown to select a VfxManager option and a Bounce button.
    private void RenderVfxOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("VfxManager", GUILayout.Width(Screen.width * 0.25f));
        selectedVfx = (VFX)EditorGUILayout.EnumPopup(selectedVfx, GUILayout.Width(Screen.width * 0.5f));
        if (GUILayout.Button("Play", GUILayout.Width(Screen.width * 0.25f)))
            OnPlayVFXClick();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    // OnPlayVFXClick plays a visual effects test based on the selected VfxManager option.
    private void OnPlayVFXClick()
    {
        switch (selectedVfx)
        {
            case VFX.BlueSlash1: g.DebugManager.VFXTest_BlueSlash1(); break;
            case VFX.BlueSlash2: g.DebugManager.VFXTest_BlueSlash2(); break;
            case VFX.BlueSlash3: g.DebugManager.VFXTest_BlueSlash3(); break;
            case VFX.BlueSlash4: g.DebugManager.VFXTest_BlueSlash4(); break;
            case VFX.BlueSword: g.DebugManager.VFXTest_BlueSword(); break;
            case VFX.BlueSword4X: g.DebugManager.VFXTest_BlueSword4X(); break;
            case VFX.BloodClaw: g.DebugManager.VFXTest_BloodClaw(); break;
            case VFX.LevelUp: g.DebugManager.VFXTest_LevelUp(); break;
            case VFX.YellowHit: g.DebugManager.VFXTest_YellowHit(); break;
            case VFX.DoubleClaw: g.DebugManager.VFXTest_DoubleClaw(); break;
            case VFX.LightningExplosion: g.DebugManager.VFXTest_LightningExplosion(); break;
            case VFX.BuffLife: g.DebugManager.VFXTest_BuffLife(); break;
            case VFX.RotaryKnife: g.DebugManager.VFXTest_RotaryKnife(); break;
            case VFX.AirSlash: g.DebugManager.VFXTest_AirSlash(); break;
            case VFX.FireRain: g.DebugManager.VFXTest_FireRain(); break;
            case VFX.VFXTest_Ray_Blast: g.DebugManager.VFXTest_RayBlast(); break;
            case VFX.LightningStrike: g.DebugManager.VFXTest_LightningStrike(); break;
            case VFX.PuffyExplosion: g.DebugManager.VFXTest_PuffyExplosion(); break;
            case VFX.RedSlash2X: g.DebugManager.VFXTest_RedSlash2X(); break;
            case VFX.GodRays: g.DebugManager.VFXTest_GodRays(); break;
            case VFX.AcidSplash: g.DebugManager.VFXTest_AcidSplash(); break;
            case VFX.GreenBuff: g.DebugManager.VFXTest_GreenBuff(); break;
            case VFX.GoldBuff: g.DebugManager.VFXTest_GoldBuff(); break;
            case VFX.HexShield: g.DebugManager.VFXTest_HexShield(); break;
            case VFX.ToxicCloud: g.DebugManager.VFXTest_ToxicCloud(); break;
            case VFX.OrangeSlash: g.DebugManager.VFXTest_OrangeSlash(); break;
            case VFX.MoonFeather: g.DebugManager.VFXTest_MoonFeather(); break;
            case VFX.PinkSpark: g.DebugManager.VFXTest_PinkSpark(); break;
            case VFX.BlueYellowSword: g.DebugManager.VFXTest_BlueYellowSword(); break;
            case VFX.BlueYellowSword3X: g.DebugManager.VFXTest_BlueYellowSword3X(); break;
            case VFX.RedSword: g.DebugManager.VFXTest_RedSword(); break;
            case VFX.TechSword: g.DebugManager.VFXTest_TechSword(); break;
            default: Debug.LogWarning("OnPlayVFXClick failed."); break;
        }
    }
}
