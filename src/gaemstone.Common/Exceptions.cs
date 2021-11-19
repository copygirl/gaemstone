using System;

namespace gaemstone.Common
{
	public class ComponentNotFoundException : Exception
	{
		public Type ComponentType { get; }
		public object Entity { get; }

		public static string BuildMessage(Type componentType, object entity)
		{
			if (entity is uint id) entity = $"entity 0x{id:X}";
			return $"Component {componentType} not found on {entity}";
		}

		public ComponentNotFoundException(Type componentType, object entity)
			: base(BuildMessage(componentType, entity))
				=> (ComponentType, Entity) = (componentType, entity);
	}
}
