using System;
using System.Numerics;
using gaemstone.Client.Bloxel.Blocks;
using gaemstone.Client.Graphics;
using gaemstone.Common.Bloxel.Chunks;
using gaemstone.Common.ECS;
using Immersion.Voxel.Blocks;

namespace gaemstone.Client.Bloxel.Chunks
{
	public class ChunkMeshGenerator
	{
		private const int STARTING_CAPACITY = 1024;

		private static readonly Vector3[][] OFFSET_PER_FACING = {
			new Vector3[]{ // East  (+X)
				new Vector3(1, 1, 1),
				new Vector3(1, 0, 1),
				new Vector3(1, 0, 0),
				new Vector3(1, 1, 0),
			},
			new Vector3[]{ // West  (-X)
				new Vector3(0, 1, 0),
				new Vector3(0, 0, 0),
				new Vector3(0, 0, 1),
				new Vector3(0, 1, 1),
			},
			new Vector3[]{ // Up    (+Y)
				new Vector3(1, 1, 0),
				new Vector3(0, 1, 0),
				new Vector3(0, 1, 1),
				new Vector3(1, 1, 1),
			},
			new Vector3[]{ // Down  (-Y)
				new Vector3(1, 0, 1),
				new Vector3(0, 0, 1),
				new Vector3(0, 0, 0),
				new Vector3(1, 0, 0),
			},
			new Vector3[]{ // South (+Z)
				new Vector3(0, 1, 1),
				new Vector3(0, 0, 1),
				new Vector3(1, 0, 1),
				new Vector3(1, 1, 1),
			},
			new Vector3[]{ // North (-Z)
				new Vector3(1, 1, 0),
				new Vector3(1, 0, 0),
				new Vector3(0, 0, 0),
				new Vector3(0, 1, 0),
			},
		};

		private static readonly int[] TRIANGLE_INDICES
			= { 0, 1, 3,  1, 2, 3 };


		private readonly Game _game;
		private ushort[] _indices = new ushort[STARTING_CAPACITY];
		private Vector3[] _vertices = new Vector3[STARTING_CAPACITY];
		private Vector3[] _normals  = new Vector3[STARTING_CAPACITY];
		private Vector2[] _uvs      = new Vector2[STARTING_CAPACITY];

		public ChunkMeshGenerator(Game game)
			=> _game = game;

		public MeshInfo? Generate(ChunkPaletteStorage<Block> storage)
		{
			var indexCount  = 0;
			var vertexCount = 0;
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = storage[x, y, z];
				if (block.Prototype == Entity.NONE) continue;

				var blockVertex     = new Vector3(x, y, z);
				ref var textureCell = ref _game.TextureCells.GetRef(block.Prototype.ID);

				foreach (var facing in BlockFacings.ALL) {
					if (!IsNeighborEmpty(storage, x, y, z, facing)) continue;

					if (_indices.Length <= indexCount + 6)
						Array.Resize(ref _indices, _indices.Length << 1);
					if (_vertices.Length <= vertexCount + 4) {
						Array.Resize(ref _vertices, _vertices.Length << 1);
						Array.Resize(ref _normals , _vertices.Length << 1);
						Array.Resize(ref _uvs     , _vertices.Length << 1);
					}

					for (var i = 0; i < TRIANGLE_INDICES.Length; i++)
						_indices[indexCount++] = (ushort)(vertexCount + TRIANGLE_INDICES[i]);

					var normal = facing.ToVector3();
					for (var i = 0; i < 4; i++) {
						var offset = OFFSET_PER_FACING[(int)facing][i];
						_vertices[vertexCount] = blockVertex + offset;
						_normals[vertexCount]  = normal;
						_uvs[vertexCount]      = i switch {
							0 => textureCell.TopLeft,
							1 => textureCell.BottomLeft,
							2 => textureCell.BottomRight,
							3 => textureCell.TopRight,
							_ => throw new InvalidOperationException()
						};
						vertexCount++;
					}
				}
			}

			return _game.MeshManager.Create(
				_indices.AsSpan(0, indexCount), _vertices.AsSpan(0, vertexCount),
				_normals.AsSpan(0, vertexCount), _uvs.AsSpan(0, vertexCount));
		}

		private bool IsNeighborEmpty(
			ChunkPaletteStorage<Block> storage,
			int x, int y, int z, BlockFacing facing)
		{
			var (ox, oy, oz) = facing;
			x += ox; y += oy; z += oz;
			if ((x < 0) || (x >= 16)
			 || (y < 0) || (y >= 16)
			 || (z < 0) || (z >= 16))
				return true;
			return (storage[x, y, z].Prototype == Entity.NONE);
		}
	}
}
