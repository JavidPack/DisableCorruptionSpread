using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DisableCorruptionSpread
{
	public class DisableCorruptionSpreadPlayer : ModPlayer
	{
		public override void OnEnterWorld() {
			if (!DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled) {
				Main.NewText(Language.GetTextValue(Mod.GetLocalizationKey("OnEnterWorldWarnCorruptionSpreadEnabled")), Color.Orange);
			}
			//Mod.Logger.Info($"OnEnterWorld, CorruptionSpreadDisabled: {DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled}");
		}
	}
}
