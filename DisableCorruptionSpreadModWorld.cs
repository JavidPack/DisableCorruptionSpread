using System;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using ILWorldGen = IL.Terraria.WorldGen;
using static Mono.Cecil.Cil.OpCodes;

namespace DisableCorruptionSpread
{
	// I don't think we need to worry about wall spreading since it is limited naturally by tiles.
	public class DisableCorruptionSpreadModWorld : ModSystem
	{
		public static bool CorruptionSpreadDisabled; // true until toggled. Don't rename since "CorruptionSpreadDisabled" is used in TagCompound

		public static bool patchSuccessTileSpread; // mod will not be loaded if false;
		public static bool patchSuccessAltar;

		public override void Load() {
			patchSuccessTileSpread = false;
			patchSuccessAltar = false;

			// To test: use ModdersToolkit REPL: Main.worldRate = 50;

			ILWorldGen.UpdateWorld_Inner += WorldGen_UpdateWorld_Inner;
			ILWorldGen.SmashAltar += WorldGen_SmashAltar;

			if (!patchSuccessTileSpread)
				Mod.Logger.Error("Failed to apply the patch for tile spreading");
			if (!patchSuccessAltar)
				Mod.Logger.Warn("Failed to apply the patch for fixing altar chance to spawn random tile of corruption");
		}

		public override void Unload() {
			patchSuccessTileSpread = false;
			patchSuccessAltar = false;
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

		private void WorldGen_UpdateWorld_Inner(ILContext il) {
			// Prevents Tile Spread
			// Modifies this code:
			// AllowedToSpreadInfections = true;
			//+AllowedToSpreadInfections = false;
			// CreativePowers.StopBiomeSpreadPower power = CreativePowerManager.Instance.GetPower<CreativePowers.StopBiomeSpreadPower>();

			var c = new ILCursor(il);

			Func<Instruction, bool>[] instructions =
			{
				i => i.MatchLdcI4(1),
				i => i.MatchStsfld<WorldGen>(nameof(WorldGen.AllowedToSpreadInfections))
			};

			if (!c.TryGotoNext(MoveType.After, instructions))
				return; // Patch unable to be applied

			c.Emit<DisableCorruptionSpreadModWorld>(Ldsfld, nameof(CorruptionSpreadDisabled));
			c.Emit<WorldGen>(Stsfld, nameof(WorldGen.AllowedToSpreadInfections));

			patchSuccessTileSpread = true;
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

			const int localIndexOfNum9 = 5;
			const int localIndexOfNum10 = 6;

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdcI4(3),
				i => i.MatchCallvirt<UnifiedRandom>(nameof(UnifiedRandom.Next)),
				i => i.MatchStloc(localIndexOfNum9),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(localIndexOfNum10)))
				return; // Patch unable to be applied

			c.Emit(Ldloc_S, (byte)localIndexOfNum9);
			c.EmitDelegate<Func<int, int>>(num9 => {
				if (CorruptionSpreadDisabled)
					num9 = 2; // Force while loop to fail
				return num9;
			});
			c.Emit(Stloc_S, (byte)localIndexOfNum9);

			patchSuccessAltar = true;
		}

		public override void OnWorldLoad() {
			CorruptionSpreadDisabled = true;
		}

		public override void LoadWorldData(TagCompound tag) {
			if (tag.ContainsKey(nameof(CorruptionSpreadDisabled)))
				CorruptionSpreadDisabled = tag.GetBool(nameof(CorruptionSpreadDisabled)); // using nameof (c#6) can help prevent spelling errors. Be aware that it will lose data if you rename the field.
		}

		public override void SaveWorldData(TagCompound tag) {
			tag[nameof(CorruptionSpreadDisabled)] = CorruptionSpreadDisabled;
		}

		public override void NetSend(BinaryWriter writer) {
			BitsByte flags = new(CorruptionSpreadDisabled);
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader) {
			BitsByte flags = reader.ReadByte();
			flags.Retrieve(ref CorruptionSpreadDisabled);
		}
	}
}