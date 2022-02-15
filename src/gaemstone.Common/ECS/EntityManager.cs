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
		public EcsType Type => Table.Type;
	}

	public class EntityManager
	{
		const int INITIAL_CAPACITY = 1024;

		readonly Universe _universe;
		readonly Queue<uint> _unusedEntityIDs = new();
		Record[] _entities = new Record[INITIAL_CAPACITY];
		// TODO: Attempt to keep Generation low by prioritizing smallest Generation?
		uint _nextUnusedID = 1;

		public int Count { get; private set; }
		public int Capacity => _entities.Length;


		internal EntityManager(Universe universe)
			=> _universe  = universe;

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
		public Universe.Entity New(EcsType type)
		{
			ref var record = ref NewRecord(type, out var entityID);
			return new(_universe, new(entityID, record.Generation));
		}

		/// <summary> Creates a new entity with the specified ID and an empty type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified ID is already in use. </exception>
		public Universe.Entity NewWithID(uint entityID) => NewWithID(entityID, _universe.EmptyType);
		/// <summary> Creates a new entity with the specified ID and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified ID is already in use. </exception>
		public Universe.Entity NewWithID(uint entityID, params object[] ids) => NewWithID(entityID, _universe.Type(ids));
		/// <summary> Creates a new entity with the specified ID and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified ID is already in use. </exception>
		public Universe.Entity NewWithID(uint entityID, params EcsId[] ids) => NewWithID(entityID, _universe.Type(ids));
		/// <summary> Creates a new entity with the specified ID and type and returns it. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified ID is already in use. </exception>
		public Universe.Entity NewWithID(uint entityID, EcsType type)
		{
			ref var record = ref NewRecordWithID(type, entityID);
			return new(_universe, new(entityID, record.Generation));
		}

		/// <summary> Creates a new entity with and returns a reference to the record. </summary>
		internal ref Record NewRecord(EcsType type, out uint entityID)
		{
			// Try to reuse a previously used entity ID.
			if (!_unusedEntityIDs.TryDequeue(out entityID)) {
				// If none are available, get the next fresh entity ID.
				// And resize the entities array if necessary.
				do {
					EnsureCapacity(_nextUnusedID + 1);
					entityID = _nextUnusedID++;
				} while (GetRecord(entityID).Occupied);
			}
			return ref NewRecordWithID(type, entityID);
		}

		/// <summary> Creates a new entity with the specified ID and returns a reference to the record. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified ID is already in use. </exception>
		internal ref Record NewRecordWithID(EcsType type, uint entityID)
		{
			EnsureCapacity(entityID + 1);

			ref var record = ref GetRecord(entityID);
			if (record.Occupied) throw new EntityExistsException(entityID,
				$"Entity already exists as {new EcsId(entityID, record.Generation)}");

			record.Table = _universe.Tables.GetOrCreate(type);
			record.Row   = record.Table.Add(new(entityID, record.Generation));

			Count++;
			return ref record;
		}


		public void Delete(EcsId entity)
		{
			ref var record = ref GetRecord(entity);

			_unusedEntityIDs.Enqueue(entity.ID);
			record.Table.Remove(record.Row);

			record.Generation++;
			record.Table = null!;

			Count--;
		}


		/// <summary> Returns the record for the specified ID. </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified ID is out of range. </exception>
		internal ref Record GetRecord(uint entityID)
		{
			if ((entityID == 0) || (entityID >= _entities.Length)) throw new EntityNotFoundException(
				entityID, $"Specified entity ID is outside of valid range (1 to {_entities.Length - 1})");
			return ref _entities[entityID];
		}

		/// <summary> Returns the record for the specified entity, which must be alive. </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified ID is out of range, or the entity is not alive. </exception>
		internal ref Record GetRecord(EcsId entity)
		{
			ref var record = ref GetRecord(entity.ID);
			if (!record.Occupied || (record.Generation != entity.Generation))
				throw new EntityNotFoundException(entity);
			return ref record;
		}


		public Universe.Entity Lookup(uint entityID)
			=> TryLookup(entityID, out var entity) ? entity
				: throw new EntityNotFoundException(entityID);
		public bool TryLookup(uint entityID, out Universe.Entity entity)
		{
			entity = default;
			if ((entityID == 0) || (entityID >= _nextUnusedID)) return false;
			ref var entry = ref _entities[entityID];
			if (!entry.Occupied) return false;

			entity = new(_universe, new(entityID, entry.Generation));
			return true;
		}


		public bool IsAlive(EcsId entity)
		{
			if (entity.ID >= _nextUnusedID) return false;
			ref var entry = ref _entities[entity.ID];
			return (entry.Occupied && (entry.Generation == entity.Generation));
		}
	}
}
