using System.Runtime.CompilerServices;
using gaemstone.ECS;

namespace gaemstone
{
	public partial class Universe
	{
		public class EntityManager : ECS.EntityManager
		{
			public EntityManager(Universe universe)
				: base(universe) {  }


			/// <summary> Creates a new entity with an empty type and returns it. </summary>
			public Entity New()
				=> New(Universe.EmptyType);

			/// <summary> Creates a new entity with the specified type and returns it. </summary>
			public Entity New(params object[] ids)
				=> New(Universe.Type(ids));

			/// <summary> Creates a new entity with the specified type and returns it. </summary>
			public Entity New(params EcsId[] ids)
				=> New(Universe.Type(ids));

			/// <summary> Creates a new entity with the specified type and returns it. </summary>
			public Entity New(EntityType type)
			{
				ref var record = ref NewRecord(type, out var entityId);
				return new(Universe, new(entityId, record.Generation));
			}


			/// <summary> Creates a new entity with the specifiedid and an empty type and returns it. </summary>
			/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
			public Entity NewWithId(uint entityId)
				=> NewWithId(entityId, Universe.EmptyType);

			/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
			/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
			public Entity NewWithId(uint entityId, params object[] ids)
				=> NewWithId(entityId, Universe.Type(ids));

			/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
			/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
			public Entity NewWithId(uint entityId, params EcsId[] ids)
				=> NewWithId(entityId, Universe.Type(ids));

			/// <summary> Creates a new entity with the specified id and type and returns it. </summary>
			/// <exception cref="EntityExistsException"> Thrown if the specified id is already in use. </exception>
			public Entity NewWithId(uint entityId, EntityType type)
			{
				ref var record = ref NewRecordWithId(type, entityId);
				return new(Universe, new(entityId, record.Generation));
			}


			public Entity Lookup(uint entityId)
				=> TryLookup(entityId, out var entity) ? entity
					: throw new EntityNotFoundException(entityId);

			public bool TryLookup(uint entityId, out Entity entity)
			{
				ref var record = ref GetRecordOrNull(entityId);
				if (Unsafe.IsNullRef(ref record)) { entity = default; return false; }
				else { entity = new(Universe, new(entityId, record.Generation)); return true; }
			}
		}
	}
}
