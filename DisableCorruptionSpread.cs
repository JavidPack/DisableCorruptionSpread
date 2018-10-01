using Terraria.ModLoader;
using Harmony;
using System.Reflection;
using Terraria;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Terraria.ModLoader.IO;
using System.IO;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace DisableCorruptionSpread
{
	class DisableCorruptionSpread : Mod
	{
		const string message = "Please don't steal code :D";
		const string HarmonyID = "mod.DisableCorruptionSpread";
		HarmonyInstance harmonyInstance;
		internal Mod HEROsMod;
		internal static DisableCorruptionSpread instance;
		public static bool patchSuccess; // mod will not be loaded if false;

		public DisableCorruptionSpread() { }

		public override void Load()
		{
			patchSuccess = false;
			harmonyInstance = HarmonyInstance.Create(HarmonyID);
			if (!harmonyInstance.HasAnyPatches(HarmonyID)) // In case Unload failed, don't double up.
			{
				harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
			}
			HEROsMod = ModLoader.GetMod("HEROsMod");
			if (!patchSuccess)
			{
				throw new Exception("DisableCorruptionSpread failed to patch. Report this to mod homepage.");
			}
			instance = this;
		}

		public override void Unload()
		{
			instance = null;
			if (harmonyInstance != null)
			{
				harmonyInstance.UnpatchAll(HarmonyID);
			}
			patchSuccess = false;
		}

		public override void PostSetupContent()
		{
			if (HEROsMod != null/* && patchSuccess*/)
				SetupHEROsModIntegration();
		}

		private void SetupHEROsModIntegration()
		{
			// Add Permissions always. 
			HEROsMod.Call(
				// Special string
				"AddPermission",
				// Permission Name
				"ToggleCorruptionSpread",
				// Permission Display Name
				"Toggle Corruption Spread"
			);
			// Add Buttons only to non-servers (otherwise the server will crash, since textures aren't loaded on servers)
			if (!Main.dedServ)
			{
				HEROsMod.Call(
					// Special string
					"AddSimpleButton",
					// Name of Permission governing the availability of the button/tool
					"ToggleCorruptionSpread",
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
		public string ToggleCorruptionSpreadTooltip()
		{
			return DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Enable Corruption Spread" : "Disable Corruption Spread";
		}

		public void ToggleCorruptionSpreadButtonPressed()
		{
			// Send message to Server. Broadcast.
			//DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			RequestToggleCorruption();
		}

		public void PermissionChanged(bool hasPermission)
		{
			// Don't do anything. Since the World has the toggle, someone with the permission needs to set it.
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			MessageType msgType = (MessageType)reader.ReadByte();
			switch (msgType)
			{
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

		public void RequestToggleCorruption()
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				ToggleCorruption();
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				var packet = GetPacket();
				packet.Write((byte)MessageType.RequestToggleCorruption);
				packet.Send();
			}
			else // Server
			{
				ToggleCorruption();
			}
		}

		public void ToggleCorruption()
		{
			DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled = !DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled;
			string message = (DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled ? "Corruption Spread is now disabled. Corruption won't spread." : "Corruption Spread is now enabled. Corruption will spread as normal.");
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				Main.NewText(message, Color.MediumVioletRed);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				Main.NewText("DisableCorruptionSpread Error");
			}
			else
			{
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

	internal class DisableCorruptionSpreadModWorld : ModWorld
	{
		public static bool CorruptionSpreadDisabled; // true until toggled. Don't rename since "CorruptionSpreadDisabled" is used in TagCompound

		public override void Initialize()
		{
			CorruptionSpreadDisabled = true;
		}

		public override void Load(TagCompound tag)
		{
			if (tag.ContainsKey(nameof(CorruptionSpreadDisabled)))
				CorruptionSpreadDisabled = tag.GetBool(nameof(CorruptionSpreadDisabled)); // using nameof (c#6) can help prevent spelling errors. Be aware that it will lose data if you rename the field.
		}

		public override TagCompound Save()
		{
			return new TagCompound {
				{nameof(CorruptionSpreadDisabled), CorruptionSpreadDisabled}
			};
		}

		public override void NetSend(BinaryWriter writer)
		{
			BitsByte flags = new BitsByte(CorruptionSpreadDisabled);
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			flags.Retrieve(ref CorruptionSpreadDisabled);
		}
	}

	internal class ToggleCorruptionSpread : ModCommand
	{
		public override CommandType Type => CommandType.World | CommandType.Console;

		public override string Command => "spread";

		public override string Description => "Toggle Corruption Spread";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			if(caller.CommandType == CommandType.Console)
			{
				DisableCorruptionSpread.instance.ToggleCorruption();
			}
			else
			{
				// SP or Client Request
				if(DisableCorruptionSpread.instance.HEROsMod != null)
				{
					caller.Reply("Use the Heros Mod button instead.");
				}
				else
				{
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

	[HarmonyPatch(typeof(WorldGen))]
	[HarmonyPatch("hardUpdateWorld")]
	public class hardUpdateWorld_Patcher
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			// Modifies this code:
			//if (NPC.downedPlantBoss && WorldGen.genRand.Next(2) != 0)
			//{
			//	return;
			//}
			//+ if(DisableCorruptionSpread.CorruptionSpreadDisabled)
			//+ 	return;
			//if (type == 23 || type == 25 || type == 32 || type == 112 || type == 163 || type == 400 || type == 398)

			var codes = new List<CodeInstruction>(instructions);

			int insertionIndex = -1;
			var instructionsToInsert = new List<CodeInstruction>();

			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldsfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(NPC), nameof(NPC.downedPlantBoss)) && codes[i + 6].opcode == OpCodes.Ret)
				{
					insertionIndex = i + 7;
					var end = codes[i + 7];
					Label ifCorruptionSpreadDisabledFalse = il.DefineLabel();

					instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DisableCorruptionSpreadModWorld), nameof(DisableCorruptionSpreadModWorld.CorruptionSpreadDisabled))) { labels = new List<Label>(end.labels) });
					instructionsToInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, ifCorruptionSpreadDisabledFalse));
					instructionsToInsert.Add(new CodeInstruction(OpCodes.Ret));
					instructionsToInsert.Add(new CodeInstruction(OpCodes.Nop) { labels = new List<Label>() { ifCorruptionSpreadDisabledFalse } }); // could put that label on end actually...
					end.labels.Clear();
				}
			}

			if (insertionIndex != -1)
			{
				codes.InsertRange(insertionIndex, instructionsToInsert);
				ErrorLogger.Log("DisableCorruptionSpread patch success.");
				DisableCorruptionSpread.patchSuccess = true;
			}
			else
			{
				ErrorLogger.Log("DisableCorruptionSpread patch failure.");
				DisableCorruptionSpread.patchSuccess = false;
			}
			return codes.AsEnumerable();
		}
	}
}
