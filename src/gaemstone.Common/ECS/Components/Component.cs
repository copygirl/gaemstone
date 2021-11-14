using System;

namespace gaemstone.Common.ECS
{
	public readonly struct Component
	{
		public readonly Type Type;
		public Component(Type value) => Type = value;
		public static Component Of<T>() => new(typeof(T));
	}
}
