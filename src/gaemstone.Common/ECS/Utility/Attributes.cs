using System;

namespace gaemstone.ECS
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ComponentAttribute : Attribute {  }

	[AttributeUsage(AttributeTargets.Struct)]
	public class TagAttribute : Attribute {  }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class RelationAttribute : Attribute {  }
}
