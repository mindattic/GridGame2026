using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
    /// <summary>
    /// TOGGLESETTING - Configuration for toggle-based settings.
    /// 
    /// PURPOSE:
    /// Defines a boolean on/off setting with getter/setter delegates
    /// for reading/writing the value.
    /// 
    /// PROPERTIES:
    /// - FriendlyName: Display label
    /// - TooltipText: Help text
    /// - Getter/Setter: ProfileSettings accessors
    /// 
    /// RELATED FILES:
    /// - SettingsManager.cs: Uses this to create UI
    /// - ProfileSettings.cs: Storage target
    /// </summary>
    public class ToggleSetting
    {
        public string FriendlyName { get; }
        public string TooltipText { get; }
        public Func<ProfileSettings, bool> Getter { get; }
        public Action<ProfileSettings, bool> Setter { get; }

        public ToggleSetting(
            string friendlyName,
            string tooltipText,
            Func<ProfileSettings, bool> getter,
            Action<ProfileSettings, bool> setter)
        {
            FriendlyName = friendlyName;
            TooltipText = tooltipText;
            Getter = getter;
            Setter = setter;
        }
    }
}
