using System;
using System.Diagnostics.CodeAnalysis;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		public readonly struct Entity
			: IEntity, IEquatable<Entity>
		{
			public Universe Universe { get; }
			public EcsId.Entity ID { get; }

			public bool IsAlive => Universe.Entities.IsAlive(this);
			public EntityType Type {
				get => Universe.Entities.GetRecord(this).Type;
				set => Universe.Tables.Move(this, ref Universe.Entities.GetRecord(this), value);
			}

			public Entity(Universe universe, EcsId.Entity id)
				{ Universe = universe; ID = id; }

			public void Delete() => Universe.Entities.Delete(this);

			public bool Has(EcsId id) => Type.Contains(id);

			public bool TryGet<T>([NotNullWhen(true)] out T? value) => Universe.TryGet(this, out value);
			public bool TrySet<T>(T value) => Universe.TrySet(this, value);

			public T Get<T>() => Universe.Get<T>(this);
			public ref T GetRef<T>() => ref Universe.GetRef<T>(this);
			public Entity Set<T>(T value) { Universe.Set(this, value); return this; }
			IEntity IEntity.Set<T>(T value) => Set(value);

			public override bool Equals(object? obj) => (obj is Entity entity) && Equals(entity);
			public bool Equals(Entity other) => (Universe == other.Universe) && (ID == other.ID);
			public override int GetHashCode() => HashCode.Combine(Universe, ID);

			// TODO: Do the ToString stuff.
			// public override string? ToString() => ID.ToString(Universe);
			// public void AppendString(StringBuilder builder) => ID.AppendString(builder);

			public static implicit operator EcsId       (Entity entity) => entity.ID;
			public static implicit operator EcsId.Entity(Entity entity) => entity.ID;
		}
	}
}
