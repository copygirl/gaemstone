using System;
using gaemstone.Common.Stores;

namespace gaemstone.Common
{
	public readonly struct Component
	{
		public readonly Type Type;
		public readonly IComponentStore Store;
		public Component(Type type, IComponentStore store)
			{ Type = type; Store = store; }
	}
}
