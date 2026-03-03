using TMPro;
using UnityEngine;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    public class ConsoleManager : MonoBehaviour
    {
        //Internal properties
        public string text
        {
            get => textMesh.text;
            set => textMesh.text = value;
        }
        public Color color
        {
            get => textMesh.color;
            set => textMesh.color = value;
        }

        //Fields
        public TextMeshProUGUI textMesh;
        public FpsMonitor fpsMonitor = new FpsMonitor();

        //Method which is used for initialization tasks that need to occur before the game starts 
        /// <summary>Initializes component references and state.</summary>
        private void Awake()
        {
            textMesh = GetComponent<TextMeshProUGUI>();
            //value.font = new Font("Consolas");
        }

        //Method which is automatically called before the first frame update  
        void Start()
        {
            fpsMonitor.Start(isActive: false);
        }

        void Update()
        {
            fpsMonitor.Update();
        }

        /// <summary>Runs per fixed-timestep physics update.</summary>
        private void FixedUpdate()
        {
            //string fps = $@"{fpsMonitor.currentFps}";
            //string turn = g.TurnManager.isHeroTurn ? "Hero" : "Opponent";
            //string phase = g.TurnManager.currentPhase.ToString();

            //value.textarea = ""
            //   + $"{fps} FPS" + Environment.NewLine 
            //   + $"Runtime: {Time.time}" + Environment.NewLine
            //   + $"   TurnManager: {turn}" + Environment.NewLine
            //   + $"  Phase: {phase}" + Environment.NewLine
            //   + "";


            //string characterName = hasSelectedHero ? focusedActor.characterName : "-";
            //string boardLocation = hasSelectedHero ? $@"({focusedActor.boardLocation.s},{focusedActor.boardLocation.y})" : "-";
            //string boardPosition = hasSelectedHero ? $@"({focusedActor.transform.boardPosition.s},{focusedActor.transform.boardPosition.y})" : "-";
            //string mouse2D = touchPosition2D.s >= 0 ? $@"({touchPosition2D.s.ToString("N0").Replace(",", ""):N0},{touchPosition2D.y.ToString("N0").Replace(",", ""):N0})" : "-";
            //string mouse3D = touchPosition3D.s >= -4 ? $@"({touchPosition3D.s.ToString("N0").Replace(",", ""):N0},{touchPosition3D.y.ToString("N0").Replace(",", ""):N0},{touchPosition3D.z.ToString("N0").Replace(", ", ""):N0})" : "-";
            //string attackers = battle.attackers.Any() ? $"[{string.Join(",", battle.attackers.Get(s => s.characterName))}]" : "-";
            //string supports = battle.supporters.Any() ? $"[{string.Join(",", battle.supporters.Get(s => s.characterName))}]" : "-";
            //string defenders = battle.defenders.Any() ? $"[{string.Join(",", battle.defenders.Get(s => s.characterName))}]" : "-";
            //string currentTeam = turnManager != null ? g.TurnManager.currentTeam.ToString() : "-";


            //string a0 = actors[0] != null ? $"{actors[0].name}: {actors[0].HP}{Environment.NewLine}": $"{Environment.NewLine}";
            //string a1 = actors[1] != null ? $"{actors[1].name}: {actors[1].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a2 = actors[2] != null ? $"{actors[2].name}: {actors[2].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a3 = actors[3] != null ? $"{actors[3].name}: {actors[3].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a4 = actors[4] != null ? $"{actors[4].name}: {actors[4].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a5 = actors[5] != null ? $"{actors[5].name}: {actors[5].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a6 = actors[6] != null ? $"{actors[6].name}: {actors[6].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a7 = actors[7] != null ? $"{actors[7].name}: {actors[7].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a8 = actors[8] != null ? $"{actors[8].name}: {actors[8].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a9 = actors[9] != null ? $"{actors[9].name}: {actors[9].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a10 = actors[10] != null ? $"{actors[10].name}: {actors[10].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a11 = actors[11] != null ? $"{actors[11].name}: {actors[11].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a12 = actors[12] != null ? $"{actors[12].name}: {actors[12].HP}{Environment.NewLine}" : $"{Environment.NewLine}";
            //string a13 = actors[13] != null ? $"{actors[13].name}: {actors[13].HP}{Environment.NewLine}" : $"{Environment.NewLine}";


            //value.textarea = a0 + a1+ a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 + a13;

        }
    }
}
