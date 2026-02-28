using System.Collections.Generic;

namespace Game.Instances.Actor
{
    /// <summary>
    /// ACTORLAYER - Layer name constants for actor rendering.
    /// 
    /// PURPOSE:
    /// Defines string constants for all named layers within
    /// an actor's GameObject hierarchy.
    /// 
    /// USAGE:
    /// ```csharp
    /// var front = actor.Find(ActorLayer.Name.Front);
    /// var healthFill = actor.Find(ActorLayer.Name.HealthBar.Fill);
    /// ```
    /// 
    /// LAYER HIERARCHY:
    /// - Front/Back: Main sprite layers
    /// - Opaque/Quality/Glow: Visual effect layers
    /// - Parallax: 3D-like depth effect
    /// - HealthBar.*: HP bar components
    /// - ActionBar.*: Action/turn bar components
    /// 
    /// RELATED FILES:
    /// - ActorRenderers.cs: Uses these constants
    /// - ActorFactory.cs: Creates layers
    /// </summary>
    public static class ActorLayer
    {
        public static class Name
        {
            public const string Front = "Front";
            public const string Back = "Back";

            public const string Opaque = "Opaque";
            public const string Quality = "Quality";
            public const string Glow = "Glow";
            public const string Parallax = "Parallax";
            public const string Thumbnail = "Thumbnail";
            public const string Gradient = "Gradient";
            public const string Frame = "Frame";
            public const string StatusIcon = "StatusIcon";

            public static class HealthBar
            {
                public const string Root = "HealthBar";
                public const string Back = "HealthBarBack";
                public const string Drain = "HealthBarDrain";
                public const string Fill = "HealthBarFill";
                public const string Text = "HealthBarText";
            }

            public static class ActionBar
            {
                public const string Root = "ActionBar";
                public const string Back = "ActionBarBack";
                public const string Drain = "ActionBarDrain";
                public const string Fill = "ActionBarFill";
                public const string Text = "ActionBarText";
            }

            public const string Mask = "Mask";
            public const string RadialBack = "RadialBack";
            public const string RadialFill = "RadialFill";
            public const string RadialText = "RadialText";
            public const string TurnDelayText = "TurnDelayText";
            public const string NameTagText = "NameTagText";

            //Armor Sub-Objects
            public static class Armor
            {
                public const string Root = "Armor";
                public const string ArmorNorth = "ArmorNorth";
                public const string ArmorEast = "ArmorEast";
                public const string ArmorSouth = "ArmorSouth";
                public const string ArmorWest = "ArmorWest";
            }

            public const string ActiveIndicator = "ActiveIndicator";
            public const string FocusIndicator = "FocusIndicator";
            public const string TargetIndicator = "TargetIndicator";

        }

        public static class Value
        {
            public const int Opaque = 1;
            public const int Quality = 2;
            public const int Glow = 3;
            public const int Parallax = 4;
            public const int Thumbnail = 5;
            public const int Gradient = 6;

            public const int Frame = 7;
            public const int StatusIcon = 8;

            public static class HealthBar
            {
                public const int Back = 9;  
                public const int Drain = 10;
                public const int Fill = 11;
                public const int Text = 12;
            }

            public static class ActionBar
            {
                public const int Back = 13;
                public const int Drain = 14;
                public const int Fill = 15;
                public const int Text = 16;
            }

            public const int Mask = 17;
            public const int RadialBack = 18;
            public const int RadialFill = 19;
            public const int RadialText = 20;
            public const int TurnDelayText = 21;
            public const int NameTagText = 22;
            //public const int WeaponIcon = 23;
           

            public static class Armor
            {
                public const int ArmorNorth = 24;
                public const int ArmorEast = 25;
                public const int ArmorSouth = 26;
                public const int ArmorWest = 27;
            }

            public const int ActiveIndicator = 28;
            public const int FocusIndicator = 29;
            public const int TargetIndicator = 30   ;


        }

    }
}
