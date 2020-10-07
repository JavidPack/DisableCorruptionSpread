using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

using static Mono.Cecil.Cil.OpCodes;

namespace DisableCorruptionSpread
{
	class DisableCorruptionSpread : Mod
	{
		const string message = "Please don't steal code :D";
		internal Mod HEROsMod;
		internal const string ToggleCorruptionSpread_Permission = "ToggleCorruptionSpread";
		internal const string ToggleCorruptionSpread_Display = "Toggle Corruption Spread";

		internal static DisableCorruptionSpread instance;
		public static bool patchSuccessTileSpread; // mod will not be loaded if false;
		public static bool patchSuccessGrassSpread;
		public static bool patchSuccessAltar;

		public override void Load() {
			patchSuccessTileSpread = false;
			patchSuccessGrassSpread = false;
			patchSuccessAltar = false;
			HEROsMod = ModLoader.GetMod("HEROsMod");

			// To test: use ModdersToolkit REPL: Main.worldRate = 50;

			IL.Terraria.WorldGen.hardUpdateWorld += WorldGen_hardUpdateWorld;
			IL.Terraria.WorldGen.UpdateWorld += WorldGen_UpdateWorld;
			IL.Terraria.WorldGen.SmashAltar += WorldGen_SmashAltar;

			if (!patchSuccessTileSpread)
				Logger.Warn("Failed to apply the patch for tile spreading");
			if (!patchSuccessGrassSpread)
				Logger.Warn("Failed to apply the patch for grass spreading");
			if (!patchSuccessAltar)
				Logger.Warn("Failed to apply the patch for fixing altar chance to spawn random tile of corruption");

			instance = this;
		}

		/*
		Some useful ModdersToolkit REPL snippets for testing patches (need `using Terraria.ID;`):

		int CorruptGrass = 0;
		for (int i = 0; i < Main.maxTilesX; i++) {
			for (int j = 0; j < Main.maxTilesY; j++) {
				Tile tile = Main.tile[i, j];
				if (tile.type == TileID.Crimstone)
					Crimstone++;
				if (tile.type == TileID.Pearlstone)
					Pearlstone++;
				if (tile.type == TileID.Ebonstone)
					Ebonstone++;
				if (tile.type == TileID.Grass)
					Grass++;
				if (tile.type == TileID.FleshGrass)
					FleshGrass++;
				if (tile.type == TileID.HallowedGrass)
					HallowedGrass++;
				if (tile.type == TileID.CorruptGrass)
					CorruptGrass++;
			}
		}
		Main.NewText("Totals: ");
		Main.NewText("Ebonstone: " + Ebonstone);
		Main.NewText("Pearlstone: " + Pearlstone);
		Main.NewText("Crimstone: " + Crimstone);
		Main.NewText("Grass: " + Grass);
		Main.NewText("FleshGrass: " + FleshGrass);
		Main.NewText("HallowedGrass: " + HallowedGrass);
		Main.NewText("CorruptGrass: " + CorruptGrass);

		//for (int i = 0; i < Main.maxTilesX; i++) {
		//    for (int j = 0; j < Main.maxTilesY; j++) {
		//		Tile tile = Main.tile[i, j];
		//		if (tile.type == TileID.Crimstone)
		//			tile.type = TileID.Stone;
		//		if (tile.type == TileID.Pearlstone)
		//			tile.type = TileID.Stone;
		//		if (tile.type == TileID.Ebonstone)
		//			tile.type = TileID.Stone;
		//	}
		//}
		*/
		private void WorldGen_hardUpdateWorld(ILContext il) {
			// Prevents Tile Spread
			// Modifies this code:
			//if (NPC.downedPlantBoss && WorldGen.genRand.Next(2) != 0) {
			//	return;
			//+ if(DisableCorruptionSpread.CorruptionSpreadDisabled)
			//+ 	return;
			//if (type == 23 || type == 25 || type == 32 || type == 112 || type == 163 || type == 400 || type == 398) {

			var c = new ILCursor(il);

			if (!c.TryGotoNext(i => i.MatchLdsfld<NPC>(nameof(NPC.downedPlantBoss))))
				return; // Patch unable to be applied
			if (!c.TryGotoNext(MoveType.After, i => i.MatchRet())) // instead of: c.Index++; // Move after ret
				return;

			c.MoveAfterLabels(); // Simplifies the logic, as we don't have to point the original label to our instruction. Affects current cursor index, not a persistent setting.
			c.EmitDelegate<Func<bool>>(() => {
				return DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			});
			var postSkipCheckLabel = il.DefineLabel();
			c.Emit(Brfalse, postSkipCheckLabel);
			c.Emit(Ret);
			c.MarkLabel(postSkipCheckLabel);

			// These 2 original approaches are a bit unreliable and ugly.
			//var originalLabel = c.IncomingLabels.First(); // unreliable on Debug. ldc.i4.s 23 might be only reliable one.

			//c.Emit(Ldsfld, typeof(DisableCorruptionSpreadModWorld).GetField(nameof(DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled)));
			//c.Index--;
			//c.MarkLabel(originalLabel);
			//c.Index++;

			//int index = c.Index;
			//var result = c.EmitDelegate<Func<bool>>(() => {
			//	return DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			//});
			//originalLabel.Target = c.Instrs[index];

			patchSuccessTileSpread = true;
		}
		private void WorldGen_UpdateWorld(ILContext il) {
			// Prevents Grass Tile Spread
			// Modifies this code: 
			/*
			for (int num22 = num9; num22 < num10; num22++)
			{
				for (int num23 = num11; num23 < num12; num23++)
				{
					if ((num7 != num22 || num8 != num23) && Main.tile[num22, num23].active())
					{
			  +			// if(num19 != 2) continue;
						if (num19 == 32)
						{
							num19 = 23;
						}
			*/

			// TODO: Customize Jungle/Moss/Mushroom
			// TODO: Underground Spread

			var c = new ILCursor(il);

			if (!c.TryGotoNext(i => i.MatchLdloc(26),
				i => i.MatchLdcI4(32),
				i => i.MatchBneUn(out _)))
				return; // Patch unable to be applied

			var forLoopContinueLabel = c.Prev.Operand as ILLabel;
			if (forLoopContinueLabel != null) {
				c.Emit(Ldloc, 26);
				c.EmitDelegate<Func<int, bool>>((int num19) => {
					if (!DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled)
						return false;
					return num19 != 2; // bool skip =...
				});
				c.Emit(Brtrue, forLoopContinueLabel);

				patchSuccessGrassSpread = true;
			}
		}

		private void WorldGen_SmashAltar(ILContext il) {
			// Modifies this code: 
			/*
 				int num9 = genRand.Next(3);
				int num10 = 0;
			+	nume9 = 2; // skips the randomly occuring while loop
				while (num9 != 2 && num10++ < 1000)
			{
			*/

			var c = new ILCursor(il);

			const int localIndexOfNum9 = 4;
			const int localIndexOfNum10 = 5;

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdcI4(3),
				i => i.MatchCallvirt<Terraria.Utilities.UnifiedRandom>("Next"),
				i => i.MatchStloc(localIndexOfNum9),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(localIndexOfNum10)))
				return; // Patch unable to be applied

			c.Emit(Ldloc_S, (byte)localIndexOfNum9);
			c.EmitDelegate<Func<int, int>>((int num9) => {
				if (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled)
					num9 = 2; // Force while loop to fail
				return num9;
			});
			c.Emit(Stloc_S, (byte)localIndexOfNum9);

			patchSuccessAltar = true;
		}


		public override void Unload() {
			instance = null;
			patchSuccessTileSpread = false;
			patchSuccessGrassSpread = false;
			patchSuccessAltar = false;
		}

		public override void PostSetupContent() {
			if (HEROsMod != null/* && patchSuccess*/)
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
					GetTexture("ToggleCorruptionSpreadButton"),
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
		public string ToggleCorruptionSpreadTooltip() {
			return DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Enable Corruption Spread" : "Disable Corruption Spread";
		}

		public void ToggleCorruptionSpreadButtonPressed() {
			// Send message to Server. Broadcast.
			//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			RequestToggleCorruption();
		}

		public void PermissionChanged(bool hasPermission) {
			// Don't do anything. Since the World has the toggle, someone with the permission needs to set it.
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType msgType = (MessageType)reader.ReadByte();
			switch (msgType) {
				case MessageType.RequestToggleCorruption:
					ToggleCorruption();
					//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
					//string message = (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Corruption Spread is now disabled. Corruption won't spread." : "Corruption Spread is now enabled. Corruption will spread as normal.");
					//NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.MediumVioletRed);
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

		public void ToggleCorruption() {
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
				NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.MediumVioletRed);
				NetMessage.SendData(MessageID.WorldData);
			}
		}

		internal enum MessageType : byte
		{
			RequestToggleCorruption, // sent by clients to server
									 //InformToggleCorruption, // sent by server to client
		}
	}

	// I don't think we need to worry about wall spreading since it is limited naturally by tiles.
	internal class DisableCorruptionSpreadModWorld : ModWorld
	{
		public static bool CorruptionSpreadDisabled; // true until toggled. Don't rename since "CorruptionSpreadDisabled" is used in TagCompound

		public override void Initialize() {
			CorruptionSpreadDisabled = true;
		}

		public override void Load(TagCompound tag) {
			if (tag.ContainsKey(nameof(CorruptionSpreadDisabled)))
				CorruptionSpreadDisabled = tag.GetBool(nameof(CorruptionSpreadDisabled)); // using nameof (c#6) can help prevent spelling errors. Be aware that it will lose data if you rename the field.
		}

		public override TagCompound Save() {
			return new TagCompound {
				{nameof(CorruptionSpreadDisabled), CorruptionSpreadDisabled}
			};
		}

		public override void NetSend(BinaryWriter writer) {
			BitsByte flags = new BitsByte(CorruptionSpreadDisabled);
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader) {
			BitsByte flags = reader.ReadByte();
			flags.Retrieve(ref CorruptionSpreadDisabled);
		}
	}

	internal class ToggleCorruptionSpread : ModCommand
	{
		public override CommandType Type => CommandType.World | CommandType.Console;

		public override string Command => "spread";

		public override string Description => "Toggle Corruption Spread";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (caller.CommandType == CommandType.Console) {
				DisableCorruptionSpread.instance.ToggleCorruption();
			}
			else {
				// SP or Client Request
				if (DisableCorruptionSpread.instance.HEROsMod != null) {
					caller.Reply("Use the Heros Mod button instead.");
				}
				else {
					DisableCorruptionSpread.instance.ToggleCorruption();
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
