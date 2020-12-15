using System;
using gaemstone.Common.Collections;
using gaemstone.Common.Utility;

namespace gaemstone.Common.ECS.Stores
{
	public class DictionaryStore<T>
		: IComponentStore<T>
	{
		private RefDictionary<uint, T> _dict { get; }
			= new RefDictionary<uint, T>();

		public Type ComponentType { get; } = typeof(T);
		public int Count => _dict.Count;

		public event ComponentAddedHandler? ComponentAdded;
		public event ComponentRemovedHandler? ComponentRemoved;
		public event ComponentChangedHandler<T>? ComponentChanged;


		public T Get(uint entityID)
		{
			ref var entry = ref _dict.TryGetEntry(GetBehavior.Default, entityID);
			if (!entry.HasValue) throw new ComponentNotFoundException(this, entityID);
			return entry.Value;
		}

		public bool Has(uint entityID)
			=> _dict.TryGetEntry(GetBehavior.Default, entityID).HasValue;

		public bool TryGet(uint entityID, out T value)
		{
			ref var entry = ref _dict.TryGetEntry(GetBehavior.Default, entityID);
			value = (entry.HasValue ? entry.Value : default(T)!);
			return entry.HasValue;
		}

		public void Set(uint entityID, T value)
		{
			var previousCount = _dict.Count;
			ref var entry = ref _dict.TryGetEntry(GetBehavior.Create, entityID);
			var entryAdded = (_dict.Count > previousCount);
			if (entryAdded) ComponentAdded?.Invoke(entityID);
			ref var oldValue = ref (entryAdded ? ref RefHelper.Null<T>() : ref entry.Value);
			ComponentChanged?.Invoke(entityID, ref oldValue, ref value);
			entry.Value = value;
		}

		public void Remove(uint entityID)
		{
			var previousCount = _dict.Count;
			ref var entry = ref _dict.TryGetEntry(GetBehavior.Remove, entityID);
			if (_dict.Count < previousCount) {
				ComponentRemoved?.Invoke(entityID);
				ComponentChanged?.Invoke(entityID, ref entry.Value, ref RefHelper.Null<T>());
			}
			else throw new ComponentNotFoundException(this, entityID);
		}


		public IComponentStore<T>.Enumerator GetEnumerator()
			=> new Enumerator(_dict);
		IComponentStore.Enumerator IComponentStore.GetEnumerator()
			=> new Enumerator(_dict);

		private struct Enumerator
			: IComponentStore<T>.Enumerator
		{
			private RefDictionary<uint, T>.Enumerator _dictEnumerator;
			public Enumerator(RefDictionary<uint, T> dict)
				=> _dictEnumerator = dict.GetEnumerator();

			public uint CurrentEntityID => _dictEnumerator.Current.Key;
			public T CurrentComponent => _dictEnumerator.Current.Value;
			public bool MoveNext() => _dictEnumerator.MoveNext();
		}
	}
}
