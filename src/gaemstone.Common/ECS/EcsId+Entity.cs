using System;
using System.Runtime.InteropServices;

namespace gaemstone.ECS
{
	public readonly partial struct EcsId
	{
		[StructLayout(LayoutKind.Explicit)]
		public readonly struct Entity
			: IEquatable<Entity>
			, IComparable<Entity>
		{
			/// <summary>
			/// A special entity value that represents "no entity".
			/// There may never be an entity alive with this value.
			/// This is equal to using <c>default(EcsEntity)</c>.
			/// </summary>
			public static readonly Entity None = default;


			/// <summary>
			/// The internal 64-bit value representing this entity.
			/// </summary>
			[FieldOffset(0)] public readonly ulong Value;

			/// <summary>
			/// The unique 32-bit integer identifier for this entity.
			/// Only one (alive) entity may have this identifier at a time.
			/// </summary>
			[FieldOffset(0)] public readonly uint ID;

			/// <summary>
			/// The generation of this entitiy.
			/// This is increased each time an entity is destroyed.
			/// </summary>
			[FieldOffset(4)] public readonly ushort Generation;


			public bool IsNone => ID == 0;


			public Entity(ulong value) : this()
			{
				const ulong UNUSED_MASK = 0x00FF_0000_0000_0000;
				const ulong ROLE_MASK   = 0xFF00_0000_0000_0000;
				if ((value & UNUSED_MASK) != 0L) throw new ArgumentException("Value has unused bits set", nameof(value));
				if ((value &   ROLE_MASK) != 0L) throw new ArgumentException("Value has role bits set", nameof(value));
				Value = value;
			}

			public Entity(uint id, ushort generation) : this()
			{
				if (id == 0) throw new ArgumentException("ID must be greater than 0", nameof(id));
				ID = id; Generation = generation;
			}


			public bool Equals(Entity other) => Value == other.Value;
			public override bool Equals(object? obj) => (obj is Entity other) && Equals(other);
			public override int GetHashCode() => HashCode.Combine(Value);

			public static bool operator ==(Entity left, Entity right) =>  left.Equals(right);
			public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

			public int CompareTo(Entity other) => Value.CompareTo(other.Value);

			public override string ToString() => $"Entity(Id=0x{ID:X},Generation=0x{Generation:X})";
		}
	}
}
