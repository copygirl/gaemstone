using System;

namespace gaemstone.Common.ECS.Stores
{
	public interface IComponentStore
	{
		Type ComponentType { get; }
		int Count { get; }

		event ComponentAddedHandler? ComponentAdded;
		event ComponentRemovedHandler? ComponentRemoved;

		void Remove(uint entityID);

		Enumerator GetEnumerator();

		interface Enumerator
		{
			uint CurrentEntityID { get; }
			bool MoveNext();
		}
	}

	public interface IComponentStore<T>
		: IComponentStore
	{
		T Get(uint entityID);
		bool TryGet(uint entityID, out T value);
		void Set(uint entityID, T value);

		new Enumerator GetEnumerator();

		new interface Enumerator
			: IComponentStore.Enumerator
		{
			T CurrentComponent { get; }
		}
	}

	public interface IComponentRefStore<T>
		: IComponentStore<T>
	{
		ref T GetRef(uint entityID);

		new Enumerator GetEnumerator();

		new interface Enumerator
			: IComponentStore<T>.Enumerator
		{
			new ref T CurrentComponent { get; }
		}
	}

	public delegate void ComponentAddedHandler(uint entityID);
	public delegate void ComponentRemovedHandler(uint entityID);
	public delegate void ComponentChangedHandler<T>(uint entityID, ref T oldValue, ref T newValue);


	public class ComponentNotFoundException
		: InvalidOperationException
	{
		public IComponentStore Store { get; }
		public uint EntityID { get; }

		public ComponentNotFoundException(IComponentStore store, uint entityID)
			: base($"No component associated with entity ID {entityID}")
				=> (Store, EntityID) = (store, entityID);
	}
}
