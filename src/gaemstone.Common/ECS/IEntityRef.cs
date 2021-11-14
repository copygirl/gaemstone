using System.Diagnostics.CodeAnalysis;

namespace gaemstone.Common.ECS
{
	public interface IEntityRef
	{
		Universe Universe { get; }
		EcsId Entity { get; }

		bool IsAlive => Universe.Entities.IsAlive(Entity);

		bool TryGet<T>([NotNullWhen(true)] out T value) => Universe.TryGet<T>(Entity, out value);
		T Get<T>() => TryGet<T>(out var value) ? value : throw new ComponentNotFoundException(typeof(T), this);
		bool Has<T>(EcsId entity) => TryGet<T>(out _);
		void Set<T>(EcsId entity, T value) => Universe.Set<T>(Entity, value);
		void Destroy() => Universe.Entities.Destroy(Entity);
	}

	public readonly struct SimpleEntityRef
	{
		public Universe Universe { get; }
		public EcsId Entity { get; }

		public SimpleEntityRef(Universe universe, EcsId entity)
			{ Universe = universe; Entity = entity; }
	}
}
