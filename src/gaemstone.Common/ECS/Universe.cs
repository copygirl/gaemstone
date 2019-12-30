using System;
using System.Collections.Generic;
using gaemstone.Common.ECS.Processors;

namespace gaemstone.Common.ECS
{
	public class Universe
	{
		public EntityManager Entities { get; }
		public ComponentManager Components { get; }
		public ProcessorManager Processors { get; }

		public Universe()
		{
			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Processors = new ProcessorManager(this);
		}

		public T Get<T>(Entity entity)
		{
			if (!Entities.IsAlive(entity))
				throw new ArgumentException($"{entity} is not alive");
			var store = Components.GetStore<T>() ?? throw new InvalidOperationException(
				$"No store in Components for type {typeof(T)}");
			return store.Get(entity.ID);
		}

		public void Set<T>(Entity entity, T value)
		{
			if (!Entities.IsAlive(entity))
				throw new ArgumentException($"{entity} is not alive");
			var store = Components.GetStore<T>() ?? throw new InvalidOperationException(
				$"No store in Components for type {typeof(T)}");
			store.Set(entity.ID, value);
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
	}
}
