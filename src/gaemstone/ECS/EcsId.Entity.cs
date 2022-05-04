using System;
using System.Runtime.InteropServices;

namespace gaemstone.ECS
{
	public readonly partial struct EcsId
	{
		/// <summary>
		/// An entity is a unique object created within the scope of a
		/// <see cref="Universe"/>, from which it can also be deleted.
		///
		/// Besides traditional game objects, entities can also represent tags,
		/// components and relations, which themselves can be added to entities.
		///
		/// An <see cref="Entity"/> implicitly converts to an <see cref="EcsId"/>.
		/// </summary>
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
			[FieldOffset(0)] public readonly uint Id;

			/// <summary>
			/// The generation of this entitiy.
			/// This is increased each time an entity is destroyed.
			/// </summary>
			[FieldOffset(4)] public readonly ushort Generation;


			public bool IsNone => Id == 0;


			public Entity(ulong value) : this()
			{
				const ulong UnusedMask = 0x00FF_0000_0000_0000;
				const ulong RoleMask   = 0xFF00_0000_0000_0000;
				if ((value & UnusedMask) != 0L) throw new ArgumentException("Value has unused bits set", nameof(value));
				if ((value &   RoleMask) != 0L) throw new ArgumentException("Value has role bits set", nameof(value));
				Value = value;
			}

			public Entity(uint id, ushort generation) : this()
			{
				if (id == 0) throw new ArgumentException("Id must be greater than 0", nameof(id));
				Id = id; Generation = generation;
			}


			public bool Equals(Entity other) => Value == other.Value;
			public override bool Equals(object? obj) => (obj is Entity other) && Equals(other);
			public override int GetHashCode() => HashCode.Combine(Value);

			public static bool operator ==(Entity left, Entity right) =>  left.Equals(right);
			public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

			public int CompareTo(Entity other) => Value.CompareTo(other.Value);

			public override string ToString() => $"Entity(Id=0x{Id:X},Generation=0x{Generation:X})";
		}
	}
}
