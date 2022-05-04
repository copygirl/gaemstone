using System;
using System.Collections.Generic;
using System.Numerics;

namespace gaemstone.ECS
{
	internal struct Record
	{
		public ushort Generation;
		public Table Table;
		public int Row;

		public bool Occupied => (Table != null);
		public EntityType Type => Table.Type;
	}

	public class EntityManager
	{
		const int InitialCapacity = 1024;

		readonly Universe _universe;
		readonly Queue<uint> _unusedEntityIds = new();
		Record[] _entities = new Record[InitialCapacity];
		// TODO: Attempt to keep Generation low by prioritizing smallest Generation?
		uint _nextUnusedId = 1;

		public int Count { get; private set; }
		public int Capacity => _entities.Length;


		internal EntityManager(Universe universe)
			=> _universe = universe;

		void Resize(int newCapacity)
		{
			if (newCapacity < Capacity) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "New capacity must be larger than previous");
			Array.Resize(ref _entities, newCapacity);
		}

		void EnsureCapacity(uint minCapacity)
		{
			if (minCapacity < Capacity) return; // Already have the necessary capacity.
			Resize((int)BitOperations.RoundUpToPowerOf2(minCapacity));
		}


		/// <summary> Creates a new entity with an empty type and returns it. </summary>
		public Universe.Entity New() => New(_universe.EmptyType);
		/// <summary> Creates a new entity with the specified type and returns it. </summary>
		public Universe.Entity New(params object[] ids) => New(_universe.Type(ids));
		/// <summary> Creates a new entity with the specified type and returns it. </summary>
		public Universe.Entity New(params EcsId[] ids) => New(_universe.Type(ids));
		/// <summary> Creates a new entity with the specified type and returns it. </summary>
		public Universe.Entity New(EntityType type)
		{
			ref var record = ref NewRecord(type, out var entityId);
			return new(_universe, new(entityId, record.Generation));
		}

		/// <summary> Creates a new entity with the specifiedid and an empty type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		public Universe.Entity NewWithId(uint entityId) => NewWithId(entityId, _universe.EmptyType);
		/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		public Universe.Entity NewWithId(uint entityId, params object[] ids) => NewWithId(entityId, _universe.Type(ids));
		/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		public Universe.Entity NewWithId(uint entityId, params EcsId[] ids) => NewWithId(entityId, _universe.Type(ids));
		/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		public Universe.Entity NewWithId(uint entityId, EntityType type)
		{
			ref var record = ref NewRecordWithId(type, entityId);
			return new(_universe, new(entityId, record.Generation));
		}

		/// <summary> Creates a new entity with and returns a reference to the record. </summary>
		internal ref Record NewRecord(EntityType type, out uint entityId)
		{
			// Try to reuse a previously used entity id.
			if (!_unusedEntityIds.TryDequeue(out entityId)) {
				// If none are available, get the next fresh entity id.
				// And resize the entities array if necessary.
				do {
					EnsureCapacity(_nextUnusedId + 1);
					entityId = _nextUnusedId++;
				} while (GetRecord(entityId).Occupied);
			}
			return ref NewRecordWithId(type, entityId);
		}

		/// <summary> Creates a new entity with the specified id and returns a reference to the record. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		internal ref Record NewRecordWithId(EntityType type, uint entityId)
		{
			EnsureCapacity(entityId + 1);

			ref var record = ref GetRecord(entityId);
			if (record.Occupied) throw new EntityExistsException(entityId,
				$"Entity already exists as {new EcsId.Entity(entityId, record.Generation)}");

			record.Table = _universe.Tables.GetOrCreate(type);
			record.Row   = record.Table.Add(new(entityId, record.Generation));

			Count++;
			return ref record;
		}


		public void Delete(EcsId.Entity entity)
		{
			ref var record = ref GetRecord(entity);

			_unusedEntityIds.Enqueue(entity.Id);
			record.Table.Remove(record.Row);

			record.Generation++;
			record.Table = null!;

			Count--;
		}


		/// <summary> Returns the record for the specified id. </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified id is out of range. </exception>
		internal ref Record GetRecord(uint entityId)
		{
			if ((entityId == 0) || (entityId >= _entities.Length)) throw new EntityNotFoundException(
				entityId, $"Specified entity id is outside of valid range (1 to {_entities.Length - 1})");
			return ref _entities[entityId];
		}

		/// <summary> Returns the record for the specified entity, which must be alive. </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified entity's id is out of range, or the entity is not alive. </exception>
		internal ref Record GetRecord(EcsId.Entity entity)
		{
			ref var record = ref GetRecord(entity.Id);
			if (!record.Occupied || (record.Generation != entity.Generation))
				throw new EntityNotFoundException(entity);
			return ref record;
		}


		public Universe.Entity Lookup(uint entityId)
			=> TryLookup(entityId, out var entity) ? entity
				: throw new EntityNotFoundException(entityId);
		public bool TryLookup(uint entityId, out Universe.Entity entity)
		{
			entity = default;
			if ((entityId == 0) || (entityId >= _nextUnusedId)) return false;
			ref var entry = ref _entities[entityId];
			if (!entry.Occupied) return false;

			entity = new(_universe, new(entityId, entry.Generation));
			return true;
		}


		public bool IsAlive(EcsId.Entity entity)
		{
			if (entity.Id >= _nextUnusedId) return false;
			ref var entry = ref _entities[entity.Id];
			return (entry.Occupied && (entry.Generation == entity.Generation));
		}
	}
}
