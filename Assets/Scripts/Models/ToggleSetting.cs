using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
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
