using System;

namespace gaemstone.Common.ECS
{
	public interface IComponentStore
	{
		Type ComponentType { get; }
	}

	public interface IComponentStore<T>
		: IComponentStore
	{
	}
}
