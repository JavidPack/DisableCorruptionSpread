using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace DisableCorruptionSpread
{
	public class DisableCorruptionSpread : Mod
	{
		const string message = "Please don't steal code :D";
		internal static Mod? HEROsMod;
		internal const string ToggleCorruptionSpread_Permission = "ToggleCorruptionSpread";
		internal const string ToggleCorruptionSpread_Display = "Toggle Corruption Spread";

		public override void Load() {
			ModLoader.TryGetMod("HEROsMod", out HEROsMod);
		}

		public override void Unload() {
			HEROsMod = null;
		}

		public override void PostSetupContent() {
			if (HEROsMod != null /*&& patchSuccess*/)
				SetupHEROsModIntegration();
		}

		private void SetupHEROsModIntegration() {
			// Add Permissions always.
			HEROsMod.Call(
				// Special string
				"AddPermission",
				// Permission Name
				ToggleCorruptionSpread_Permission,
				// Permission Display Name
				ToggleCorruptionSpread_Display
			);
			// Add Buttons only to non-servers (otherwise the server will crash, since textures aren't loaded on servers)
			if (!Main.dedServ) {
				HEROsMod.Call(
					// Special string
					"AddSimpleButton",
					// Name of Permission governing the availability of the button/tool
					ToggleCorruptionSpread_Permission,
					// Texture of the button. 38x38 is recommended for HERO's Mod. Also, a white outline on the icon similar to the other icons will look good.
					Assets.Request<Texture2D>("ToggleCorruptionSpreadButton"),
					// A method that will be called when the button is clicked
					(Action)ToggleCorruptionSpreadButtonPressed,
					// A method that will be called when the player's permissions have changed
					(Action<bool>)PermissionChanged,
					// A method that will be called when the button is hovered, returning the Tooltip
					(Func<string>)ToggleCorruptionSpreadTooltip
				);
			}
		}

		// These assume patchSuccess and permissions.
		public static string ToggleCorruptionSpreadTooltip() {
			return DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Enable Corruption Spread" : "Disable Corruption Spread";
		}

		public void ToggleCorruptionSpreadButtonPressed() {
			// Send message to Server. Broadcast.
			//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			RequestToggleCorruption();
		}

		public static void PermissionChanged(bool hasPermission) {
			// Don't do anything. Since the World has the toggle, someone with the permission needs to set it.
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType msgType = (MessageType)reader.ReadByte();
			switch (msgType) {
				case MessageType.RequestToggleCorruption:
					ToggleCorruption();
					//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
					//string message = (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Corruption Spread is now disabled. Corruption won't spread." : "Corruption Spread is now enabled. Corruption will spread as normal.");
					//ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.MediumVioletRed);
					//NetMessage.SendData(MessageID.WorldData);
					//bool hasPermission = true;
					//if(HEROsMod != null)
					//{
					//	// Assume permission, since packet sent by button
					//	//hasPermission = HEROsMod.Call("HasPermission") never implemented....
					//}
					//if (hasPermission)
					//{
					//	var packet = GetPacket();
					//	packet.Write((byte)MessageType.InformToggleCorruption);
					//	packet.Write((byte)MessageType.InformToggleCorruption);
					//}
					break;
				default:
					break;
			}
		}

		public void RequestToggleCorruption() {
			if (Main.netMode == NetmodeID.SinglePlayer) {
				ToggleCorruption();
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				var packet = GetPacket();
				packet.Write((byte)MessageType.RequestToggleCorruption);
				packet.Send();
			}
			else // Server
			{
				ToggleCorruption();
			}
		}

		public static void ToggleCorruption() {
			DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			string message = (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Corruption Spread is now disabled. Corruption won't spread." : "Corruption Spread is now enabled. Corruption will spread as normal.");
			if (Main.netMode == NetmodeID.SinglePlayer) {
				Main.NewText(message, Color.MediumVioletRed);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				Main.NewText("DisableCorruptionSpread Error");
			}
			else {
				Console.WriteLine(message);
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.MediumVioletRed);
				NetMessage.SendData(MessageID.WorldData);
			}
		}

		internal enum MessageType : byte
		{
			RequestToggleCorruption, // sent by clients to server
									 //InformToggleCorruption, // sent by server to client
		}
	}
}
