using System;

namespace gaemstone.ECS
{
	public readonly partial struct EcsId
	{
		public readonly struct Pair
			: IEquatable<Pair>
			, IComparable<Pair>
		{
			public readonly ulong Value;

			public uint Target   => (uint)Value;
			public uint Relation => (uint)((Value >> 32) & 0xFFFFFF);


			public Pair(ulong value) : this()
			{
				if (Value >> 56 != (ulong)EcsRole.Pair) throw new ArgumentException(
					"Role bits must be set to EcsRole.Pair", nameof(value));
				Value = value;
			}

			public Pair(uint relation, uint target)
			{
				if (relation > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(relation),
					relation, "Relation must fit into 24 bits (be smaller or equal to 0xFFFFFF");
				Value = (ulong)target | (relation << 32) | ((ulong)EcsRole.Pair << 56);
			}


			public bool Equals(Pair other) => Value == other.Value;
			public override bool Equals(object? obj) => (obj is Pair other) && Equals(other);
			public override int GetHashCode() => HashCode.Combine(Value);

			public static bool operator ==(Pair left, Pair right) =>  left.Equals(right);
			public static bool operator !=(Pair left, Pair right) => !left.Equals(right);

			public int CompareTo(Pair other) => Value.CompareTo(other.Value);

			public override string ToString() => $"Pair(Target=0x{Target:X},Relation=0x{Relation:X})";
		}
	}
}
