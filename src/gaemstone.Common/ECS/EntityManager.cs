using System;
using System.Collections.Generic;

namespace gaemstone.Common.ECS
{
	public class EntityManager
	{
		private const int STARTING_CAPACITY = 1024;

		private uint _nextUnusedID = 0;
		private Entry[] _entities = new Entry[STARTING_CAPACITY];
		private Queue<uint> _unusedEntityIDs = new Queue<uint>();
		// TODO: Attempt to keep Generation low by prioritizing smallest Generation?

		public int Count { get; private set; }
		public int Capacity => _entities.Length;

		public event Action<Entity>? OnEntityCreated;
		public event Action<Entity>? OnEntityDestroyed;
		public event Action<int>? OnCapacityChanged;


		public Entity New()
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
			Count++;

			var entity = new Entity(entityID, entry.Generation);
			OnEntityCreated?.Invoke(entity);
			return entity;
		}

		public void Destroy(in Entity entity)
		{
			if (entity.ID >= _nextUnusedID) throw new InvalidOperationException(
				$"Invalid (not yet used) entity ID {entity.ID}");
			ref var entry = ref _entities[entity.ID];
			if (!entry.Occupied || (entry.Generation != entity.Generation))
				throw new InvalidOperationException($"Entity {entity} is not alive");

			entry.Occupied = false;
			entry.Generation++;
			Count--;

			_unusedEntityIDs.Enqueue(entity.ID);
			OnEntityDestroyed?.Invoke(entity);
		}


		public Entity? GetByID(uint entityID)
		{
			if (entityID >= _nextUnusedID) return null;
			ref var entry = ref _entities[entityID];
			if (!entry.Occupied) return null;
			return new Entity(entityID, entry.Generation);
		}

		public bool IsAlive(in Entity entity)
		{
			if (entity.ID >= _nextUnusedID) return false;
			ref var entry = ref _entities[entity.ID];
			return (entry.Occupied && (entry.Generation == entity.Generation));
		}


		private void Resize(int newCapacity)
		{
			if (newCapacity < Capacity) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "New capacity must be larger than previous");

			var newEntities = new Entry[newCapacity];
			Buffer.BlockCopy(_entities, 0, newEntities, 0, Capacity);
			_entities = newEntities;

			OnCapacityChanged?.Invoke(Capacity);
		}


		private struct Entry
		{
			public uint Generation;
			public bool Occupied;
		}
	}
}