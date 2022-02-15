using System;
using System.Collections.Generic;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		// Built-in Components
		static internal readonly EcsId TypeID       = new(0x01);
		static internal readonly EcsId IdentifierID = new(0x02);

		// Built-in Tags
		static internal readonly EcsId ComponentID = new(0x10);
		static internal readonly EcsId TagID       = new(0x11);
		static internal readonly EcsId RelationID  = new(0x12);

		// TODO: Built-in Relationships
		// static internal readonly EcsId ChildOf = new(0x20);
		// static internal readonly EcsId IsA     = new(0x21);

		// Built-in Special Entities
		static internal readonly EcsId Wildcard = new(0x30);

		public EntityManager    Entities   { get; }
		public TableManager     Tables     { get; }
		public QueryManager     Queries    { get; }
		public ProcessorManager Processors { get; }

		public EcsType EmptyType { get; }
		public EcsType Type(params object[] ids)    => EmptyType.Union(ids);
		public EcsType Type(params EcsId[] ids)     => EmptyType.Union(ids);
		public EcsType Type(IEnumerable<EcsId> ids) => EmptyType.Union(ids);

		public Universe()
		{
			Entities   = new(this);
			Tables     = new(this);
			Queries    = new(this);
			Processors = new(this);

			EmptyType = EcsType.Empty(this);

			// Built-in Components
			Entities.NewWithID(TypeID.ID);
			Entities.NewWithID(IdentifierID.ID);

			// Built-in Tags
			Entities.NewWithID(ComponentID.ID);
			Entities.NewWithID(TagID.ID);
			Entities.NewWithID(RelationID.ID);

			// Bootstrap table structures so other methods can work.
			Tables.Bootstrap();

			// Create the special "Wildcard" (*) entity that can be used when looking up relationships.
			var wildcard = Entities.NewWithID(Wildcard.ID, typeof(Identifier));
			wildcard.Set((Identifier)"*");

			NewComponent<Common.Transform>();
		}

		public EcsId NewComponent<T>()
		{
			var entity = Entities.New(typeof(Type), typeof(Identifier), typeof(Component));
			entity.Set(typeof(T));
			entity.Set((Identifier)typeof(T).Name);
			return entity;
		}
	}
}
