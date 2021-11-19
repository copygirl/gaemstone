using System;
using System.Reflection;
using gaemstone.Common.Stores;

namespace gaemstone.Common
{
	public class ComponentManager
	{
		static readonly uint COMPONENT_ID  = 0x01;
		static readonly uint IDENTIFIER_ID = 0x02;

		readonly Universe _universe;
		readonly LookupDictionaryStore<Type, Component> _components = new(c => c.Type);

		public ComponentManager(Universe universe)
		{
			_universe = universe;

			universe.Entities.New(COMPONENT_ID);
			universe.Entities.New(IDENTIFIER_ID);
			_components.TryAdd(COMPONENT_ID, new(typeof(Component), _components));
			_components.TryAdd(IDENTIFIER_ID, new(typeof(Identifier), new DictionaryStore<Identifier>()));
			GetStore<Identifier>().TryAdd(COMPONENT_ID, new(nameof(Component)));
			GetStore<Identifier>().TryAdd(IDENTIFIER_ID, new(nameof(Identifier)));

			universe.Entities.OnEntityDestroyed += Clear;
		}

		public IComponentStore GetStore(Type componentType)
		{
			if (!_components.TryGetEntityID(componentType, out var entityID)) {
				// When the component does not yet have a
				if (componentType.GetCustomAttribute<StoreAttribute>() is not StoreAttribute attr)
					throw new InvalidOperationException($"No IComponentStore for type {componentType}");

				// Allow for shorthand specifying a generic type definition in StoreAttribute,
				// for example `PackedArrayStore<>` instead of `PackedArrayStore<Component>`.
				var storeType = attr.Type.IsGenericTypeDefinition
					? attr.Type.MakeGenericType(componentType) : attr.Type;

				var store = (IComponentStore)Activator.CreateInstance(storeType)!;
				if (store.ComponentType != componentType) throw new InvalidOperationException(
					$"StoreAttribute specified for {componentType} produced IComponentStore with mismatching ComponentType");
				AddStore(store);
				return store;
			}
			return _components.TryGet(entityID, out var component) ? component.Store
				: throw new InvalidOperationException(); // Should not occur.
		}

		public IComponentStore<T> GetStore<T>()
			=> (IComponentStore<T>)GetStore(typeof(T));

		public void AddStore(IComponentStore store)
		{
			if (_components.TryGetEntityID(store.ComponentType, out _)) throw new ArgumentException(
				$"Component type {store.ComponentType} already has an IComponentStore defined");
			var entity = _universe.Entities.New();
			GetStore<Component>().TryAdd(entity.ID, new(store.ComponentType, store));
			GetStore<Identifier>().TryAdd(entity.ID, new(store.ComponentType.Name));
		}

		void Clear(EcsId entity)
		{
			var allComponents = _components.GetEnumerator();
			while (allComponents.MoveNext()) {
				var component = allComponents.CurrentComponent;
				component.Store.TryRemove(entity.ID, out _);
			}
		}
	}
}
