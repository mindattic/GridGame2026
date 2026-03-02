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
    public static class SortingHelper
    {
        public static class Layer
        {
            public const string Default = "Default";
            public const string Board = "BoardManager";
            public const string DottedLine = "DottedLine";
            public const string SupportLineBelow = "SupportLineBelow";
            public const string ActorBelow = "ActorBelow";
            public const string BoardOverlay = "BoardOverlay";
            public const string SupportLineAbove = "SupportLineAbove";
            public const string ActorAbove = "ActorAbove";
            public const string VFX = "VFX";
            public const string Coin = "Coin";
            public const string DamageText = "CombatTextManager";
            public const string PortraitPopIn = "PortraitPopIn";
            public const string Portrait = "Portrait3DManager";
        }

        public static class Order
        {
            public const int Min = 0;
            public const int Opponent = 100;
            public const int Supporter = 200;
            public const int Attacker = 300;
            public const int AttackLine = 400;
            public const int Max = 999;
        }
    }

}
