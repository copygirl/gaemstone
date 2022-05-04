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
	/// list of <see cref="EcsId"/>s it has: Components, tags, relations, ...
	/// </summary>
	public class EntityType
		: IEquatable<EntityType>
		, IReadOnlyList<EcsId>
	{
		readonly ImmutableSortedSet<EcsId> _entries;
		readonly int _hashCode;

		public Universe Universe { get; }
		public EcsId this[int index] => _entries[index];
		public int Count => _entries.Count;

		internal static EntityType Empty(Universe universe)
			=> new(universe, ImmutableSortedSet<EcsId>.Empty);
		EntityType(Universe universe, ImmutableSortedSet<EcsId> entries)
		{
			Universe = universe;
			_entries = entries;
			var hashCode = new HashCode();
			foreach (var id in _entries) hashCode.Add(id);
			_hashCode = hashCode.ToHashCode();
		}

		public int IndexOf(EcsId id)
			=> _entries.IndexOf(id);
		public bool Contains(EcsId id)
			=> _entries.Contains(id);

		public bool Includes(IEnumerable<EcsId> other)
			=> _entries.IsSupersetOf(other);
		public bool Overlaps(IEnumerable<EcsId> other)
			=> _entries.Overlaps(other);


		public EntityType Union(params object[] ids)
			=> Union(ids.Select(o => (EcsId)Universe.Lookup(o).Id));
		public EntityType Union(params EcsId[] ids)
			=> Union((IEnumerable<EcsId>)ids);
		public EntityType Union(IEnumerable<EcsId> ids)
		{
			var newEntries = _entries.Union(ids);
			if (ReferenceEquals(newEntries, _entries)) return this;
			else return new(Universe, newEntries);
		}

		public EntityType Except(params object[] ids)
			=> Except(ids.Select(o => (EcsId)Universe.Lookup(o).Id));
		public EntityType Except(params EcsId[] ids)
			=> Except((IEnumerable<EcsId>)ids);
		public EntityType Except(IEnumerable<EcsId> ids)
		{
			var newEntries = _entries.Except(ids);
			if (ReferenceEquals(newEntries, _entries)) return this;
			else return new(Universe, newEntries);
		}


		public IEnumerator<EcsId> GetEnumerator()
			=> _entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public bool Equals(EntityType? other)
			=> (other is not null) && _entries.SetEquals(other._entries);
		public override bool Equals(object? obj) => Equals(obj as EntityType);
		public override int GetHashCode() => _hashCode;

		public static bool operator ==(EntityType left, EntityType right)
			=> ReferenceEquals(left, right) || left.Equals(right);
		public static bool operator !=(EntityType left, EntityType right)
			=> !(left == right);

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append('[');
			for (var i = 0; i < Count; i++) {
				if (i > 0) builder.Append(",");
				builder.Append(this[i]);
			}
			builder.Append(']');
			return builder.ToString();
		}
	}
}
