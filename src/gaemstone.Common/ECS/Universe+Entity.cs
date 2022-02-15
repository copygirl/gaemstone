using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		public readonly struct Entity
			: IEntity, IEquatable<Entity>
		{
			public Universe Universe { get; }
			public EcsId ID { get; }
			EcsId? IEntity.ID => ID;

			public bool IsAlive => Universe.Entities.IsAlive(this);
			public EcsType Type {
				get => Universe.Entities.GetRecord(this).Type;
				set => Universe.Tables.Move(this, ref Universe.Entities.GetRecord(this), value);
			}

			public Entity(Universe universe, EcsId id)
				{ Universe = universe; ID = id; }

			public void Delete() => Universe.Entities.Delete(this);

			public bool Has(EcsId id) => Type.Contains(id);

			public bool TryGet<T>([NotNullWhen(true)] out T? value) => Universe.TryGet(this, out value);
			public bool TrySet<T>(T value) => Universe.TrySet(this, value);

			public T Get<T>() => Universe.Get<T>(this);
			public ref T GetRef<T>() => ref Universe.GetRef<T>(this);
			public void Set<T>(T value) => Universe.Set<T>(this, value);


			public override bool Equals(object? obj) => (obj is Entity entity) && Equals(entity);
			public bool Equals(Entity other) => (Universe == other.Universe) && (ID == other.ID);
			public override int GetHashCode() => HashCode.Combine(Universe, ID);

			public override string? ToString() => ID.ToString(Universe);
			public void AppendString(StringBuilder builder) => ID.AppendString(builder);

			public static implicit operator EcsId(Entity entity) => entity.ID;
		}
	}
}
