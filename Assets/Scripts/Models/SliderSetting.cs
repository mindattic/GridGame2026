using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// SLIDERSETTING - Configuration for slider-based settings.
    /// 
    /// PURPOSE:
    /// Defines a numeric setting with min/max range, increment step,
    /// and getter/setter delegates for reading/writing the value.
    /// 
    /// PROPERTIES:
    /// - FriendlyName: Display label
    /// - TooltipText: Help text
    /// - Min/Max: Value range
    /// - Increment: Step size
    /// - AsInt: Round to integer
    /// - Getter/Setter: ProfileSettings accessors
    /// 
    /// RELATED FILES:
    /// - SettingsManager.cs: Uses this to create UI
    /// - ProfileSettings.cs: Storage target
    /// </summary>
    public class SliderSetting
    {
        public string FriendlyName { get; }
        public string TooltipText { get; }
        public float Min { get; }
        public float Max { get; }
        public float Increment { get; }
        public bool AsInt { get; }
        public Func<ProfileSettings, float> Getter { get; }
        public Action<ProfileSettings, float> Setter { get; }

        public SliderSetting(
            string friendlyName,
            string tooltipText,
            float min,
            float max,
            float increment,
            Func<ProfileSettings, float> getter,
            Action<ProfileSettings, float> setter,
            bool asInt = false)
        {
            FriendlyName = friendlyName;
            TooltipText = tooltipText;
            Min = min;
            Max = max;
            Increment = increment;
            Getter = getter;
            Setter = setter;
            AsInt = asInt;
        }
    }
}
