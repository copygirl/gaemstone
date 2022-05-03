using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using gaemstone.Common.Utility;

namespace gaemstone.ECS
{
	public class TableManager
	{
		readonly Universe _universe;
		readonly Dictionary<EntityType, Table> _tables = new();
		readonly Dictionary<EcsId, List<Table>> _index = new();

		public event Action<Table>? TableAdded;
		// public event Action<Table>? TableRemoved;

		internal TableManager(Universe universe)
		{
			_universe = universe;
			TableAdded += AddTableToIndex;
		}

		internal void Bootstrap()
		{
			var componentTable = BootstrapTable(
				type:        _universe.Type(Universe.TypeID, Universe.IdentifierID, Universe.ComponentID),
				storageType: _universe.Type(Universe.TypeID, Universe.IdentifierID),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(Universe.TypeID       , typeof(Type)),
				(Universe.IdentifierID , typeof(Identifier)));
			var tagTable = BootstrapTable(
				type:        _universe.Type(Universe.TypeID, Universe.IdentifierID, Universe.TagID),
				storageType: _universe.Type(Universe.TypeID, Universe.IdentifierID),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(Universe.ComponentID , typeof(Component)),
				(Universe.TagID       , typeof(Tag)));

			TableAdded?.Invoke(componentTable);
			TableAdded?.Invoke(tagTable);
		}

		Table BootstrapTable(EntityType type, EntityType storageType, Type[] columnTypes,
		                     params (EcsId.Entity ID, Type Type)[] entities)
		{
			var table = new Table(_universe, type, storageType, columnTypes);
			_tables.Add(type, table);

			// FIXME: Only works as long as long as entities.Count <= Table.STARTING_CAPACITY.
			table.EnsureCapacity(entities.Length);
			var typeColumn       = table.Columns.OfType<Type[]>().Single();
			var identifierColumn = table.Columns.OfType<Identifier[]>().Single();

			foreach (var (id, entityType) in entities) {
				ref var record = ref _universe.Entities.GetRecord(id);
				Move(id, ref record, type);
				typeColumn[record.Row]       = entityType;
				identifierColumn[record.Row] = entityType.Name;
			}

			return table;
		}

		void AddTableToIndex(Table table)
		{
			foreach (var id in table.Type) {
				if (id.AsPair() is EcsId.Pair pair) {
					_index.GetOrAddNew(new EcsId.Pair(Universe.Wildcard.ID, pair.Target)).Add(table);
					_index.GetOrAddNew(new EcsId.Pair(pair.Relation, Universe.Wildcard.ID)).Add(table);
				}
				_index.GetOrAddNew(id).Add(table);
			}
		}


		public bool TryGet(EntityType type, [MaybeNullWhen(false)] out Table table)
			=> _tables.TryGetValue(type, out table);

		public IEnumerable<Table> GetAll(EcsId id)
			=> _index.TryGetValue(id, out var list)
				? new ReadOnlyCollection<Table>(list)
				: Enumerable.Empty<Table>();


		public Table GetOrCreate(EntityType type)
		{
			if (!_tables.TryGetValue(type, out var table)) {
				var storageType = _universe.EmptyType;
				var columnTypes = new List<Type>();
				foreach (var id in type) {
					var storage = GetStorageType(id);
					if (storage == null) continue;
					storageType = storageType.Union(id);
					columnTypes.Add(storage);
				}
				table = new Table(_universe, type, storageType, columnTypes);
				_tables.Add(type, table);
				TableAdded?.Invoke(table);

			}
			return table;
		}

		Type? GetStorageType(EcsId id)
		{
			if (id.AsEntity() is EcsId.Entity entity) {
				if (_universe.Has<Component>(entity)) return _universe.Get<Type>(entity);
			} else if (id.AsPair() is EcsId.Pair pair) {
				var relation = _universe.Lookup(pair.Relation);
				var target   = _universe.Lookup(pair.Target);
				if (_universe.Has<Component>(relation)) return _universe.Get<Type>(relation);
				if (_universe.Has<Component>(target))   return _universe.Get<Type>(target);
			}
			return null;
		}


		internal void Move(EcsId.Entity entity, ref Record record, EntityType toType)
		{
			if (record.Type == toType) return;

			var oldTable = record.Table;
			var oldRow   = record.Row;

			// Add the entity to the new Table, and get the row index.
			record.Table = GetOrCreate(toType);
			record.Row   = record.Table.Add(entity);

			// Iterate the old and new types and when they overlap (have the
			// same entry), attempt to move data over to the new Table.
			var oldIndex = 0;
			var newIndex = 0;
			while ((oldIndex < oldTable.StorageType.Count) && (newIndex < record.Table.StorageType.Count)) {
				var diff = oldTable.StorageType[oldIndex].CompareTo(record.Table.StorageType[newIndex]);
				if (diff == 0) {
					Array.Copy(oldTable.Columns[oldIndex], oldRow, record.Table.Columns[newIndex], record.Row, 1);
					newIndex++;
					oldIndex++;
				}
				// If the entries are not the same, advance only one of them.
				// Since the entries in EcsType are sorted, we can do this.
				else   if (diff > 0)   newIndex++;
				else /*if (diff < 0)*/ oldIndex++;
			}

			// Finally, remove the entity from the old Table.
			oldTable.Remove(oldRow);
		}
	}
}
