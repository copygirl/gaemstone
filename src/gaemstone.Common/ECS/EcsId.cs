using System;
using System.Runtime.InteropServices;
using System.Text;

namespace gaemstone.ECS
{
	//         ID       Generation    Role
	// |-------32-------|---16---|###|-8-|
	// ===================================
	// |   Low 32 bits  |  High 32 bits  |
	// ===================================

	// When Role == Pair, this is the layout:
	//       Target        Relation   Role
	// |-------32-------|-----24-----|###|

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

		public static EcsId Pair(uint relation, uint target)
			=> new(target | (relation & 0xFFFFFF) << 32 | (ulong)EcsRole.Pair << 56);
		public static EcsId Pair(EcsId relation, EcsId target)
			=> Pair(target.ID, relation.ID);
		public (uint Relation, uint Target) ToPair()
			=> (High & 0xFFFFFF, Low);
		public (EcsId Relation, EcsId Target) ToPair(Universe context)
		{
			// FIXME: This doesn't support the special Universe.Wildcard ID.
			var (relationId, targetId) = ToPair();
			if (!context.Entities.TryLookup(relationId, out var relation)) throw new EntityNotFoundException(relationId);
			if (!context.Entities.TryLookup(targetId  , out var target  )) throw new EntityNotFoundException(targetId);
			return (relation, target);
		}


		public bool Equals(EcsId other) => Value == other.Value;
		public override bool Equals(object? obj) => (obj is EcsId other) && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(EcsId left, EcsId right) => left.Equals(right);
		public static bool operator !=(EcsId left, EcsId right) => !left.Equals(right);

		public int CompareTo(EcsId other) => Value.CompareTo(other.Value);


		public override string ToString()
		{
			var builder = new StringBuilder();
			AppendString(builder);
			return builder.ToString();
		}
		public void AppendString(StringBuilder builder)
		{
			if (Role == EcsRole.Pair) {
				builder.Append($"EcsId.Pair(relation: 0x{High & 0xFFFFFF:X}, target: 0x{Low:X})");
			} else {
				builder.Append($"EcsId(id: 0x{ID:X}");
				if (Generation != 0) builder.Append($", generation: {Generation}");
				if (Role != EcsRole.None) builder.Append($", role: {Role}");
				builder.Append(')');
			}
		}
		public void AppendString(StringBuilder builder, Universe context)
		{
			if (Role == EcsRole.Pair) {
				void AppendId(uint id)
				{
					if (id == Universe.Wildcard.ID)
						builder.Append('*');
					else if (context.Entities.TryLookup(id, out var entity))
						entity.AppendString(builder, context);
					else builder.Append($"0x{id:X}");
				}

				var (relationId, targetId) = ToPair();
				builder.Append('(');
				AppendId(relationId);
				builder.Append(",");
				AppendId(targetId);
				builder.Append(')');
			} else if (context.TryGet<Identifier>(this, out var identifier))
				builder.Append(identifier);
			else
				AppendString(builder);
		}
	}

	public enum EcsRole : byte
	{
		None,
		Pair,
	}
}
