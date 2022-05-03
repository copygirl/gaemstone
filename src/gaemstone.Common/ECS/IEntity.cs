using System.Diagnostics.CodeAnalysis;

namespace gaemstone.ECS
{
	public interface IEntity
	{
		Universe Universe { get; }
		EntityType Type { get; set; }
		bool IsAlive { get; }

		void Delete();

		bool Has(EcsId id) => Type.Contains(id);

		bool TryGet<T>([NotNullWhen(true)] out T? value);
		bool TrySet<T>(T value);

		T Get<T>();
		IEntity Set<T>(T value);
	}

	public static class EntityExtensions
	{
		public static TEntity Add<TEntity>(this TEntity entity, params object[] ids)
			where TEntity : IEntity { entity.Type = entity.Type.Union(ids); return entity; }
		public static TEntity Add<TEntity>(this TEntity entity, params EcsId[] ids)
			where TEntity : IEntity { entity.Type = entity.Type.Union(ids); return entity; }

		public static TEntity Remove<TEntity>(this TEntity entity, params object[] ids)
			where TEntity : IEntity { entity.Type = entity.Type.Except(ids); return entity; }
		public static TEntity Remove<TEntity>(this TEntity entity, params EcsId[] ids)
			where TEntity : IEntity { entity.Type = entity.Type.Except(ids); return entity; }
	}
}
