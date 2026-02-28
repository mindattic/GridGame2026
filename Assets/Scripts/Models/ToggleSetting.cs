using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
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
