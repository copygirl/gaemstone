using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace gaemstone.ECS
{
	public class TableManager
	{
		readonly Universe _universe;
		readonly Dictionary<EcsType, Table> _tables = new();
		readonly Dictionary<EcsId, List<Table>> _index = new();

		// public event Action<Table>? TableAdded;
		// public event Action<Table>? TableRemoved;

		internal TableManager(Universe universe)
			=>  _universe = universe;


		public bool TryGet(EcsType type, [MaybeNullWhen(false)] out Table table)
			=> _tables.TryGetValue(type, out table);

		public Table GetOrCreate(EcsType type)
		{
			if (!_tables.TryGetValue(type, out var table)) {
				var storageType = new EcsType(_universe);
				var columnTypes = new List<Type>();
				foreach (var id in type) {
					var storage = GetStorageType(id);
					if (storage == null) continue;
					storageType = storageType.Add(id);
					columnTypes.Add(storage);
				}
				table = new Table(_universe, type, storageType, columnTypes);
				_tables.Add(type, table);

				foreach (var id in type) {
					if (id.Role == EcsRole.Pair) {
						var (relationId, targetId) = id.ToPair();
						AddToIndex(EcsId.Pair(Universe.Wildcard.ID, targetId), table);
						AddToIndex(EcsId.Pair(relationId, Universe.Wildcard.ID), table);
					}
					AddToIndex(id, table);
				}
			}
			return table;
		}

		void AddToIndex(EcsId id, Table table)
		{
			if (!_index.TryGetValue(id, out var list))
				_index.Add(id, list = new());
			list.Add(table);
		}

		public IEnumerable<Table> GetAll(EcsId id)
			=> _index.TryGetValue(id, out var list) ? list : Enumerable.Empty<Table>();

		internal void Bootstrap(EcsType type, EcsType storageType, Type[] columnTypes,
		                        params (EcsId ID, Type Type)[] entities)
		{
			var table = new Table(_universe, type, storageType, columnTypes);
			_tables.Add(type, table);
			foreach (var id in type) AddToIndex(id, table);

			// FIXME: Only works as long as long as entities.Count <= Table.STARTING_CAPACITY.
			table.EnsureCapacity(entities.Length);
			var typeColumn       = table.Columns.OfType<Type[]>().Single();
			var identifierColumn = table.Columns.OfType<Identifier[]>().Single();

			foreach (var (id, entityType) in entities) {
				ref var record = ref _universe.Entities.GetRecord(id);
				SetEntityType(id, ref record, type);
				typeColumn[record.Row]       = entityType;
				identifierColumn[record.Row] = entityType.Name;
			}
		}

		Type? GetStorageType(EcsId id)
		{
			if (id.Role == EcsRole.Pair) {
				var (relation, target) = id.ToPair(_universe);
				if (_universe.Has<Component>(relation)) return _universe.Get<Type>(relation);
				if (_universe.Has<Component>(target))   return _universe.Get<Type>(target);
			}
			if (_universe.Has<Component>(id)) return _universe.Get<Type>(id);
			return null;
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
