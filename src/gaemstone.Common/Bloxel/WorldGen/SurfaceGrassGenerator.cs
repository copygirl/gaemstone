// using System.Collections.Generic;
// using Immersion.Voxel.Blocks;
// using Immersion.Voxel.Chunks;

// namespace Immersion.Voxel.WorldGen
// {
// 	public class SurfaceGrassGenerator
// 		: IWorldGenerator
// 	{
// 		public static readonly string IDENTIFIER = nameof(SurfaceGrassGenerator);

// 		private const int AIR_BLOCKS_NEEDED   = 12;
// 		private const int DIRT_BLOCKS_BENEATH =  3;

// 		public string Identifier { get; } = IDENTIFIER;

// 		public IEnumerable<string> Dependencies { get; } = new []{
// 			BasicWorldGenerator.IDENTIFIER
// 		};

// 		public IEnumerable<(Neighbor, string)> NeighborDependencies { get; } = new []{
// 			(Neighbor.Up, BasicWorldGenerator.IDENTIFIER)
// 		};

// 		public void Populate(World world, IChunk chunk)
// 		{
// 			var up = chunk.Neighbors[Neighbor.Up]!;
// 			for (var lx = 0; lx < 16; lx++)
// 			for (var lz = 0; lz < 16; lz++) {
// 				var numAirBlocks = 0;
// 				var blockIndex   = 0;
// 				for (var ly = 15 + AIR_BLOCKS_NEEDED; ly >= 0; ly--) {
// 					var block = (ly >= 16) ? up.Storage[lx, ly - 16, lz]
// 					                       : chunk.Storage[lx, ly, lz];
// 					if (block.IsAir) {
// 						numAirBlocks++;
// 						blockIndex = 0;
// 					} else if ((numAirBlocks >= AIR_BLOCKS_NEEDED) || (blockIndex > 0)) {
// 						if (ly < 16) {
// 							if (blockIndex == 0)
// 								chunk.Storage[lx, ly, lz] = Block.GRASS;
// 							else if (blockIndex <= DIRT_BLOCKS_BENEATH)
// 								chunk.Storage[lx, ly, lz] = Block.DIRT;
// 						}
// 						blockIndex++;
// 						numAirBlocks = 0;
// 					}
// 				}
// 			}
// 		}
// 	}
// }
