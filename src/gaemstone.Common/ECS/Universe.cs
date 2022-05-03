using System.Collections.Generic;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		// Built-in Components
		static internal readonly EcsId.Entity TypeID       = new(0x01);
		static internal readonly EcsId.Entity IdentifierID = new(0x02);

		// Built-in Tags
		static internal readonly EcsId.Entity ComponentID = new(0x10);
		static internal readonly EcsId.Entity TagID       = new(0x11);
		static internal readonly EcsId.Entity RelationID  = new(0x12);

		// TODO: Built-in Relationships
		static internal readonly EcsId.Entity ChildOf = new(0x20);
		static internal readonly EcsId.Entity IsA     = new(0x21);

		// Built-in Special Entities
		static internal readonly EcsId.Entity Wildcard = new(0x30);

		public EntityManager    Entities   { get; }
		public TableManager     Tables     { get; }
		public QueryManager     Queries    { get; }
		public ProcessorManager Processors { get; }

		public EntityType EmptyType { get; }
		public EntityType Type(params object[] ids)    => EmptyType.Union(ids);
		public EntityType Type(params EcsId[] ids)     => EmptyType.Union(ids);
		public EntityType Type(IEnumerable<EcsId> ids) => EmptyType.Union(ids);

		public Universe()
		{
			Entities   = new(this);
			Tables     = new(this);
			Queries    = new(this);
			Processors = new(this);

			EmptyType = EntityType.Empty(this);


			// Built-in Components required to bootstrap
			Entities.NewWithID(TypeID.ID);
			Entities.NewWithID(IdentifierID.ID);
			// Built-in Tags required to bootstrap.
			Entities.NewWithID(ComponentID.ID);
			Entities.NewWithID(TagID.ID);
			// Bootstrap table structures so other methods can work.
			Tables.Bootstrap();


			// Build-in Tags
			Entities.NewWithID(RelationID.ID).Add(typeof(Tag)).Set(typeof(Relation));

			// Build-in Relations
			Entities.NewWithID(ChildOf.ID).Add(typeof(Tag), typeof(Relation)).Set(typeof(ChildOf));
			Entities.NewWithID(IsA.ID    ).Add(typeof(Tag), typeof(Relation)).Set(typeof(IsA));

			// Special "Wildcard" (*), used when looking up relationships
			Entities.NewWithID(Wildcard.ID).Set((Identifier)"*");


			NewComponent<Common.Transform>();
		}

		public EcsId NewComponent<T>()
			=> Entities.New()
				.Add(typeof(Component))
				.Set(typeof(T))
				.Set((Identifier)typeof(T).Name);
	}
}
