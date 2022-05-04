using System.Collections.Generic;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		// Built-in Components
		static internal readonly EcsId.Entity TypeId       = new(0x01);
		static internal readonly EcsId.Entity IdentifierId = new(0x02);

		// Built-in Tags
		static internal readonly EcsId.Entity ComponentId = new(0x10);
		static internal readonly EcsId.Entity TagId       = new(0x11);
		static internal readonly EcsId.Entity RelationId  = new(0x12);

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
			Entities.NewWithId(TypeId.Id);
			Entities.NewWithId(IdentifierId.Id);
			// Built-in Tags required to bootstrap.
			Entities.NewWithId(ComponentId.Id);
			Entities.NewWithId(TagId.Id);
			// Bootstrap table structures so other methods can work.
			Tables.Bootstrap();


			// Build-in Tags
			Entities.NewWithId(RelationId.Id).Add(typeof(Tag)).Set(typeof(Relation));

			// Build-in Relations
			Entities.NewWithId(ChildOf.Id).Add(typeof(Tag), typeof(Relation)).Set(typeof(ChildOf));
			Entities.NewWithId(IsA.Id    ).Add(typeof(Tag), typeof(Relation)).Set(typeof(IsA));

			// Special "Wildcard" (*), used when looking up relationships
			Entities.NewWithId(Wildcard.Id).Set((Identifier)"*");


			NewComponent<Common.Transform>();
		}

		public EcsId NewComponent<T>()
			=> Entities.New()
				.Add(typeof(Component))
				.Set(typeof(T))
				.Set((Identifier)typeof(T).Name);
	}
}
