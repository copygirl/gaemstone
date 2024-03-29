using System;
using System.Diagnostics.CodeAnalysis;
using gaemstone.ECS;

namespace gaemstone
{
	public partial class Universe
	{
		public bool Has<T>(EcsId.Entity entity)
		{
			var id   = Lookup<T>();
			var type = Entities.GetRecord(entity).Type;
			return type.Contains(id);
		}


		public bool TryGet<T>(EcsId.Entity entity, [NotNullWhen(true)] out T? value)
		{
			var id = Lookup<T>();
			ref var record = ref Entities.GetRecord(entity);
			if (record.Table.TryGetStorageColumn<T>(id, out var column))
				{ value = column[record.Row]!; return true; }
			else { value = default!; return false; }
		}

		public T Get<T>(EcsId.Entity entity)
			=> TryGet<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(entity, typeof(T));

		public ref T GetRef<T>(EcsId.Entity entity)
		{
			var id = Lookup<T>();
			ref var record = ref Entities.GetRecord(entity);
			var column = record.Table.GetStorageColumn<T>(id, entity);
			return ref column[record.Row];
		}


		public bool TrySet<T>(EcsId.Entity entity, T value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			if (!TryLookup<T>(out var id)) return false;
			ref var record = ref Entities.GetRecord(entity);

			// TODO: Should TrySet() automatically add the component if it doesn't have it?
			Tables.Move(entity, ref record, record.Type.Union(id));

			if (!record.Table.TryGetStorageColumn<T>(id, out var column)) return false;
			column[record.Row] = value;
			return true;
		}

		public void Set<T>(EcsId.Entity entity, T value)
		{
			if (TrySet(entity, value)) return; // Successful.
			throw new ComponentNotFoundException(entity, typeof(T), "Not a Component");
		}
	}
}
