using System.Diagnostics.CodeAnalysis;

namespace gaemstone.ECS
{
	public interface IEntity
	{
		Universe Universe { get; }
		EcsId? ID { get; }

		bool IsAlive { get; }
		EcsType Type { get; set; }

		void Delete();

		bool Has(EcsId id) => Type.Contains(id);

		bool TryGet<T>([NotNullWhen(true)] out T? value);
		bool TrySet<T>(T value);

		T Get<T>();
		void Set<T>(T value);
	}
}
