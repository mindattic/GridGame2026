using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{

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
