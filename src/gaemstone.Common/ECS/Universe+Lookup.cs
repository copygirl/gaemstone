using System;

namespace gaemstone.ECS
{
	public partial class Universe
	{
		public Entity Lookup(object obj)
		{
			if (TryLookup(obj, out var entity)) return entity;
			throw new EntityNotFoundException(obj);
		}

		public bool TryLookup(object obj, out Entity entity)
		{
			switch (obj) {
				case Entity       e: entity = e;            return true;
				case EcsId.Entity e: entity = new(this, e); return true;
				case EcsId        i: if (i.AsEntity() is EcsId.Entity e2) { entity = new(this, e2); return true; } else break;
				case uint         i: return Entities.TryLookup(i, out entity);
				case Type         t: return TryLookup(t, out entity);
				case Identifier   i: return TryLookup(i, out entity);
				case string       n: return TryLookup(n, out entity);
			}
			throw new ArgumentException($"Must be Universe.Entity, EcsId.Entity, uint, Type, Identifier or string", nameof(obj));
		}


		public Entity Lookup<T>() => Lookup(typeof(T));
		public Entity Lookup(Type type) => TryLookup(type, out var id) ? id
			: throw new InvalidOperationException($"Entity with Type={type.Name} not found");

		public bool TryLookup<T>(out Entity entity) => TryLookup(typeof(T), out entity);
		public bool TryLookup(Type type, out Entity entity)
		{
			foreach (var table in Tables.GetAll(TypeId)) {
				var column = table.GetStorageColumn<Type>(TypeId);
				for (var i = 0; i < table.Count; i++) if (column[i] == type)
					{ entity = new(this, table.Entities[i]); return true; }
			}
			entity = default;
			return false;
		}


		public Entity Lookup(string name) => TryLookup(name, out var id) ? id
			: throw new InvalidOperationException($"Entity with Identifer='{name}' not found");

		public bool TryLookup(string name, out Entity entity)
		{
			foreach (var table in Tables.GetAll(IdentifierId)) {
				var column = table.GetStorageColumn<Identifier>(IdentifierId);
				for (var i = 0; i < table.Count; i++) if (column[i] == name)
					{ entity = new(this, table.Entities[i]); return true; }
			}
			entity = default;
			return false;
		}
	}
}
