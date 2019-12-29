using System;
using System.Collections.Generic;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using gaemstone.Common.ECS.Stores;

namespace gaemstone.Common.Bloxel.Chunks
{
	public class ChunkTracker : IProcessor
	{
		private Universe _universe = null!;
		private PackedArrayStore<ChunkLoader> _loaderStore = null!;

		public void OnLoad(Universe universe)
		{
			_universe    = universe;
			_loaderStore = new PackedArrayStore<ChunkLoader>();
			_universe.Components.AddStore(_loaderStore);
		}

		public void OnUnload()
		{
			// TODO: When removing stores is supported, implement this?
			throw new NotSupportedException();
		}

		public void OnUpdate(double delta)
		{
			var enumerator = _loaderStore.GetEnumerator();
			while (enumerator.MoveNext()) {
				var entity      = _universe.Entities.GetByID(enumerator.CurrentEntityID)!.Value;
				var chunkLoader = enumerator.CurrentComponent;
				var transform   = _universe.Get<Transform>(entity);

				// TODO: Do stuff.
			}
		}
	}

	public readonly struct ChunkLoader
	{
		public int Distance { get; }

		public ChunkLoader(int distance)
		{
			if (distance <= 0) throw new ArgumentOutOfRangeException(
				nameof(distance), distance, "Distance must be positive");
			Distance = distance;
		}
	}

	public class ChunkLoadedOctree
	{
		private const int LEVELS = 4;
		private const int CHUNK_TO_METACHUNK_SHIFT = 1 << LEVELS;
		private static readonly int BYTES_PER_METACHUNK = 2;
		static ChunkLoadedOctree()
		{

		}

		private readonly Dictionary<(int, int, int), byte[]> _metachunks
			= new Dictionary<(int, int, int), byte[]>();

		private bool Get(ChunkPos pos)
		{
			var mx = pos.X >> CHUNK_TO_METACHUNK_SHIFT;
			var my = pos.Y >> CHUNK_TO_METACHUNK_SHIFT;
			var mz = pos.Z >> CHUNK_TO_METACHUNK_SHIFT;
			if (!_metachunks.TryGetValue((mx, my, mz), out var metachunk)) return false;

		}
	}
}
