using Terraria.Localization;
using Terraria.ModLoader;

namespace DisableCorruptionSpread
{
	public class ToggleCorruptionSpread : ModCommand
	{
		public override CommandType Type => CommandType.World | CommandType.Console;

		public override string Command => "spread";

		public override string Description => "Toggle Corruption Spread";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (caller.CommandType == CommandType.Console) {
				DisableCorruptionSpread.ToggleCorruption();
			}
			else {
				// SP or Client Request
				if (DisableCorruptionSpread.HEROsMod != null) {
					caller.Reply(Language.GetTextValue(Mod.GetLocalizationKey("UseHerosButtonInstead")));
				}
				else {
					DisableCorruptionSpread.ToggleCorruption();
				}
			}

			//if (!DisableCorruptionSpread.patchSuccess)
			//{
			//	caller.Reply("DisableCorruptionSpread failed to patch. Report this to mod homepage.");
			//	return;
			//}
			//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			//if (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled)
			//{
			//	caller.Reply("Corruption Spread is now disabled. Corruption won't spread.");
			//}
			//else
			//{
			//	caller.Reply("Corruption Spread is now enabled. Corruption will spread as normal.");
			//}
		}
	}
}
