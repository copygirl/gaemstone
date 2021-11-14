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
		public static readonly EcsId COMPONENT_ID  = new(0x01);
		public static readonly EcsId IDENTIFIER_ID = new(0x02);

		public static readonly EcsId IsA     = new(0x100);
		public static readonly EcsId ChildOf = new(0x101);

		public EntityManager    Entities   { get; }
		public ComponentManager Components { get; }
		public ProcessorManager Processors { get; }
		public QueryManager     Queries    { get; }

		IComponentStore<Prototype>? _prototypes;
		IComponentStore<Prototype> Prototypes
			=> _prototypes ??= Components.GetStore<Prototype>();

		public Universe()
		{
			Entities   = new();
			Components = new(Entities);
			Processors = new(this);
			Queries    = new(this);
		}

		public bool TryGetOwn<T>(EcsId entity, [NotNullWhen(true)] out T value)
		{
			EnsureEntityIsAlive(entity);
			return Components.GetStore<T>().TryGet(entity.ID, out value);
		}
		public bool TryGet<T>(EcsId entity, [NotNullWhen(true)] out T value)
		{
			EnsureEntityIsAlive(entity);
			var store = Components.GetStore<T>();
			foreach (var e in GetPrototypeChain(entity))
				if (store.TryGet(e.ID, out value)) return true;
			value = default!;
			return false;
		}

		public T GetOwn<T>(EcsId entity)
			=> TryGetOwn<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(typeof(T), entity);
		public T Get<T>(EcsId entity)
			=> TryGet<T>(entity, out var value) ? value
				: throw new ComponentNotFoundException(typeof(T), entity);

		public bool HasOwn<T>(EcsId entity)
			=> TryGetOwn<T>(entity, out _);
		public bool Has<T>(EcsId entity)
			=> TryGet<T>(entity, out _);

		public void Set<T>(EcsId entity, T value)
		{
			EnsureEntityIsAlive(entity);
			Components.GetStore<T>().Set(entity.ID, value);
		}


		public IEnumerable<(EcsId, T)> GetAll<T>()
		{
			var store = Components.GetStore<T>();
			if (store == null) yield break;

			var enumerator = store.GetEnumerator();
			while (enumerator.MoveNext())
				yield return (
					Entities.Lookup(enumerator.CurrentEntityID)!.Value,
					enumerator.CurrentComponent
				);
		}


		IEnumerable<EcsId> GetPrototypeChain(EcsId entity)
		{
			while (true) {
				yield return entity;
				if (!Prototypes.TryGet(entity.ID, out var prototype)
					|| !Entities.IsAlive(prototype.Value)) break;
				entity = prototype.Value;
			}
		}

		void EnsureEntityIsAlive(EcsId entity)
		{
			if (!Entities.IsAlive(entity))
				throw new ArgumentException($"{entity} is not alive");
		}
	}
}
