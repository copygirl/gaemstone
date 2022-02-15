using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace gaemstone.ECS
{
	/// <summary>
	/// An entity type, sometimes referred to as "archetype", represents the
	/// concrete type of an entity (or table). More specifically, it is the
	/// list of <see cref="EcsId"/>s it has (components, tags, and so on).
	/// </summary>
	public class EcsType
		: IEquatable<EcsType>
		, IReadOnlyList<EcsId>
	{
		public Universe Universe { get; }
		readonly ImmutableSortedSet<EcsId> _entries;
		readonly int _hashCode;

		public int Count => _entries.Count;
		public EcsId this[int index] => _entries[index];

		internal static EcsType Empty(Universe universe)
			=> new(universe, ImmutableSortedSet<EcsId>.Empty);
		EcsType(Universe universe, ImmutableSortedSet<EcsId> entries)
		{
			Universe = universe;
			_entries = entries;

			var hashCode = new HashCode();
			hashCode.Add(Count);
			foreach (var id in _entries) hashCode.Add(id);
			_hashCode = hashCode.ToHashCode();
		}

		public int IndexOf(EcsId id)   => _entries.IndexOf(id);
		public bool Contains(EcsId id) => _entries.Contains(id);

		public bool Includes(EcsType other) => _entries.IsSupersetOf(other._entries);
		public bool Overlaps(EcsType other) => _entries.Overlaps(other._entries);


		public EcsType Union(params object[] ids) => Union(ids.Select(o => Universe.Lookup(o).ID));
		public EcsType Union(params EcsId[] ids)  => Union((IEnumerable<EcsId>)ids);
		public EcsType Union(IEnumerable<EcsId> ids)
		{
			var newEntries = _entries.Union(ids);
			if (ReferenceEquals(newEntries, _entries)) return this;
			else return new(Universe, newEntries);
		}

		public EcsType Except(params object[] ids) => Except(ids.Select(o => Universe.Lookup(o).ID));
		public EcsType Except(params EcsId[] ids)  => Except((IEnumerable<EcsId>)ids);
		public EcsType Except(IEnumerable<EcsId> ids)
		{
			var newEntries = _entries.Except(ids);
			if (ReferenceEquals(newEntries, _entries)) return this;
			else return new(Universe, newEntries);
		}


		public IEnumerator<EcsId> GetEnumerator()
			=> _entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public bool Equals(EcsType? other)
			=> (other is not null) && _entries.SetEquals(other._entries);
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
