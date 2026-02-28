using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
	/// <summary>
	/// DROPDOWNSETTING - Configuration for dropdown-based settings.
	/// 
	/// PURPOSE:
	/// Defines an enum-based setting with getter/setter delegates
	/// for reading/writing the value.
	/// 
	/// PROPERTIES:
	/// - FriendlyName: Display label
	/// - TooltipText: Help text
	/// - EnumType: Type of enum for options
	/// - Getter/Setter: ProfileSettings accessors
	/// 
	/// RELATED FILES:
	/// - SettingsManager.cs: Uses this to create UI
	/// - ProfileSettings.cs: Storage target
	/// </summary>
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
