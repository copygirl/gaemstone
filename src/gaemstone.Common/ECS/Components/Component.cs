using System;
using gaemstone.Common.ECS.Stores;

namespace gaemstone.Common.ECS
{
	public readonly struct Component
	{
		public readonly Type Type;
		public readonly IComponentStore Store;
		public Component(Type type, IComponentStore store)
			{ Type = type; Store = store; }
	}
}
