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

		public void OnComponentChanged(uint entityID, NullableRef<T> oldValue, NullableRef<T> newValue)
		{
			if (!oldValue.IsNull) {
				var key = _lookupFunc(in oldValue.Reference);
				_lookup.Remove(key);
			}
			if (!newValue.IsNull) {
				var key = _lookupFunc(in newValue.Reference);
				_lookup.Add(key, entityID);
			}
		}

		public delegate TKey KeyLookupFunc(in T component);
	}
}
