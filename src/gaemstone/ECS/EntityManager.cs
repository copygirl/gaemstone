using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace gaemstone.ECS
{
	public class EntityManager
	{
		const int InitialCapacity = 1024;


		readonly Queue<uint> _unusedEntityIds = new();

		Record[] _entities = new Record[InitialCapacity];
		// TODO: Attempt to keep Generation low by prioritizing smallest Generation?
		uint _nextUnusedId = 1;

		public Universe Universe { get; }
		public int Count { get; private set; }
		public int Capacity => _entities.Length;


		internal EntityManager(Universe universe)
			=> Universe = universe;


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


		/// <summary>
		/// Returns a reference to the record for the specified entity id, or
		/// a <see cref="Unsafe.NullRef{Record}"/> if the id is out of range.
		/// The returned record may not contain an alive entity.
		/// </summary>
		public ref Record GetRecordOrNull(uint entityId)
			=> ref ((entityId > 0) && (entityId < _entities.Length))
				? ref _entities[entityId] : ref Unsafe.NullRef<Record>();

		/// <summary>
		/// Returns a reference to the record for the specified entity id.
		/// The returned record may not contain an alive entity.
		/// </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified id is out of range. </exception>
		public ref Record GetRecord(uint entityId)
		{
			ref var record = ref GetRecordOrNull(entityId);
			if (Unsafe.IsNullRef(ref record)) throw new EntityNotFoundException(
				entityId, $"Specified entity id is outside of valid range (1 to {_entities.Length - 1})");
			return ref record;
		}

		/// <summary>
		/// Returns a reference to the record for the specified entity, or a
		/// <see cref="Unsafe.NullRef{Record}"/> if the entity's id is out of
		/// range, or the entity is not alive.
		/// </summary>
		public ref Record GetRecordOrNull(EcsId.Entity entity)
		{
			ref var record = ref GetRecordOrNull(entity.Id);
			if (!Unsafe.IsNullRef(ref record)) {
				if (!record.Occupied || (record.Generation != entity.Generation))
					return ref Unsafe.NullRef<Record>();
			}
			return ref record;
		}

		/// <summary> Returns a reference to the record for the specified entity, which must be alive. </summary>
		/// <exception cref="EntityNotFoundException"> Thrown if the specified entity's id is out of range, or the entity is not alive. </exception>
		public ref Record GetRecord(EcsId.Entity entity)
		{
			ref var record = ref GetRecord(entity.Id);
			if (!record.Occupied || (record.Generation != entity.Generation))
				throw new EntityNotFoundException(entity);
			return ref record;
		}


		/// <summary> Returns whether the specified entity is currently alive. </summary>
		public bool IsAlive(EcsId.Entity entity)
		{
			if ((entity.Id == 0) || (entity.Id >= _nextUnusedId)) return false;
			ref var entry = ref _entities[entity.Id];
			return (entry.Occupied && (entry.Generation == entity.Generation));
		}


		/// <summary> Creates a new entity with the specified id and returns a reference to its record. </summary>
		public ref Record NewRecord(EntityType type, out uint entityId)
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

		/// <summary> Creates a new entity with the specified id and returns a reference to its record. </summary>
		/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
		public ref Record NewRecordWithId(EntityType type, uint entityId)
		{
			EnsureCapacity(entityId + 1);

			ref var record = ref GetRecord(entityId);
			if (record.Occupied) throw new EntityExistsException(entityId,
				$"Id is already occupied by {new EcsId.Entity(entityId, record.Generation)}");

			record.Table = Universe.Tables.GetOrCreate(type);
			record.Row   = record.Table.Add(new(entityId, record.Generation));

			Count++;
			return ref record;
		}


		/// <summary> Attempts to delete the specified entity and returns if successful. </summary>
		public bool Delete(EcsId.Entity entity)
		{
			ref var record = ref GetRecordOrNull(entity);
			if (Unsafe.IsNullRef(ref record)) return false;

			_unusedEntityIds.Enqueue(entity.Id);
			record.Table.Remove(record.Row);

			record.Generation++;
			record.Table = null!;

			Count--;
			return true;
		}
	}
}
