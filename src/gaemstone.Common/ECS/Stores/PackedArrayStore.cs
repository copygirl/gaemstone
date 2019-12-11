using System;
using System.Collections.Generic;

namespace gaemstone.Common.ECS.Stores
{
	public class PackedArrayStore<T>
			: IComponentRefStore<T>
		where T : struct
	{
		private const int STARTING_CAPACITY = 256;

		private readonly Dictionary<uint, int> _indices
			= new Dictionary<uint, int>();

		protected uint[] EntityIDs { get; private set; }
		protected T[] Components { get; private set; }
		protected IReadOnlyDictionary<uint, int> Indices { get; }

		public Type ComponentType { get; } = typeof(T);
		public int Count { get; private set; }

		public int Capacity {
			get => Components.Length;
			set => Resize(value);
		}

		public T this[int index] {
			get => Components[index];
			set => Components[index] = value;
		}

		public event ComponentAddedHandler? ComponentAdded;
		public event ComponentRemovedHandler? ComponentRemoved;

		public PackedArrayStore()
		{
			EntityIDs  = new uint[STARTING_CAPACITY];
			Components = new T[STARTING_CAPACITY];
			Indices    = _indices;
		}


		public ref T GetRefByIndex(int index)
			=> ref Components[index];

		public uint GetEntityIDByIndex(int index)
			=> EntityIDs[index];


		public void RemoveByIndex(int index)
		{
			if ((index < 0) || (index >= Count)) throw new ArgumentOutOfRangeException(nameof(index));

			var entityID = EntityIDs[index];
			if (!_indices.Remove(entityID)) throw new InvalidOperationException(
				$"{EntityIDs[index]} not found in Indices"); // Shouldn't occur.

			ComponentRemoved?.Invoke(entityID);

			if (index == --Count) {
				EntityIDs[index]  = default;
				Components[index] = default;
			} else {
				EntityIDs[index]  = EntityIDs[Count];
				Components[index] = Components[Count];
				_indices[EntityIDs[index]] = index;
			}
		}

		public bool TryFindIndex(uint entityID, out int index)
			=> _indices.TryGetValue(entityID, out index);
		public int? FindIndex(uint entityID)
			=> _indices.TryGetValue(entityID, out var index) ? index : (int?)null;
		public int FindIndexOrThrow(uint entityID)
			=> _indices.TryGetValue(entityID, out var index) ? index
				: throw new ComponentNotFoundException(this, entityID);

		public int GetOrCreateIndex(uint entityID)
		{
			var added = false;
			if (!TryFindIndex(entityID, out var index)) {
				added = true;
				index = Count++;
				// Ensure we have the capacity to add another entry.
				if (Count > Capacity) Resize(Capacity << 1);
				// Associate the entry at the new index with this entity ID.
				EntityIDs[index] = entityID;
			}
			_indices[entityID] = index;
			if (added) ComponentAdded?.Invoke(entityID);
			return index;
		}


		public T Get(uint entityID)
			=> this[FindIndexOrThrow(entityID)];

		public ref T GetRef(uint entityID)
			=> ref Components[FindIndexOrThrow(entityID)];
		public void Set(uint entityID, T value)
			=> this[GetOrCreateIndex(entityID)] = value;

		public void Remove(uint entityID)
			=> RemoveByIndex(FindIndexOrThrow(entityID));


		private void Resize(int newCapacity)
		{
			if (newCapacity < 0) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "Capacity must be 0 or positive");

			if (newCapacity < Count) {
				for (int i = newCapacity; i < Count; i++) {
					var entityID = EntityIDs[i];
					_indices.Remove(entityID);
					ComponentRemoved?.Invoke(entityID);
				}
				Count = newCapacity;
			}

			var newEntities   = new uint[newCapacity];
			var newComponents = new T[newCapacity];

			var copyCount = Math.Min(Capacity, newCapacity);
			Buffer.BlockCopy(EntityIDs, 0, newEntities, 0, copyCount * sizeof(uint));
			// FIXME: "Object must be an array of primitives." - Find another way to do fast copy.
			// Buffer.BlockCopy(_components, 0, newComponents, 0, copyCount);
			Array.Copy(Components, newComponents, copyCount);

			EntityIDs  = newEntities;
			Components = newComponents;
		}


		public IComponentRefStore<T>.Enumerator GetEnumerator()
			=> new Enumerator(this);
		IComponentStore<T>.Enumerator IComponentStore<T>.GetEnumerator()
			=> GetEnumerator();

		private struct Enumerator
			: IComponentRefStore<T>.Enumerator
		{
			private readonly PackedArrayStore<T> _store;
			private int _index;

			public Enumerator(PackedArrayStore<T> store)
				=> (_store, _index) = (store, -1);

			public uint CurrentEntityID => _store.EntityIDs[_index];
			public ref T CurrentComponent => ref _store.Components[_index];
			T IComponentStore<T>.Enumerator.CurrentComponent => _store.Components[_index];
			public bool MoveNext() => (++_index < _store.Count);
		}
	}
}
