/*
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace DisableCorruptionSpread
{
	[Label("Begone Evil! Config")]
	class DisableCorruptionSpreadConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;

		// Get rid of World-specific /spread command?

		[DefaultValue(true)]
		[Label("[i:59] Disable Corruption Spread")]
		public bool DisableCorruptionSpreading;

		[DefaultValue(true)]
		[Label("[i:369] Disable Hallow Spread")]
		public bool DisableHallowSpreading;

		// Jungle

		// Mushroom

		// Moss

		[DefaultValue(true)]
		[Label("[i:61][i:836][i:409] Disable Demon Altar Random Spread")]
		[Tooltip("When breaking a Demon Altar, there is a chance of Crimstone, Pearlstone, or Ebonstone randomly spawning in the world.\nThis option disables that.")]
		public bool DisableAltarSpreading;

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			if (DisableCorruptionSpread.HEROsMod != null && DisableCorruptionSpread.HEROsMod.Version >= new Version(0, 2, 2)) {
				if (DisableCorruptionSpread.HEROsMod.Call("HasPermission", whoAmI, DisableCorruptionSpread.ToggleCorruptionSpread_Permission) is bool result && result)
					return true;
				message = $"You lack the \"{DisableCorruptionSpread.ToggleCorruptionSpread_Display}\" permission.";
				return false;
			}
			return base.AcceptClientChanges(pendingConfig, whoAmI, ref message);
		}
	}
}
*/