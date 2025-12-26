using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
	public class DropdownSetting
	{
		public string FriendlyName { get; }
		public string TooltipText { get; }
		public Type EnumType { get; }
		public Func<ProfileSettings, object> Getter { get; }
		public Action<ProfileSettings, object> Setter { get; }

		public DropdownSetting(
			string friendlyName,
			string tooltipText,
			Type enumType,
			Func<ProfileSettings, object> getter,
			Action<ProfileSettings, object> setter)
		{
			FriendlyName = friendlyName;
			TooltipText = tooltipText;
			EnumType = enumType;
			Getter = getter;
			Setter = setter;
		}
	}
}
