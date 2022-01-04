using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace gaemstone.Common
{
	public class EcsType
		: IEquatable<EcsType>
		, IReadOnlyList<EcsId>
	{
		public static readonly EcsType Empty = new(Enumerable.Empty<EcsId>());


		readonly ImmutableList<EcsId> _entries;
		readonly int _hashCode;

		public int Count => _entries.Count;
		public EcsId this[int index] => _entries[index];

		public EcsType(params EcsId[] entries)
			: this((IEnumerable<EcsId>)entries) {  }
		public EcsType(IEnumerable<EcsId> entries)
		{
			_entries = entries
				.OrderBy(id => id)
				.Distinct()
				.ToImmutableList();

			var hashCode = new HashCode();
			foreach (var id in this) hashCode.Add(id);
			_hashCode = hashCode.ToHashCode();
		}

		public int IndexOf(EcsId value)
			=> _entries.IndexOf(value);
		public bool Contains(EcsId value)
			=> _entries.Contains(value);

		public bool Overlaps(EcsType other)
		{
			var thisEnum  = GetEnumerator();
			var otherEnum = other.GetEnumerator();
			if (!thisEnum.MoveNext()) return false;
			if (!otherEnum.MoveNext()) return false;
			while (true) {
				var compare = thisEnum.Current.CompareTo(otherEnum.Current);
				if      (compare == 0) return true;
				else if (compare <  0) { if (!thisEnum.MoveNext())  return false; }
				else                   { if (!otherEnum.MoveNext()) return false; }
			}
		}


		public EcsType Add(params EcsId[] values)
			=> Add((IEnumerable<EcsId>)values);
		public EcsType Add(IEnumerable<EcsId> values)
			=> new(_entries.Concat(values));

		public EcsType Remove(params EcsId[] values)
			=> Remove((IEnumerable<EcsId>)values);
		public EcsType Remove(IEnumerable<EcsId> values)
			=> new(_entries.Except(values));


		public IEnumerator<EcsId> GetEnumerator()
			=> _entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public bool Equals(EcsType? other)
			=> (other is not null) && Enumerable.SequenceEqual(_entries, other._entries);
		public override bool Equals(object? obj) => Equals(obj as EcsType);
		public override int GetHashCode() => _hashCode;

		public static bool operator ==(EcsType left, EcsType right)
			=> object.ReferenceEquals(left, right) || left.Equals(right);
		public static bool operator !=(EcsType left, EcsType right)
			=> !(left == right);

		public override string ToString()
			=> $"EcsType({string.Join(", ", this)})";
		public string ToPrettyString(Universe universe)
			=> $"[{string.Join(", ", this.Select(id => id.ToPrettyString(universe)))}]";
	}
}
