using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using gaemstone.Common.Utility;

namespace gaemstone.Common.Stores
{
	public class DictionaryStore<T>
		: IComponentStore<T>
	{
		readonly RefDictionary<uint, T> _dict = new();

		public Type ComponentType { get; } = typeof(T);
		public int Count => _dict.Count;


		public bool Has(uint entityID)
			=> _dict.ContainsKey(entityID);

		public bool TryGet(uint entityID, [NotNullWhen(true)] out T value)
			=> _dict.TryGetValue(entityID, out value!);

		public bool TryAdd(uint entityID, T value)
		{
			ref var entry = ref _dict.GetRef(GetBehavior.Add, entityID);
			if (Unsafe.IsNullRef(ref entry)) return false;
			entry = value;
			return true;
		}

		public bool Set(uint entityID, T value, [NotNullWhen(true)] out T previous)
		{
			var previousCount = Count;
			ref var entry = ref _dict.GetRef(GetBehavior.Create, entityID);
			previous = entry;
			entry = value;
			return (Count == previousCount);
		}

		public bool TryRemove(uint entityID, [NotNullWhen(true)] out T previous)
		{
			var previousCount = Count;
			previous = _dict.GetRef(GetBehavior.Remove, entityID);
			return (Count == previousCount);
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
