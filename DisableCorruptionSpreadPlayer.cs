using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DisableCorruptionSpread
{
	public class DisableCorruptionSpreadPlayer : ModPlayer
	{
		public override void OnEnterWorld() {
			if (!DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled) {
				Main.NewText("Warning: Corruption Spread is enabled. Corruption will spread as normal. Use /spread in chat to disable spreading again.", Color.Orange);
			}
			//Mod.Logger.Info($"OnEnterWorld, CorruptionSpreadDisabled: {DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled}");
		}
	}
}
