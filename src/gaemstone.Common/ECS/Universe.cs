using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace gaemstone.ECS
{
	public class Universe
	{
		static internal readonly EcsId Wildcard = new(0xFFFFFA);


		readonly EcsId _typeID;

		public EntityManager    Entities   { get; }
		public TableManager     Tables     { get; }
		public QueryManager     Queries    { get; }
		public ProcessorManager Processors { get; }


		public Universe()
		{
			Entities   = new(this);
			Tables     = new(this);
			Queries    = new(this);
			Processors = new(this);

			// Built-in components.
			_typeID        = Entities.NewWithID(0x01);
			var identifier = Entities.NewWithID(0x02);

			// Built-in tags.
			var component = Entities.NewWithID(0x10);
			var tag       = Entities.NewWithID(0x11);
			var relation  = Entities.NewWithID(0x12);

			// TODO: Built-in relationships.

			Tables.Bootstrap(
				type:        new(this, _typeID, identifier, component),
				storageType: new(this, _typeID, identifier),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(_typeID     , typeof(Type)),
				(identifier, typeof(Identifier)));
			Tables.Bootstrap(
				type:        new(this, _typeID, identifier, tag),
				storageType: new(this, _typeID, identifier),
				columnTypes: new[]{ typeof(Type), typeof(Identifier) },
				(component, typeof(Component)),
				(tag      , typeof(Tag)),
				(relation , typeof(Relation)));

			NewComponent<Common.Transform>();
		}


		public EcsId NewComponent<T>()
		{
			var entity = Entities.New(typeof(Type), typeof(Identifier), typeof(Component));
			Set(entity, typeof(T));
			Set(entity, (Identifier)typeof(T).Name);
			return entity;
		}


		public bool Has<T>(EcsId entity)
		{
			var id   = GetEntityWithTypeOrThrow<T>();
			var type = Entities.GetRecord(entity).Type;
			return type.Contains(id);
		}

		public bool TryGet<T>(EcsId entity, [NotNullWhen(true)] out T? value)
		{
			var id = GetEntityWithTypeOrThrow<T>();
			ref var record = ref Entities.GetRecord(entity);
			var columnIndex = record.Table.StorageType.IndexOf(id);
			if (columnIndex < 0) { value = default!; return false; }
			value = ((T[])record.Table.Columns[columnIndex])[record.Row]!;
			return true;
		}
		public T Get<T>(EcsId entity)
			=> TryGet<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(entity, typeof(T));

		public ref T GetRef<T>(EcsId entity)
		{
			var id = GetEntityWithTypeOrThrow<T>();
			ref var record = ref Entities.GetRecord(entity);
			var columnIndex = record.Table.StorageType.IndexOf(id);
			if (columnIndex < 0) throw new ComponentNotFoundException(entity, typeof(T));
			return ref ((T[])record.Table.Columns[columnIndex])[record.Row];
		}

		public void Set<T>(EcsId entity, T value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			var id = GetEntityWithTypeOrThrow<T>();
			ref var record = ref Entities.GetRecord(entity);
			Tables.Add(entity, ref record, id);

			var columnIndex = record.Table.StorageType.IndexOf(id);
			if (columnIndex < 0) throw new ComponentNotFoundException(entity, typeof(T));
			((T[])record.Table.Columns[columnIndex])[record.Row] = value;
		}


		public EcsId GetEntityWithTypeOrThrow<T>()
			=> GetEntityWithTypeOrThrow(typeof(T));
		public EcsId GetEntityWithTypeOrThrow(Type type)
			=> TryGetEntityWithType(type, out var component) ? component
				: throw new InvalidOperationException($"Entity with Type={type.Name} not found");
		public bool TryGetEntityWithType(Type type, out EcsId entity)
		{
			// TODO: Optimize this?
			foreach (var table in Tables.GetAll(_typeID)) {
				var column = table.Columns.OfType<Type[]>().First();
				for (var i = 0; i < table.Count; i++) {
					if (column[i] == type) {
						entity = table.Entities[i];
						return true;
					}
				}
			}
			entity = default;
			return false;
		}
	}
}
