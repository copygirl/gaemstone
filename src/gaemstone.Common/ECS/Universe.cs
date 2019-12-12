using gaemstone.Common.ECS.Processors;

namespace gaemstone.Common.ECS
{
	public class Universe
	{
		public EntityManager Entities { get; }
		public ComponentManager Components { get; }
		public ProcessorManager Processors { get; }

		public Universe()
		{
			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Processors = new ProcessorManager(this);
		}
	}
}
