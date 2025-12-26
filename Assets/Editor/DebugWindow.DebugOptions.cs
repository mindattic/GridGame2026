using Assets.Helper;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // RenderDebugOptions renders a dropdown for various debug options and a Run button.
    private void RenderDebugOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Debug Options", GUILayout.Width(Screen.width * 0.25f));
        selectedOption = (DebugOptions)EditorGUILayout.EnumPopup(selectedOption, GUILayout.Width(Screen.width * 0.5f));
        if (GUILayout.Button("Run", GUILayout.Width(Screen.width * 0.25f)))
            OnDebugOptionRunClick();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    // OnDebugOptionRunClick executes a test based on the selected debug option.
    private void OnDebugOptionRunClick()
    {
        switch (selectedOption)
        {
            case DebugOptions.AddExperience: g.DebugManager.AddExperience(); break;  
            case DebugOptions.ArrangeSingleCombo: g.DebugManager.ArrangeSingleCombo(); break;
            case DebugOptions.ArrangeDoubleCombo: g.DebugManager.ArrangeDoubleCombo(); break;
            case DebugOptions.ArrangeTripleCombo: g.DebugManager.ArrangeTripleCombo(); break;
            case DebugOptions.ArrangeSurroundCombo: g.DebugManager.ArrangeSurroundCombo(); break;

            case DebugOptions.Bump: g.DebugManager.Bump(); break;
            case DebugOptions.Dodge: g.DebugManager.Dodge(); break;
            case DebugOptions.Fireball: g.DebugManager.Fireball(); break;
            case DebugOptions.Heal: g.DebugManager.Heal(); break;
            case DebugOptions.KillEnemies: g.DebugManager.KillEnemies(); break;
            case DebugOptions.KillHeroes: g.DebugManager.KillHeroes(); break;

            case DebugOptions.GotoPostBattleScreen: g.DebugManager.GotoPostBattleScreen(); break;
            case DebugOptions.PortraitPopIn: g.DebugManager.PortraitPopIn(); break;
            case DebugOptions.Portrait2DSlideIn: g.DebugManager.Portrait2DSlideIn(); break;
            case DebugOptions.Portrait3DSlideIn: g.DebugManager.Portrait3DSlideIn(); break;
            case DebugOptions.RandomizeBackground: g.DebugManager.RandomizeBackground(); break;
            case DebugOptions.Shake: g.DebugManager.Shake(); break;

            case DebugOptions.SpawnCoins: g.DebugManager.SpawnCoins(); break;

            case DebugOptions.SpawnDamageText: g.DebugManager.SpawnDamageText(); break;
            case DebugOptions.SpawnHealText: g.DebugManager.SpawnHealText(); break;
            case DebugOptions.SpawnSupportLines: g.DebugManager.SpawnSupportLines(); break;
            case DebugOptions.SpawnSynergyLines: g.DebugManager.SpawnSynergyLines(); break;

            case DebugOptions.SpawnTitle: g.DebugManager.TitleTest(); break;
            case DebugOptions.SpawnTooltip1: g.DebugManager.SpawnTooltip1(); break;
            case DebugOptions.SpawnTooltip2: g.DebugManager.SpawnTooltip2(); break;

            case DebugOptions.Spin: g.DebugManager.Spin(); break;
            case DebugOptions.TriggerEnemyMoveAttack: g.DebugManager.TriggerEnemyMoveAttack(); break;
            case DebugOptions.TriggerEnemyAttack: g.DebugManager.TriggerEnemyAttack(); break;

            default: Debug.LogWarning("OnDebugOptionRunClick failed."); break;
        }
    }
}
