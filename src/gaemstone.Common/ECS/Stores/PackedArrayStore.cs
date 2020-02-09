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
		private uint[] _entityIDs;
		private T[] _components;

		protected IReadOnlyDictionary<uint, int> Indices { get; }
		protected uint[] EntityIDs => _entityIDs;
		protected T[] Components => _components;

		public Type ComponentType { get; } = typeof(T);
		public int Count { get; private set; }

		public int Capacity {
			get => _components.Length;
			set => Resize(value);
		}

		public T this[int index] {
			get => _components[index];
			set => _components[index] = value;
		}

		public event ComponentAddedHandler? ComponentAdded;
		public event ComponentRemovedHandler? ComponentRemoved;

		public PackedArrayStore()
		{
			Indices     = _indices;
			_entityIDs  = new uint[STARTING_CAPACITY];
			_components = new T[STARTING_CAPACITY];
		}


		public ref T GetRefByIndex(int index)
			=> ref _components[index];

		public uint GetEntityIDByIndex(int index)
			=> _entityIDs[index];


		public void RemoveByIndex(int index)
		{
			if ((index < 0) || (index >= Count)) throw new ArgumentOutOfRangeException(nameof(index));

			var entityID = _entityIDs[index];
			if (!_indices.Remove(entityID)) throw new InvalidOperationException(
				$"{_entityIDs[index]} not found in Indices"); // Shouldn't occur.

			ComponentRemoved?.Invoke(entityID);

			if (index == --Count) {
				_entityIDs[index]  = default;
				_components[index] = default;
			} else {
				_entityIDs[index]  = _entityIDs[Count];
				_components[index] = _components[Count];
				_indices[_entityIDs[index]] = index;
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
				_entityIDs[index] = entityID;
			}
			_indices[entityID] = index;
			if (added) ComponentAdded?.Invoke(entityID);
			return index;
		}


		public T Get(uint entityID)
			=> this[FindIndexOrThrow(entityID)];

		public bool TryGet(uint entityID, out T value)
		{
			var found = TryFindIndex(entityID, out var index);
			value = (found ? this[index] : default(T));
			return found;
		}

		public ref T GetRef(uint entityID)
			=> ref _components[FindIndexOrThrow(entityID)];

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
					var entityID = _entityIDs[i];
					_indices.Remove(entityID);
					ComponentRemoved?.Invoke(entityID);
				}
				Count = newCapacity;
			}

			Array.Resize(ref _entityIDs, newCapacity);
			Array.Resize(ref _components, newCapacity);
		}


		public IComponentRefStore<T>.Enumerator GetEnumerator()
			=> new Enumerator(this);
		IComponentStore<T>.Enumerator IComponentStore<T>.GetEnumerator()
			=> new Enumerator(this);
		IComponentStore.Enumerator IComponentStore.GetEnumerator()
			=> new Enumerator(this);

		private struct Enumerator
			: IComponentRefStore<T>.Enumerator
		{
			private readonly PackedArrayStore<T> _store;
			private int _index;

			public Enumerator(PackedArrayStore<T> store)
				=> (_store, _index) = (store, -1);

			public uint CurrentEntityID => _store._entityIDs[_index];
			public ref T CurrentComponent => ref _store._components[_index];
			T IComponentStore<T>.Enumerator.CurrentComponent => _store._components[_index];
			public bool MoveNext() => (++_index < _store.Count);
		}
	}
}
