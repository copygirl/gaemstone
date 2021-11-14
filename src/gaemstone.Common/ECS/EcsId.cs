using System;
using System.Runtime.InteropServices;
using System.Text;

namespace gaemstone.Common.ECS
{
	// Reserved for Components (?)
	//   |    Regular Entities
	//   |       |  Generation        Role
	//   v       v          v          v
	// |-8-|-----24-----|---16---|###|-8-|
	// ===================================
	// |  Low 32 bits   |  High 32 bits  |
	// ===================================

	// When Role == Pair, this is the layout:
	// |-------32-------|-----24-----|###|
	//       Target        Relation

	[StructLayout(LayoutKind.Explicit)]
	public readonly struct EcsId
		: IEquatable<EcsId>
		, IComparable<EcsId>
	{
		public static readonly EcsId None = default;

		[FieldOffset(0)] public readonly ulong Value;

		[FieldOffset(0)] public readonly uint Low;
		[FieldOffset(4)] public readonly uint High;

		[FieldOffset(0)] public readonly uint ID;
		[FieldOffset(4)] public readonly ushort Generation;
		[FieldOffset(7)] public readonly EcsRole Role;

		public bool IsNone => (ID == 0);

		EcsId(ulong value) : this() => Value = value;
		EcsId(uint low, uint high) : this() { Low = low; High = high; }

		public EcsId(uint id) : this(id, 0, EcsRole.None) {  }
		public EcsId(uint id, EcsRole role) : this(id, 0, role) {  }
		public EcsId(uint id, ushort generation) : this(id, generation, EcsRole.None) {  }
		public EcsId(uint id, ushort generation, EcsRole role) : this()
		{
			if (id == 0) throw new ArgumentOutOfRangeException(nameof(id), "ID must be greater than 0 to be valid.");
			ID = id; Generation = generation; Role = role;
		}

		public static EcsId Pair(EcsId relation, EcsId target)
			=> new(target.ID | (relation.Value & 0xFFFFFF) << 32 | (ulong)EcsRole.Pair << 56);
		public (uint Relation, uint Target) ToPair()
			=> (High & 0xFFFFFF, Low);
		public (EcsId Relation, EcsId Target) ToPair(Universe universe)
		{
			var (relationId, targetId) = ToPair();
			var relation = universe.Entities.Lookup(relationId);
			var target   = universe.Entities.Lookup(targetId);
			if ((relation == null) || (target == null)) throw new InvalidOperationException(
				"Relation or Target are not alive (anymore?)");
			return (relation.Value, target.Value);
		}


		public bool Equals(EcsId other) => Value == other.Value;
		public override bool Equals(object? obj) => (obj is EcsId other) && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(EcsId left, EcsId right) => left.Equals(right);
		public static bool operator !=(EcsId left, EcsId right) => !left.Equals(right);

		public int CompareTo(EcsId other) => Value.CompareTo(other.Value);


		public override string ToString()
		{
			if (Role == EcsRole.Pair) {
				return $"EcsId.Pair(relation: 0x{High & 0xFFFFFF:X}, target: 0x{Low:X})";
			} else {
				var sb = new StringBuilder("EcsId(id: 0x").AppendFormat("{0:X}", ID);
				if (Generation != 0) sb.Append(", generation: ").Append(Generation);
				if (Role != EcsRole.None) sb.Append(", role: ").Append(Role);
				return sb.Append(')').ToString();
			}
		}

		// TODO: Move this out of this type.
		public string ToPrettyString(Universe universe)
		{
			var sb = new StringBuilder();
			void AppendIdentifier(EcsId id)
			{
				if (universe.TryGet<Identifier>(id, out var i)) sb.Append(i);
				else sb.Append("0x").AppendFormat("{0:X}", id.ID);
			}
			if (Role == EcsRole.Pair) {
				var (relation, target) = ToPair(universe);
				AppendIdentifier(relation);
				sb.Append(" + ");
				AppendIdentifier(target);
			} else {
				if (Role != EcsRole.None) sb.Append(Role).Append(" | ");
				AppendIdentifier(this);
			}
			return sb.ToString();
		}
	}

	public enum EcsRole : byte
	{
		None,
		Pair,
	}
}
