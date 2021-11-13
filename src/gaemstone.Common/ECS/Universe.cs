using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using gaemstone.Common.Components;
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

		private IComponentStore<Prototype>? _prototypes;
		private IComponentStore<Prototype> Prototypes
			=> (_prototypes ?? (_prototypes = Components.GetStore<Prototype>()));

		public Universe()
		{
			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Processors = new ProcessorManager(this);
			Queries    = new QueryManager(this);
		}

		public bool TryGetOwn<T>(Entity entity, [NotNullWhen(true)] out T value)
		{
			EnsureEntityIsAlive(entity);
			return Components.GetStore<T>().TryGet(entity.ID, out value);
		}
		public bool TryGet<T>(Entity entity, [NotNullWhen(true)] out T value)
		{
			EnsureEntityIsAlive(entity);
			var store = Components.GetStore<T>();
			foreach (var e in GetPrototypeChain(entity))
				if (store.TryGet(e.ID, out value)) return true;
			value = default(T)!;
			return false;
		}

		public T GetOwn<T>(Entity entity)
			=> TryGetOwn<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(typeof(T), entity);
		public T Get<T>(Entity entity)
			=> TryGet<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(typeof(T), entity);

		public bool HasOwn<T>(Entity entity)
			=> TryGetOwn<T>(entity, out _);
		public bool Has<T>(Entity entity)
			=> TryGet<T>(entity, out _);

		public void Set<T>(Entity entity, T value)
		{
			EnsureEntityIsAlive(entity);
			Components.GetStore<T>().Set(entity.ID, value);
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


		private IEnumerable<Entity> GetPrototypeChain(Entity entity)
		{
			while (true) {
				yield return entity;
				if (!Prototypes.TryGet(entity.ID, out var prototype)
					|| !Entities.IsAlive(prototype.Value)) break;
				entity = prototype.Value;
			}
		}

		private void EnsureEntityIsAlive(Entity entity)
		{
			if (!Entities.IsAlive(entity))
				throw new ArgumentException($"{entity} is not alive");
		}
	}
}
