using System;
using gaemstone.Common.Utility;

namespace gaemstone.Common.ECS
{
	public readonly struct Entity
		: IEquatable<Entity>
	{
		public static readonly Entity None = default(Entity);

		public uint ID { get; }
		public uint Generation { get; }

		public bool IsNone => (ID == 0);

		public Entity(uint id, uint generation)
			=> (ID, Generation) = (id, generation);

		public bool Equals(Entity other)
			=> (ID == other.ID) && (Generation == other.Generation);
		public override bool Equals(object obj)
			=> (obj is Entity other) && Equals(other);

		public override int GetHashCode()
			=> unchecked(HashHelper.Combine((int)ID, (int)Generation));
		public override string ToString()
			=> $"Entity(0x{ID:X8}, Generation={Generation})";

		public static bool operator ==(Entity left, Entity right)
			=> left.Equals(right);
		public static bool operator !=(Entity left, Entity right)
			=> !left.Equals(right);
	}
}
