using System.Collections.Generic;
using gaemstone.Common.Utility;

namespace gaemstone.Common.ECS.Stores
{
	public class LookupDictionaryStore<TKey, T>
			: DictionaryStore<T>
		where TKey : struct
	{
		private readonly Dictionary<TKey, uint> _lookup
			= new Dictionary<TKey, uint>();
		private readonly KeyLookupFunc _lookupFunc;

		public LookupDictionaryStore(KeyLookupFunc lookupFunc)
		{
			_lookupFunc = lookupFunc;
			ComponentChanged += OnComponentChanged;
		}

		public void OnComponentChanged(uint entityID, ref T oldValue, ref T newValue)
		{
			if (!RefHelper.IsNull(ref oldValue)) {
				var key = _lookupFunc(oldValue);
				_lookup.Remove(key);
			}
			if (!RefHelper.IsNull(ref newValue)) {
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
