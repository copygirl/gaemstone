using System.Diagnostics.CodeAnalysis;
using gaemstone.ECS;

namespace gaemstone
{
	public interface IEntityRef
	{
		Universe Universe { get; }
		EntityType Type { get; set; }
		bool IsAlive { get; }

		void Delete();

		bool Has(EcsId id) => Type.Contains(id);

		bool TryGet<T>([NotNullWhen(true)] out T? value);
		bool TrySet<T>(T value);

		T Get<T>();
		IEntityRef Set<T>(T value);
	}

	public static class EntityRefExtensions
	{
		public static T Add<T>(this T entity, params object[] ids)
			where T : IEntityRef { entity.Type = entity.Type.Union(ids); return entity; }
		public static T Add<T>(this T entity, params EcsId[] ids)
			where T : IEntityRef { entity.Type = entity.Type.Union(ids); return entity; }

		public static T Remove<T>(this T entity, params object[] ids)
			where T : IEntityRef { entity.Type = entity.Type.Except(ids); return entity; }
		public static T Remove<T>(this T entity, params EcsId[] ids)
			where T : IEntityRef { entity.Type = entity.Type.Except(ids); return entity; }
	}
}
