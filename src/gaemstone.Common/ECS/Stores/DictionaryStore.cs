using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using gaemstone.Common.Collections;

namespace gaemstone.Common.ECS.Stores
{
	public class DictionaryStore<T>
		: IComponentStore<T>
	{
		readonly RefDictionary<uint, T> _dict = new();

		public Type ComponentType { get; } = typeof(T);
		public int Count => _dict.Count;

		public event ComponentAddedHandler? ComponentAdded;
		public event ComponentRemovedHandler? ComponentRemoved;
		public event ComponentChangedHandler<T>? ComponentChanged;


		public bool TryGet(uint entityID, [NotNullWhen(true)] out T value)
			=> _dict.TryGetValue(entityID, out value!);

		public bool Has(uint entityID)
			=> _dict.ContainsKey(entityID);

		public void Set(uint entityID, T value)
		{
			var previousCount = _dict.Count;
			ref var entry = ref _dict.GetRef(GetBehavior.Create, entityID);
			var entryAdded = (_dict.Count > previousCount);
			if (entryAdded) ComponentAdded?.Invoke(entityID);
			ref var oldValue = ref (entryAdded ? ref Unsafe.NullRef<T>() : ref value);
			ComponentChanged?.Invoke(entityID, ref oldValue, ref value);
			entry = value;
		}

		public bool Remove(uint entityID)
		{
			var previousCount = _dict.Count;
			ref var entry = ref _dict.GetRef(GetBehavior.Remove, entityID);
			if (_dict.Count < previousCount) {
				ComponentRemoved?.Invoke(entityID);
				ComponentChanged?.Invoke(entityID, ref entry, ref Unsafe.NullRef<T>());
				return true;
			} else return false;
		}


		public IComponentStore<T>.IEnumerator GetEnumerator() => new Enumerator(_dict);
		IComponentStore.IEnumerator IComponentStore.GetEnumerator() => new Enumerator(_dict);

		struct Enumerator
			: IComponentStore<T>.IEnumerator
		{
			RefDictionary<uint, T>.Enumerator _dictEnumerator;
			public Enumerator(RefDictionary<uint, T> dict)
				=> _dictEnumerator = dict.GetEnumerator();

			object IComponentStore.IEnumerator.CurrentComponent => _dictEnumerator.Current.Value!;
			public T CurrentComponent => _dictEnumerator.Current.Value!;
			public uint CurrentEntityID => _dictEnumerator.Current.Key;
			public bool MoveNext() => _dictEnumerator.MoveNext();
		}
	}
}
