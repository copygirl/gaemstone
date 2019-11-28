using System;

namespace gaemstone.Common.ECS
{
	public class EntityManager
	{
		private const int STARTING_CAPACITY = 1024;

		private Entry[] _entities = new Entry[STARTING_CAPACITY];
		private uint _nextUnusedID = 0;

		public int Count { get; private set; }
		public int Capacity => _entities.Length;

		public event Action<int>? OnCapacityChanged;


		public Entity New()
		{
			// TODO: Reuse entities after they have been destroyed.

			if (_nextUnusedID >= Capacity)
				Resize(Capacity << 1);

			var entityID   = _nextUnusedID++;
			ref var entry  = ref _entities[entityID];
			entry.Occupied = true;
			Count++;
			return new Entity(entityID, entry.Generation);
		}

		public Entity? GetByID(uint entityID)
		{
			if (entityID >= Capacity) return null;
			ref var entry = ref _entities[entityID];
			if (!entry.Occupied) return null;
			return new Entity(entityID, entry.Generation);
		}

		public bool IsAlive(in Entity entity)
		{
			if (entity.ID >= Capacity) return false;
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
