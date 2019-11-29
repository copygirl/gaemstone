using System;
using System.Collections.Generic;

namespace gaemstone.Common.ECS
{
	public class PackedArrayComponentStore<T>
			: IComponentStore<T>
		where T : struct
	{
		private const int STARTING_CAPACITY = 256;

		private uint[] _entities = new uint[STARTING_CAPACITY];
		private T[] _components = new T[STARTING_CAPACITY];
		private readonly Dictionary<uint, int> _indices
			= new Dictionary<uint, int>();

		public Type ComponentType { get; } = typeof(T);

		public int Count { get; private set; }

		public int Capacity {
			get => _components.Length;
			set => Resize(value);
		}


		public ref T GetComponentByIndex(int index)
			=> ref _components[index];

		public uint GetEntityIDByIndex(int index)
			=> _entities[index];

		public void RemoveByIndex(int index)
		{
			if ((index < 0) || (index >= Count)) throw new ArgumentOutOfRangeException(nameof(index));
			if (!_indices.Remove(_entities[index])) throw new Exception($"{_entities[index]} not found in _indices");
			if (index == --Count) return;
			_entities[index] = _entities[Count];
			_components[index] = _components[Count];
			_indices[_entities[index]] = index;
		}

		public bool TryFindIndex(uint entityID, out int index)
			=> _indices.TryGetValue(entityID, out index);
		public int? FindIndex(uint entityID)
			=> TryFindIndex(entityID, out var index) ? index : (int?)null;


		public ref T Get(uint entityID)
		{
			if (!TryFindIndex(entityID, out var index)) throw new InvalidOperationException();
			return ref _components[index];
		}

		public void Set(uint entityID, T component)
		{
			if (!TryFindIndex(entityID, out var index)) {
				index = Count++;
				if (Count > Capacity)
					Resize(Capacity << 1);
				_entities[index] = entityID;
			}
			_components[index] = component;
			_indices[entityID] = index;
		}

		public void Remove(uint entityID)
		{
			if (!TryFindIndex(entityID, out var index)) throw new InvalidOperationException(
				$"No component associated with entity ID {entityID}");
			RemoveByIndex(index);
		}


		private void Resize(int newCapacity)
		{
			if (newCapacity < 0) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "Capacity must be 0 or positive");

			if (newCapacity < Count) {
				for (int i = newCapacity; i < Count; i++)
					_indices.Remove(_entities[i]);
				Count = newCapacity;
			}

			var newEntities = new uint[newCapacity];
			var newComponents = new T[newCapacity];

			var copyCount = Math.Min(Capacity, newCapacity);
			Buffer.BlockCopy(_entities, 0, newEntities, 0, copyCount);
			Buffer.BlockCopy(_components, 0, newComponents, 0, copyCount);

			_entities = newEntities;
			_components = newComponents;
		}
	}
}
