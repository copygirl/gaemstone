using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace gaemstone.ECS
{
	public class EcsType
		: IEquatable<EcsType>
		, IReadOnlyList<EcsId>
	{
		public Universe Universe { get; }
		readonly ImmutableSortedSet<EcsId> _entries;
		readonly int _hashCode;

		public int Count => _entries.Count;
		public EcsId this[int index] => _entries[index];

		public EcsType(Universe universe, params EcsId[] entries)
			: this(universe, (IEnumerable<EcsId>)entries) {  }
		public EcsType(Universe universe, IEnumerable<EcsId> entries)
		{
			Universe = universe;
			_entries = entries.ToImmutableSortedSet();

			var hashCode = new HashCode();
			foreach (var id in _entries) hashCode.Add(id);
			_hashCode = hashCode.ToHashCode();
		}

		public int IndexOf(EcsId value)
			=> _entries.IndexOf(value);
		public bool Contains(EcsId value)
			=> _entries.Contains(value);
		public bool Includes(EcsType other)
			=> _entries.IsSupersetOf(other._entries);
		public bool Overlaps(EcsType other)
			=> _entries.Overlaps(other._entries);


		public EcsType Add(params EcsId[] values)
			=> Add((IEnumerable<EcsId>)values);
		public EcsType Add(IEnumerable<EcsId> values)
			=> new(Universe, _entries.Concat(values));

		public EcsType Remove(params EcsId[] values)
			=> Remove((IEnumerable<EcsId>)values);
		public EcsType Remove(IEnumerable<EcsId> values)
			=> new(Universe, _entries.Except(values));


		public IEnumerator<EcsId> GetEnumerator()
			=> _entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public bool Equals(EcsType? other)
			=> (other is not null) && Enumerable.SequenceEqual(_entries, other._entries);
		public override bool Equals(object? obj) => Equals(obj as EcsType);
		public override int GetHashCode() => _hashCode;

		public static bool operator ==(EcsType left, EcsType right)
			=> ReferenceEquals(left, right) || left.Equals(right);
		public static bool operator !=(EcsType left, EcsType right)
			=> !(left == right);

		public override string ToString()
		{
			var builder = new StringBuilder();
			AppendString(builder);
			return builder.ToString();
		}
		public void AppendString(StringBuilder builder)
		{
			builder.Append('[');
			for (var i = 0; i < Count; i++) {
				if (i > 0) builder.Append(", ");
				this[i].AppendString(builder, Universe);
			}
			builder.Append(']');
		}
	}
}
