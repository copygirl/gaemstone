using System.Linq;
using System;
using System.Numerics;
using gaemstone.Bloxel.Chunks;
using gaemstone.Client;
using gaemstone.Client.Graphics;
using gaemstone.Common.ECS.Stores;
using gaemstone.Common.Components;

namespace gaemstone.Bloxel.Client
{
	// TODO: Turn into an IProcessor.
	public class ChunkMeshGenerator
	{
		const int STARTING_CAPACITY = 1024;

		static readonly Vector3[][] OFFSET_PER_FACING = {
			new Vector3[]{ new(1,1,1), new(1,0,1), new(1,0,0), new(1,1,0) }, // East  (+X)
			new Vector3[]{ new(0,1,0), new(0,0,0), new(0,0,1), new(0,1,1) }, // West  (-X)
			new Vector3[]{ new(1,1,0), new(0,1,0), new(0,1,1), new(1,1,1) }, // Up    (+Y)
			new Vector3[]{ new(1,0,1), new(0,0,1), new(0,0,0), new(1,0,0) }, // Down  (-Y)
			new Vector3[]{ new(0,1,1), new(0,0,1), new(1,0,1), new(1,1,1) }, // South (+Z)
			new Vector3[]{ new(1,1,0), new(1,0,0), new(0,0,0), new(0,1,0) }  // North (-Z)
		};

		static readonly int[] TRIANGLE_INDICES
			= { 0, 1, 3,  1, 2, 3 };


		readonly MeshManager _meshManager;
		readonly LookupDictionaryStore<ChunkPos, Chunk> _chunkStore;
		readonly IComponentStore<ChunkPaletteStorage<Prototype>> _storageStore;
		readonly IComponentStore<TextureCoords4> _textureCellStore;

		ushort[] _indices   = new ushort[STARTING_CAPACITY];
		Vector3[] _vertices = new Vector3[STARTING_CAPACITY];
		Vector3[] _normals  = new Vector3[STARTING_CAPACITY];
		Vector2[] _uvs      = new Vector2[STARTING_CAPACITY];

		public ChunkMeshGenerator(Game game)
		{
			_meshManager = game.Processors.GetOrThrow<MeshManager>();
			_chunkStore       = (LookupDictionaryStore<ChunkPos, Chunk>)game.Components.GetStore<Chunk>();
			_storageStore     = game.Components.GetStore<ChunkPaletteStorage<Prototype>>();
			_textureCellStore = game.Components.GetStore<TextureCoords4>();
		}

		public Mesh? Generate(ChunkPos chunkPos)
		{
			var storages = new ChunkPaletteStorage<Prototype>[3, 3, 3];
			foreach (var (x, y, z) in Neighbors.ALL.Prepend(Neighbor.None))
				if (_chunkStore.TryGetEntityID(chunkPos.Add(x, y, z), out var neighborID))
					if (_storageStore.TryGet(neighborID, out var storage))
						storages[x+1, y+1, z+1] = storage;
			var centerStorage = storages[1, 1, 1];

			var indexCount  = 0;
			var vertexCount = 0;
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = centerStorage[x, y, z].Value;
				if (block.IsNone) continue;

				var blockVertex = new Vector3(x, y, z);
				_textureCellStore.TryGet(block.ID, out var textureCell);

				foreach (var facing in BlockFacings.ALL) {
					if (!IsNeighborEmpty(storages, x, y, z, facing)) continue;

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

			return (indexCount > 0)
				? _meshManager.Create(
					_indices.AsSpan(0, indexCount), _vertices.AsSpan(0, vertexCount),
					_normals.AsSpan(0, vertexCount), _uvs.AsSpan(0, vertexCount))
				: (Mesh?)null;
		}

		static bool IsNeighborEmpty(
			ChunkPaletteStorage<Prototype>[,,] storages,
			int x, int y, int z, BlockFacing facing)
		{
			var cx = 1; var cy = 1; var cz = 1;
			switch (facing) {
				case BlockFacing.East  : x += 1; if (x >= 16) cx += 1; break;
				case BlockFacing.West  : x -= 1; if (x <   0) cx -= 1; break;
				case BlockFacing.Up    : y += 1; if (y >= 16) cy += 1; break;
				case BlockFacing.Down  : y -= 1; if (y <   0) cy -= 1; break;
				case BlockFacing.South : z += 1; if (z >= 16) cz += 1; break;
				case BlockFacing.North : z -= 1; if (z <   0) cz -= 1; break;
			}
			return storages[cx, cy, cz]?[x & 0b1111, y & 0b1111, z & 0b1111].Value.IsNone ?? true;
		}
	}
}
