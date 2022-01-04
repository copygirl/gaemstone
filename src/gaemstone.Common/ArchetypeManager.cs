using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace gaemstone.Common
{
	public class ArchetypeManager
	{
		public class Node : IEnumerable<Archetype>
		{
			readonly ArchetypeManager _manager;
			readonly Dictionary<EcsId, Node> _addEdges    = new();
			readonly Dictionary<EcsId, Node> _removeEdges = new();
			Archetype? _archetype;

			public EcsType Type { get; }
			public Archetype? MaybeArchetype => _archetype;
			public Archetype Archetype => _archetype ??= new(_manager._universe, Type);

			internal Node(ArchetypeManager manager, EcsType type)
			{
				_manager = manager;
				Type = type;

				foreach (var id in Type) {
					// Any addition edges of ids contained within
					// this node's Type already just point to itself.
					_addEdges.Add(id, this);

					// Connect with the neighboring nodes in the graph.
					var neighbor = _manager[Type.Remove(id)];
					_removeEdges.Add(id, neighbor);
					neighbor._addEdges.Add(id, this);
				}
			}

			public Node With(EcsId id)
			{
				if (!_addEdges.TryGetValue(id, out var node))
					node = new(_manager, Type.Add(id));
				return node;
			}

			public Node Without(EcsId id)
			{
				// All removal edges should be initialized when nodes are created, so
				// if it doesn't exist, assume the specified id is not part of Type.
				if (!_removeEdges.TryGetValue(id, out var node)) {
					Debug.Assert(!Type.Contains(id));
					_removeEdges.Add(id, node = this);
				}
				return node;
			}

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
			public IEnumerator<Archetype> GetEnumerator()
			{
				if (_archetype != null)
					yield return _archetype;
				foreach (var node in _addEdges.Values) {
					if (node == this) continue;
					var enumerator = node.GetEnumerator();
					while (enumerator.MoveNext())
						yield return enumerator.Current;
				}
			}
		}


		readonly Universe _universe;

		public Node Root { get; }

		public Node this[EcsType type] { get {
			var node = Root;
			foreach (var id in type)
				node = node.With(id);
			return node;
		} }

		internal ArchetypeManager(Universe universe)
		{
			_universe = universe;
			Root = new(this, EcsType.Empty);
		}


		internal void Add(EcsId entity, EcsId value) => ModifyEntityType(entity, type => type.Add(value));
		internal void Remove(EcsId entity, EcsId value) => ModifyEntityType(entity, type => type.Remove(value));
		internal void SetEntityType(EcsId entity, EcsType type) => ModifyEntityType(entity, _ => type);

		internal void Add(EcsId entity, ref Record record, EcsId value) => ModifyEntityType(entity, ref record, type => type.Add(value));
		internal void Remove(EcsId entity, ref Record record, EcsId value) => ModifyEntityType(entity, ref record, type => type.Remove(value));
		internal void SetEntityType(EcsId entity, ref Record record, EcsType type) => ModifyEntityType(entity, ref record, _ => type);

		internal void ModifyEntityType(EcsId entity, Func<EcsType, EcsType> func)
			=> ModifyEntityType(entity, ref _universe.Entities.GetRecord(entity), func);
		internal void ModifyEntityType(EcsId entity, ref Record record, Func<EcsType, EcsType> func)
		{
			var toType = func(record.Type);
			if (record.Type == toType) return;

			var oldArchetype = record.Archetype;
			var oldRow       = record.Row;
			var oldType      = record.Type;

			// Add the entity to the new Archetype, and get the row index.
			record.Archetype = this[toType].Archetype;
			record.Row       = record.Archetype.Add(entity);

			// Iterate the old and new types and when they overlap (have the
			// same entry), attempt to move data over to the new Archetype.
			var oldIndex = 0;
			var newIndex = 0;
			while ((oldIndex < oldType.Count) && (newIndex < toType.Count)) {
				var diff = oldType[oldIndex].CompareTo(toType[newIndex]);
				if (diff == 0) {
					// Only copy if the column actually exists (is a component).
					if (oldArchetype.Columns[oldIndex] is Array column)
						Array.Copy(column, oldRow, record.Archetype.Columns[newIndex], record.Row, 1);
					newIndex++;
					oldIndex++;
				}
				// If the entries are not the same, advance only one of them.
				// Since the entries in EcsType are sorted, we can do this.
				else   if (diff > 0)   newIndex++;
				else /*if (diff < 0)*/ oldIndex++;
			}

			// Finally, remove the entity from the old Archetype.
			oldArchetype.Remove(oldRow);
		}
	}
}
