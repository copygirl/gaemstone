using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace gaemstone.ECS
{
	public class Table
	{
		const int InitialCapacity = 16;


		public Universe Universe { get; }
		public EntityType Type { get; }

		public EntityType StorageType { get; }
		internal EcsId.Entity[] Entities { get; private set; }
		internal Array[] Columns { get; }

		public bool IsEmpty => Count == 0;
		public int Count { get; private set; }
		public int Capacity => Entities.Length;


		internal Table(Universe universe, EntityType type, EntityType storageType,
		               IEnumerable<Type> columnTypes)
		{
			Universe = universe;
			Type     = type;

			StorageType = storageType;
			Entities    = new EcsId.Entity[0];
			Columns     = columnTypes.Select(type => Array.CreateInstance(type, 0)).ToArray();
		}


		public void Resize(int length)
		{
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
			if (length < Count) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be smaller than Count");

			var newEntities = new EcsId.Entity[length];
			Array.Copy(Entities, newEntities, Math.Min(Entities.Length, length));
			Entities = newEntities;
			for (var i = 0; i < Columns.Length; i++) {
				var elementType = Columns[i].GetType().GetElementType()!;
				var newColumn   = Array.CreateInstance(elementType, length);
				Array.Copy(Columns[i], newColumn, Math.Min(Columns[i].Length, length));
				Columns[i] = newColumn;
			}
		}

		public void EnsureCapacity(int minCapacity)
		{
			if (minCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(minCapacity), "minCapacity must be positive");
			if (minCapacity <= Capacity) return; // Already have the necessary capacity.
			if (minCapacity < InitialCapacity) minCapacity = InitialCapacity;
			Resize((int)BitOperations.RoundUpToPowerOf2((uint)minCapacity));
		}


		internal int Add(EcsId.Entity entity)
		{
			EnsureCapacity(Count + 1);
			Entities[Count] = entity;
			return Count++;
		}

		internal void Remove(int row)
		{
			if (row >= Count) throw new ArgumentOutOfRangeException(nameof(row), "row cannot be greater or equal to Count");
			Count--;

			if (row < Count) {
				// Move the last element into the place of the removed one.
				Entities[row] = Entities[Count];
				foreach (var column in Columns)
					Array.Copy(column, Count, column, row, 1);
				// Update the moved element's Record to point to its new row.
				ref var record = ref Universe.Entities.GetRecord(Entities[row].Id);
				record.Table = this;
				record.Row   = row;
			}

			// Clear out the last element.
			Entities[Count] = default;
			foreach (var column in Columns)
				Array.Clear(column, Count, 1);
		}


		public T[] GetStorageColumn<T>(EcsId id, EcsId? entity = null)
			=> TryGetStorageColumn<T>(id, out var column) ? column
				: throw new ComponentNotFoundException(entity, typeof(T));

		public bool TryGetStorageColumn<T>(EcsId id, [NotNullWhen(true)] out T[]? value)
		{
			var index = StorageType.IndexOf(id);
			if (index >= 0) { value = (T[])Columns[index]; return true; }
			else { value = null; return false; }
		}
	}
}
