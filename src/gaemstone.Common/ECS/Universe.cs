using System;
using System.Collections.Generic;
using gaemstone.Common.ECS.Processors;
using gaemstone.Common.ECS.Stores;

namespace gaemstone.Common.ECS
{
	public class Universe
	{
		public EntityManager    Entities   { get; }
		public ComponentManager Components { get; }
		public ProcessorManager Processors { get; }
		public QueryManager     Queries    { get; }

		public Universe()
		{
			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Processors = new ProcessorManager(this);
			Queries    = new QueryManager(this);
		}

		public T Get<T>(Entity entity)
		{
			EnsureEntityIsAlive(entity);
			return GetStoreOrThrow<T>().Get(entity.ID);
		}

		public bool Has<T>(Entity entity)
		{
			EnsureEntityIsAlive(entity);
			return Components.GetStore<T>()?.Has(entity.ID) ?? false;
		}

		public void Set<T>(Entity entity, T value)
		{
			EnsureEntityIsAlive(entity);
			GetStoreOrThrow<T>().Set(entity.ID, value);
		}

		public IEnumerable<(Entity, T)> GetAll<T>()
		{
			var store = Components.GetStore<T>();
			if (store == null) yield break;

			var enumerator = store.GetEnumerator();
			while (enumerator.MoveNext())
				yield return (
					Entities.GetByID(enumerator.CurrentEntityID)!.Value,
					enumerator.CurrentComponent
				);
		}


		private void EnsureEntityIsAlive(Entity entity)
		{
			if (!Entities.IsAlive(entity))
				throw new ArgumentException($"{entity} is not alive");
		}

		private IComponentStore<T> GetStoreOrThrow<T>()
			=> Components.GetStore<T>() ?? throw new InvalidOperationException(
				$"No store in {nameof(Components)} for type {typeof(T)}");
	}
}
