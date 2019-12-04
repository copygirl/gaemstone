using System;
using System.Collections.Generic;
using gaemstone.Common.ECS.Stores;

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
			entities.OnEntityDestroyed += Clear;
		}

		public int AddStore(IComponentStore store)
		{
			if (_byType.ContainsKey(store.ComponentType)) throw new ArgumentException(
				$"Component type {store.ComponentType} is already being tracked");
			if (Count == MAX_COMPONENT_TYPES) throw new InvalidOperationException(
				$"Current component type count is already at maximum ({MAX_COMPONENT_TYPES})");

			var storeIndex = Count;
			_stores.Add(store);
			_byType.Add(store.ComponentType, storeIndex);

			store.OnComponentAdded   += entityID => Set(entityID, storeIndex);
			store.OnComponentRemoved += entityID => Unset(entityID, storeIndex);

			return storeIndex;
		}

		public IComponentStore GetStore(int storeIndex)
			=> _stores[storeIndex];
		public IComponentStore<T> GetStore<T>()
			=> (IComponentStore<T>)GetStore(_byType[typeof(T)]);


		public int GetFlags(uint entityID)
			=> _components[entityID];

		public bool Has(uint entityID, int storeIndex)
			=> (GetFlags(entityID) & (1 << storeIndex)) != 0;
		public bool Has<T>(uint entityID)
			=> Has(entityID, _byType[typeof(T)]);

		public void Set(uint entityID, int storeIndex)
			=> _components[entityID] |= (1 << storeIndex);
		public void Set<T>(uint entityID)
			=> Set(entityID, _byType[typeof(T)]);

		public void Unset(uint entityID, int storeIndex)
			=> _components[entityID] &= ~(1 << storeIndex);
		public void Unset<T>(uint entityID)
			=> Unset(entityID, _byType[typeof(T)]);


		private void Resize(int newCapacity)
		{
			if (newCapacity < _components.Length) throw new ArgumentOutOfRangeException(
				nameof(newCapacity), newCapacity, "New capacity must be larger than previous");

			var newComponents = new int[newCapacity];
			Buffer.BlockCopy(_components, 0, newComponents, 0, _components.Length);
			_components = newComponents;
		}

		private void Clear(Entity entity)
		{
			var flags = GetFlags(entity.ID);
			for (var i = 0; i < _stores.Count; i++) {
				if ((flags & 1) != 0)
					_stores[i].Remove(entity.ID);
				// TODO: Remove fires OnComponentRemoved, which we don't need to process.
				flags >>= 1;
			}
			_components[entity.ID] = 0;
		}
	}
}
