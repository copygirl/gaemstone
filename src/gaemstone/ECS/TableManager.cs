using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using gaemstone.Utility;

namespace gaemstone.ECS
{
	public class TableManager
	{
		readonly Dictionary<EntityType, Table> _tables = new();
		readonly Dictionary<EcsId, List<Table>> _index = new();

		public Universe Universe { get; }

		public event Action<Table>? TableAdded;
		// public event Action<Table>? TableRemoved;

		internal TableManager(Universe universe)
		{
			Universe = universe;
			TableAdded += AddTableToIndex;
		}

		internal void Bootstrap()
		{
			var componentTable = BootstrapTable(
				type:        Universe.Type(Universe.TypeId, Universe.IdentifierId, Universe.ComponentId),
				storageType: Universe.Type(Universe.TypeId, Universe.IdentifierId),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(Universe.TypeId       , typeof(Type)),
				(Universe.IdentifierId , typeof(Identifier)));
			var tagTable = BootstrapTable(
				type:        Universe.Type(Universe.TypeId, Universe.IdentifierId, Universe.TagId),
				storageType: Universe.Type(Universe.TypeId, Universe.IdentifierId),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(Universe.ComponentId , typeof(Component)),
				(Universe.TagId       , typeof(Tag)));

			TableAdded?.Invoke(componentTable);
			TableAdded?.Invoke(tagTable);
		}

		Table BootstrapTable(EntityType type, EntityType storageType, Type[] columnTypes,
		                     params (EcsId.Entity Id, Type Type)[] entities)
		{
			var table = new Table(Universe.Entities, type, storageType, columnTypes);
			_tables.Add(type, table);

			// FIXME: Only works as long as long as entities.Count <= Table.STARTING_CAPACITY.
			table.EnsureCapacity(entities.Length);
			var typeColumn       = table.Columns.OfType<Type[]>().Single();
			var identifierColumn = table.Columns.OfType<Identifier[]>().Single();

			foreach (var (id, entityType) in entities) {
				ref var record = ref Universe.Entities.GetRecord(id);
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
					_index.GetOrAddNew(new EcsId.Pair(Universe.Wildcard.Id, pair.Target)).Add(table);
					_index.GetOrAddNew(new EcsId.Pair(pair.Relation, Universe.Wildcard.Id)).Add(table);
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
				var storageType = Universe.EmptyType;
				var columnTypes = new List<Type>();
				foreach (var id in type) {
					var storage = GetStorageType(id);
					if (storage == null) continue;
					storageType = storageType.Union(id);
					columnTypes.Add(storage);
				}
				table = new Table(Universe.Entities, type, storageType, columnTypes);
				_tables.Add(type, table);
				TableAdded?.Invoke(table);

			}
			return table;
		}

		Type? GetStorageType(EcsId id)
		{
			if (id.AsEntity() is EcsId.Entity entity) {
				if (Universe.Has<Component>(entity)) return Universe.Get<Type>(entity);
			} else if (id.AsPair() is EcsId.Pair pair) {
				var relation = Universe.Lookup(pair.Relation);
				var target   = Universe.Lookup(pair.Target);
				if (Universe.Has<Component>(relation)) return Universe.Get<Type>(relation);
				if (Universe.Has<Component>(target))   return Universe.Get<Type>(target);
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
