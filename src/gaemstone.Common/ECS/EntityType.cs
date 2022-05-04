using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
		public Universe Universe { get; }
		readonly ImmutableSortedSet<EcsId> _entries;
		readonly int _hashCode;

		public int Count => _entries.Count;
		public EcsId this[int index] => _entries[index];

		internal static EntityType Empty(Universe universe)
			=> new(universe, ImmutableSortedSet<EcsId>.Empty);
		EntityType(Universe universe, ImmutableSortedSet<EcsId> entries)
		{
			Universe = universe;
			_entries = entries;

			var hashCode = new HashCode();
			hashCode.Add(Count);
			foreach (var id in _entries) hashCode.Add(id);
			_hashCode = hashCode.ToHashCode();
		}

		public int IndexOf(EcsId id)
			=> _entries.IndexOf(id);
		public bool Contains(EcsId id)
			=> _entries.Contains(id);

		public bool Includes(EntityType other)
			=> _entries.IsSupersetOf(other._entries);
		public bool Overlaps(EntityType other)
			=> _entries.Overlaps(other._entries);


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
			=> Except(ids.Select(o => Universe.Lookup(o).Id));
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

		// TODO: ToString
		// public override string ToString()
		// {
		// 	var builder = new StringBuilder();
		// 	AppendString(builder);
		// 	return builder.ToString();
		// }
		// public void AppendString(StringBuilder builder)
		// {
		// 	builder.Append('[');
		// 	for (var i = 0; i < Count; i++) {
		// 		if (i > 0) builder.Append(", ");
		// 		this[i].AppendString(builder, Universe);
		// 	}
		// 	builder.Append(']');
		// }
	}
}
