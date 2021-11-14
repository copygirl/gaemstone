using System;
using System.Collections.Generic;

namespace gaemstone.Common.ECS
{
	public class EntityManager
	{
		const int STARTING_CAPACITY = 1024;

		uint _nextUnusedID = 1;
		Entry[] _entities = new Entry[STARTING_CAPACITY];
		readonly Queue<uint> _unusedEntityIDs = new();
		// TODO: Attempt to keep Generation low by prioritizing smallest Generation?

		public int Count { get; private set; }
		public int Capacity => _entities.Length;

		public event Action<EcsId>? OnEntityCreated;
		public event Action<EcsId>? OnEntityDestroyed;
		public event Action<int>? OnCapacityChanged;


		public EcsId New()
		{
			// Try to reuse a previously used entity ID.
			if (!_unusedEntityIDs.TryDequeue(out var entityID)) {
				// If none are available, get the next fresh entity ID.
				// And resize the entities array if necessary.
				if (_nextUnusedID >= Capacity)
					Resize(Capacity << 1);
				entityID = _nextUnusedID++;
			}

			ref var entry  = ref _entities[entityID];
			entry.Occupied = true;
			entry.Generation++;
			Count++;

			var entity = new EcsId(entityID, entry.Generation);
			OnEntityCreated?.Invoke(entity);
			return entity;
		}

		public void Destroy(EcsId entity)
		{
			if (entity.ID >= _nextUnusedID) throw new InvalidOperationException(
				$"Entity {entity} is not alive (ID not yet assigned)");
			ref var entry = ref _entities[entity.ID];
			if (!entry.Occupied || (entry.Generation != entity.Generation))
				throw new InvalidOperationException($"Entity {entity} is not alive");

			entry.Occupied = false;
			Count--;

			_unusedEntityIDs.Enqueue(entity.ID);
			OnEntityDestroyed?.Invoke(entity);
		}


		public EcsId? Lookup(uint entityID)
		{
			if ((entityID == 0) || (entityID >= _nextUnusedID)) return null;
			ref var entry = ref _entities[entityID];
			if (!entry.Occupied) return null;
			return new(entityID, entry.Generation);
		}

		public bool IsAlive(EcsId entity)
		{
			if (entity.ID >= _nextUnusedID) return false;
			ref var entry = ref _entities[entity.ID];
			return (entry.Occupied && (entry.Generation == entity.Generation));
		}


		void Resize(int newCapacity)
		{
			if (newCapacity < Capacity) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "New capacity must be larger than previous");

			Array.Resize(ref _entities, newCapacity);
			OnCapacityChanged?.Invoke(Capacity);
		}


		struct Entry
		{
			public ushort Generation;
			public bool Occupied;
		}
	}
}
