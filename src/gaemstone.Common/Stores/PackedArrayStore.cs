using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace gaemstone.Common.Stores
{
	public class PackedArrayStore<T>
		: IComponentRefStore<T>
		where T : struct
	{
		const int STARTING_CAPACITY = 256;

		readonly Dictionary<uint, int> _indices = new();
		uint[] _entityIDs;
		T[] _components;

		protected IReadOnlyDictionary<uint, int> Indices { get; }
		protected uint[] EntityIDs => _entityIDs;
		protected T[] Components => _components;

		public Type ComponentType => typeof(T);
		public int Count { get; private set; }

		public int Capacity {
			get => _components.Length;
			set => Resize(value);
		}

		public T this[int index] {
			get => _components[index];
			set => _components[index] = value;
		}

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
			=> _indices.TryGetValue(entityID, out var index) ? index : null;

		public int GetOrCreateIndex(uint entityID)
		{
			if (!TryFindIndex(entityID, out var index)) {
				index = Count++;
				// Ensure we have the capacity to add another entry.
				if (Count > Capacity) Resize(Capacity << 1);
				// Associate the entry at the new index with this entity ID.
				_entityIDs[index] = entityID;
			}
			_indices[entityID] = index;
			return index;
		}


		public bool Has(uint entityID)
			=> TryFindIndex(entityID, out _);

		public bool TryGet(uint entityID, [NotNullWhen(true)] out T value)
		{
			var found = TryFindIndex(entityID, out var index);
			value = (found ? this[index] : default);
			return found;
		}

		public ref T TryGetRef(uint entityID)
			=> ref TryFindIndex(entityID, out var index)
				? ref _components[index] : ref Unsafe.NullRef<T>();

		public ref T GetOrCreateRef(uint entityID)
		{
			// Must be two lines because _components
			// might be resized by GetOrCreateIndex.
			var index = GetOrCreateIndex(entityID);
			return ref _components[index];
		}

		public bool TryAdd(uint entityID, T value)
		{
			var previousCount = Count;
			ref var entry = ref GetOrCreateRef(entityID);
			if (Count > previousCount) return false;
			entry = value;
			return true;
		}

		public bool Set(uint entityID, T value, [NotNullWhen(true)] out T previous)
		{
			var previousCount = Count;
			ref var entry = ref GetOrCreateRef(entityID);
			previous = entry;
			entry = value;
			return (Count == previousCount);
		}

		public bool TryRemove(uint entityID, [NotNullWhen(true)] out T previous)
		{
			if (TryFindIndex(entityID, out var index)) {
				previous = _components[index];
				RemoveByIndex(index);
				return true;
			} else {
				previous = default;
				return false;
			}
		}


		void Resize(int newCapacity)
		{
			if (newCapacity < 0) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "Capacity must be 0 or positive");
			if (newCapacity < Count)
				throw new ArgumentOutOfRangeException(
					nameof(newCapacity), newCapacity, "Capacity must be greater or equal to Count");
			// for (int i = newCapacity; i < Count; i++) {
			// 	var entityID = _entityIDs[i];
			// 	_indices.Remove(entityID);
			// }
			// Count = newCapacity;

			Array.Resize(ref _entityIDs, newCapacity);
			Array.Resize(ref _components, newCapacity);
		}


		public IComponentRefStore<T>.IEnumerator GetEnumerator() => new Enumerator(this);
		IComponentStore<T>.IEnumerator IComponentStore<T>.GetEnumerator() => new Enumerator(this);
		IComponentStore.IEnumerator IComponentStore.GetEnumerator() => new Enumerator(this);

		struct Enumerator
			: IComponentRefStore<T>.IEnumerator
		{
			readonly PackedArrayStore<T> _store;
			int _index;

			public Enumerator(PackedArrayStore<T> store)
				=> (_store, _index) = (store, -1);

			object IComponentStore.IEnumerator.CurrentComponent => _store._components[_index];
			T IComponentStore<T>.IEnumerator.CurrentComponent => _store._components[_index];
			public ref T CurrentComponent => ref _store._components[_index];
			public uint CurrentEntityID => _store._entityIDs[_index];
			public bool MoveNext() => (++_index < _store.Count);
		}
	}
}
