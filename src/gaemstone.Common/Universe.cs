using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using gaemstone.Common.Processors;

namespace gaemstone.Common
{
	public class Universe
	{
		// Built-in components.
		public static readonly EcsId Component  = new(0x01);
		public static readonly EcsId Identifier = new(0x02);

		// Built-in relationships.
		public static readonly EcsId IsA     = new(0x100);
		public static readonly EcsId ChildOf = new(0x101);


		public EntityManager    Entities   { get; }
		public ArchetypeManager Archetypes { get; }
		public ProcessorManager Processors { get; }
		public QueryManager     Queries    { get; }


		public Universe()
		{
			Entities   = new(this);
			Archetypes = new(this);
			Processors = new(this);
			Queries    = new(this);


			// Bootstrap the components necessary to represent components:
			Entities.NewWithID(Component.ID);
			Entities.NewWithID(Identifier.ID);

			var type = new EcsType(Component, Identifier);
			Archetypes.SetEntityType(Component, type);
			Archetypes.SetEntityType(Identifier, type);

			// Initialize the columns in the Archtype.
			// They were not initialized as Component[] and Identifier[]
			// arrays, because these components do not yet fully exist.
			var arch = Archetypes[type].Archetype;
			arch.Entities[0] = Component;
			arch.Entities[1] = Identifier;
			arch.Columns[0] = new Component[arch.Capacity];
			arch.Columns[1] = new Identifier[arch.Capacity];
			((Component[])arch.Columns[0])[0] = new Component(typeof(Component));
			((Component[])arch.Columns[0])[1] = new Component(typeof(Identifier));
			((Identifier[])arch.Columns[1])[0] = nameof(Component);
			((Identifier[])arch.Columns[1])[1] = nameof(Identifier);


			// TODO: Implement relationships.
			// Entities.NewWithID(IsA.ID);
			// Entities.NewWithID(ChildOf.ID);

			NewComponent<Transform>();
		}


		public EcsId NewComponent<T>()
		{
			var entity = Entities.New(Component, Identifier);
			Set<Component>(entity, new(typeof(T)));
			Set<Identifier>(entity, typeof(T).Name);
			return entity;
		}


		public bool Has<T>(EcsId entity)
			=> TryGet<T>(entity, out _);

		public bool TryGet<T>(EcsId entity, [NotNullWhen(true)] out T? value)
		{
			var record = Entities.GetRecord(entity);
			var column = record.Archetype.Columns.OfType<T[]>().FirstOrDefault();
			if (column == null) { value = default!; return false; }
			value = column[record.Row]!;
			return true;
		}
		public T Get<T>(EcsId entity)
			=> TryGet<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(entity, typeof(T));

		public ref T GetRef<T>(EcsId entity)
		{
			var record = Entities.GetRecord(entity);
			var column = record.Archetype.Columns.OfType<T[]>().FirstOrDefault();
			if (column == null) throw new ComponentNotFoundException(entity, typeof(T));
			return ref column[record.Row];
		}

		public void Set<T>(EcsId entity, T value)
		{
			var component  = GetEntityForComponentTypeOrThrow(typeof(T));
			ref var record = ref Entities.GetRecord(entity);
			Archetypes.Add(entity, ref record, component);
			var columnIndex = record.Type.IndexOf(component);
			((T[])record.Archetype.Columns[columnIndex])[record.Row] = value;
		}


		public EcsId GetEntityForComponentTypeOrThrow(Type type)
		{
			if (!TryGetEntityForComponentType(type, out var component))
				throw new ComponentNotFoundException(type, "The component type was not found");
			return component;
		}
		public bool TryGetEntityForComponentType(Type type, out EcsId entity)
		{
			foreach (var archetype in Archetypes.Root.With(Component)) {
				var column = archetype.Columns.OfType<Component[]>().First();
				var index = Array.FindIndex(column, component => component.Type == type);
				if (index < 0) continue;

				entity = archetype.Entities[index];
				return true;
			}
			entity = default;
			return false;
		}
	}
}
