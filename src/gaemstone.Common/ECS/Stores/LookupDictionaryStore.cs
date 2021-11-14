using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace gaemstone.Common.ECS.Stores
{
	public class LookupDictionaryStore<TKey, T>
			: DictionaryStore<T>
		where TKey : struct
	{
		readonly Dictionary<TKey, uint> _lookup = new();
		readonly KeyLookupFunc _lookupFunc;

		public LookupDictionaryStore(KeyLookupFunc lookupFunc)
		{
			_lookupFunc = lookupFunc;
			ComponentChanged += OnComponentChanged;
		}

		public void OnComponentChanged(uint entityID, ref T oldValue, ref T newValue)
		{
			if (!Unsafe.IsNullRef(ref oldValue)) {
				var key = _lookupFunc(oldValue);
				_lookup.Remove(key);
			}
			if (!Unsafe.IsNullRef(ref newValue)) {
				var key = _lookupFunc(newValue);
				_lookup.Add(key, entityID);
			}
		}

		public uint GetEntityID(TKey key)
			=> _lookup.TryGetValue(key, out uint entityID) ? entityID
				: throw new KeyNotFoundException($"The key '{key}' was not found");

		public bool TryGetEntityID(TKey key, out uint entityID)
			=> _lookup.TryGetValue(key, out entityID);

		public delegate TKey KeyLookupFunc(T component);
	}
}
