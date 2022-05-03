using System;
using System.Runtime.InteropServices;

namespace gaemstone.ECS
{
	[StructLayout(LayoutKind.Explicit)]
	public readonly partial struct EcsId
		: IEquatable<EcsId>
		, IComparable<EcsId>
	{
		[FieldOffset(0)] public readonly ulong Value;

		[FieldOffset(7)] public readonly EcsRole Role;


		[FieldOffset(0)] readonly Entity _asEntity;
		[FieldOffset(0)] readonly Pair   _asPair;


		EcsId(ulong value) : this() => Value = value;


		public static implicit operator EcsId(Entity entity) => new(entity.Value);
		public static implicit operator EcsId(Pair   pair  ) => new(pair.Value);

		public Entity? AsEntity() => (Role == EcsRole.Entity) ? _asEntity : null;
		public Pair?   AsPair  () => (Role == EcsRole.Pair  ) ? _asPair   : null;


		public bool Equals(EcsId other) => Value == other.Value;
		public override bool Equals(object? obj) => (obj is EcsId other) && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(EcsId left, EcsId right) =>  left.Equals(right);
		public static bool operator !=(EcsId left, EcsId right) => !left.Equals(right);

		public int CompareTo(EcsId other) => Value.CompareTo(other.Value);

		public override string ToString() => Role switch {
			EcsRole.Entity => _asEntity.ToString(),
			EcsRole.Pair   => _asPair.ToString(),
			_ => $"EcsId(Value=0x{Value:X},Role={Role})"
		};
	}

	public enum EcsRole : byte
	{
		Entity,
		Pair,
	}
}
