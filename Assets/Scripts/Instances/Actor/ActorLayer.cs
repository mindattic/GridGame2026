using System.Collections.Generic;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
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

namespace Scripts.Instances.Actor
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
    /// var healthText = actor.Find(ActorLayer.Name.HealthText);
    /// ```
    /// 
    /// LAYER HIERARCHY:
    /// - Front/Back: Main sprite layers
    /// - Backdrop/Frame: Background tint layers
    /// - Thumbnail: Portrait sprite
    /// - HealthText: Numeric HP readout (top-right corner)
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

            public const string Backdrop = "Backdrop";
            public const string Frame = "Frame";
            public const string Thumbnail = "Thumbnail";

            public const string Mask = "Mask";
            public const string Gradient = "Gradient";
            public const string NameTagText = "NameTagText";
            public const string HealthText = "HealthText";

            public const string ActiveIndicator = "ActiveIndicator";
            public const string FocusIndicator = "FocusIndicator";
            public const string TargetIndicator = "TargetIndicator";
        }

        public static class Value
        {
            public const int Backdrop = 1;
            public const int Thumbnail = 2;
            public const int Frame = 5;

            public const int Mask = 17;
            public const int Gradient = 12;
            public const int NameTagText = 22;
            public const int HealthText = 12;

            public const int ActiveIndicator = 28;
            public const int FocusIndicator = 29;
            public const int TargetIndicator = 30;
        }
    }
}
