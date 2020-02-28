namespace gaemstone.Common.ECS
{
	public interface IEntityRef
	{
		Universe Universe { get; }
		Entity Entity { get; }

		T Get<T>()
			=> Universe.Get<T>(Entity);
		bool Has<T>(Entity entity)
			=> Universe.Has<T>(Entity);
		void Set<T>(Entity entity, T value)
			=> Universe.Set<T>(Entity, value);
	}
}
