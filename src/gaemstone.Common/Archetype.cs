using System;
using System.Numerics;

namespace gaemstone.Common
{
	public class Archetype
	{
		const int INITIAL_CAPACITY = 64;

		public Universe Universe { get; }
		public EcsType Type { get; }

		// Note: Will be intialized by first call to Resize.
		internal EcsId[] Entities { get; private set; } = null!;
		internal Array[] Columns { get; private set; } = null!;

		public int Count { get; private set; }
		public int Capacity => Entities?.Length ?? 0;


		internal Archetype(Universe universe, EcsType type)
		{
			Universe = universe;
			Type     = type;
			Resize(INITIAL_CAPACITY);
		}


		public void Resize(int length)
		{
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
			if (length < Count) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be smaller than Count");

			if (Entities == null) {
				Entities = new EcsId[length];
				Columns  = new Array[Type.Count];
				for (var i = 0; i < Type.Count; i++)
					if (Universe.TryGet<Component>(Type[i], out var component))
						if (component.Type != null)
							Columns[i] = Array.CreateInstance(component.Type, length);
			} else {
				var newEntities = new EcsId[length];
				Array.Copy(Entities, newEntities, Math.Min(Entities.Length, length));
				Entities = newEntities;
				for (var i = 0; i < Type.Count; i++) {
					if (Columns![i] == null) continue;
					var elementType = Columns[i].GetType().GetElementType()!;
					var newColumn   = Array.CreateInstance(elementType, length);
					Array.Copy(Columns[i], newColumn, Math.Min(Columns[i].Length, length));
					Columns[i] = newColumn;
				}
			}
		}

		public void EnsureCapacity(int minCapacity)
		{
			if (minCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(minCapacity), "minCapacity must be positive");
			if (minCapacity <= Capacity) return; // Already have the necessary capacity.
			Resize((int)BitOperations.RoundUpToPowerOf2((uint)minCapacity));
		}


		internal int Add(EcsId entity)
		{
			EnsureCapacity(Count + 1);
			Entities![Count] = entity;
			return Count++;
		}

		internal void Remove(int row)
		{
			if (row >= Count) throw new ArgumentOutOfRangeException(nameof(row), "row cannot be greater or equal to Count");
			Count--;

			if (row < Count) {
				// Move the last element into the place of the removed one.
				Entities![row] = Entities[Count];
				foreach (var column in Columns!)
					if (column != null)
						Array.Copy(column, Count, column, row, 1);
				// Update the moved element's Record to point to its new row.
				ref var record = ref Universe.Entities.GetRecord(Entities[row].ID);
				record.Archetype = this;
				record.Row = row;
			}

			// Clear out the last element.
			Entities![Count] = default;
			foreach (var column in Columns!)
				if (column != null)
					Array.Clear(column, Count, 1);
		}
	}
}
