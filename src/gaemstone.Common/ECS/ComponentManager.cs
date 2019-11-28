using System;
using System.Collections.Generic;

namespace gaemstone.Common.ECS
{
	public class ComponentManager
	{
		public static readonly int MAX_COMPONENT_TYPES = 32;

		private int[] _components = null!; // Set in the first call to Resize.
		private List<IComponentStore> _stores
			= new List<IComponentStore>(MAX_COMPONENT_TYPES);
		private Dictionary<Type, int> _byType
			= new Dictionary<Type, int>(MAX_COMPONENT_TYPES);

		public int Count => _stores.Count;


		public ComponentManager(EntityManager entities)
		{
			_components = new int[entities.Capacity];
			entities.OnCapacityChanged += Resize;
		}


		public int AddStore(IComponentStore store)
		{
			if (_byType.ContainsKey(store.ComponentType)) throw new ArgumentException(
				$"Component type {store.ComponentType} is already being tracked");
			if (Count == MAX_COMPONENT_TYPES) throw new InvalidOperationException(
				$"Current component type count is already at maximum ({MAX_COMPONENT_TYPES})");

			var index = Count;
			_stores.Add(store);
			_byType.Add(store.ComponentType, index);
			return index;
		}

		public IComponentStore GetStore(int index)
			=> _stores[index];
		public IComponentStore<T> GetStore<T>()
			=> (IComponentStore<T>)GetStore(_byType[typeof(T)]);


		public int GetFlags(uint entityID)
			=> _components[entityID];

		public bool Has(uint entityID, int index)
			=> (GetFlags(entityID) & (1 << index)) != 0;
		public bool Has<T>(uint entityID)
			=> Has(entityID, _byType[typeof(T)]);

		public void Set(uint entityID, int index, bool value)
		{
			if (value) _components[entityID] |= (1 << index);
			else _components[entityID] &= ~(1 << index);
		}
		public void Set<T>(uint entityID, bool value)
			=> Set(entityID, _byType[typeof(T)], value);


		private void Resize(int newCapacity)
		{
			if (newCapacity < _components.Length) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "New capacity must be larger than previous");

			var newComponents = new int[newCapacity];
			Buffer.BlockCopy(_components, 0, newComponents, 0, _components.Length);
			_components = newComponents;
		}
	}
}
