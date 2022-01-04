using System;

namespace gaemstone.Common
{
	public readonly struct Component
	{
		public readonly Type Type;
		public Component(Type type)
			=> Type = type;
	}
}
