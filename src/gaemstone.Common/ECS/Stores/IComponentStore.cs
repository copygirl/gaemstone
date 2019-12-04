using System;

namespace gaemstone.Common.ECS.Stores
{
	public interface IComponentStore
	{
		Type ComponentType { get; }

		event Action<uint>? OnComponentAdded;
		event Action<uint>? OnComponentRemoved;

		void Remove(uint entityID);
	}

	public interface IComponentStore<T>
		: IComponentStore
	{
		T Get(uint entityID);
		void Set(uint entityID, T value);

		Enumerator GetEnumerator();

		interface Enumerator
		{
			uint CurrentEntityID { get; }
			T CurrentComponent { get; }
			bool MoveNext();
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
