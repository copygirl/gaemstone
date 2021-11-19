using System;

namespace gaemstone.Common.Stores
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class StoreAttribute : Attribute
	{
		public Type Type { get; }

		public StoreAttribute(Type? type = null)
		{
			if (type == null) type = typeof(PackedArrayStore<>);
			if (!typeof(IComponentStore).IsAssignableFrom(type)) throw new ArgumentException(
				$"The specified type {type} is not an IComponentStore");
			if (type.GetConstructor(Type.EmptyTypes) == null) throw new ArgumentException(
				$"{type} must define a public parameterless constructor");
			Type = type;
		}
	}
}
