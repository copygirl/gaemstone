using System;
using System.Diagnostics.CodeAnalysis;
using gaemstone.ECS;

namespace gaemstone
{
	public partial class Universe
	{
		public readonly struct Entity
			: IEntityRef, IEquatable<Entity>
		{
			public Universe Universe { get; }
			public EcsId.Entity Id { get; }

			public bool IsAlive => Universe.Entities.IsAlive(this);
			public EntityType Type {
				get => Universe.Entities.GetRecord(this).Type;
				set => Universe.Tables.Move(this, ref Universe.Entities.GetRecord(this), value);
			}

			public Entity(Universe universe, EcsId.Entity id)
				{ Universe = universe; Id = id; }

			public void Delete() => Universe.Entities.Delete(this);

			public bool Has(EcsId id) => Type.Contains(id);

			public bool TryGet<T>([NotNullWhen(true)] out T? value) => Universe.TryGet(this, out value);
			public bool TrySet<T>(T value) => Universe.TrySet(this, value);

			public T Get<T>() => Universe.Get<T>(this);
			public ref T GetRef<T>() => ref Universe.GetRef<T>(this);
			public Entity Set<T>(T value) { Universe.Set(this, value); return this; }
			IEntityRef IEntityRef.Set<T>(T value) => Set(value);

			public override bool Equals(object? obj) => (obj is Entity entity) && Equals(entity);
			public bool Equals(Entity other) => (Universe == other.Universe) && (Id == other.Id);
			public override int GetHashCode() => HashCode.Combine(Universe, Id);

			// TODO: Do the ToString stuff.
			// public override string? ToString() => Id.ToString(Universe);
			// public void AppendString(StringBuilder builder) => Id.AppendString(builder);

			public static implicit operator EcsId       (Entity entity) => entity.Id;
			public static implicit operator EcsId.Entity(Entity entity) => entity.Id;
		}
	}
}
