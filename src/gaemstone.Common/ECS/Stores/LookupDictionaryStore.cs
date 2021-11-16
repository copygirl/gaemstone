using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace gaemstone.Common.ECS.Stores
{
	public class LookupDictionaryStore<TKey, T>
		: IComponentStore<T>
		where TKey : notnull
	{
		readonly DictionaryStore<T> _base = new();
		readonly Dictionary<TKey, uint> _lookup = new();
		readonly Func<T, TKey> _lookupFunc;

		public Type ComponentType => _base.ComponentType;
		public int Count => _base.Count;

		public LookupDictionaryStore(Func<T, TKey> lookupFunc)
			=>  _lookupFunc = lookupFunc;


		public bool TryGetEntityID(TKey key, out uint entityID)
			=> _lookup.TryGetValue(key, out entityID);

		public uint GetEntityID(TKey key)
			=> _lookup.TryGetValue(key, out uint entityID) ? entityID
				: throw new KeyNotFoundException($"The key '{key}' was not found");


		public bool Has(uint entityID)
			=>_base.Has(entityID);

		public bool TryGet(uint entityID, [NotNullWhen(true)] out T value)
			=> _base.TryGet(entityID, out value);

		public bool TryAdd(uint entityID, T value)
		{
			var success = _base.TryAdd(entityID, value);
			if (success) _lookup.Add(_lookupFunc(value), entityID);
			return success;
		}

		public bool Set(uint entityID, T value, [NotNullWhen(true)] out T previous)
		{
			var found = _base.Set(entityID, value, out previous);
			if (!found) _lookup.Add(_lookupFunc(value), entityID);
			return found;
		}

		public bool TryRemove(uint entityID, [NotNullWhen(true)] out T previous)
		{
			var found = _base.TryRemove(entityID, out previous);
			if (found && !_lookup.Remove(_lookupFunc(previous)))
				throw new InvalidOperationException(); // Should not occur.
			return found;
		}

		public IComponentStore<T>.IEnumerator GetEnumerator() =>_base.GetEnumerator();
		IComponentStore.IEnumerator IComponentStore.GetEnumerator() => _base.GetEnumerator();
	}
}
