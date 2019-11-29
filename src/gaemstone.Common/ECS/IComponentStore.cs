using System;

namespace gaemstone.Common.ECS
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
	}
}
