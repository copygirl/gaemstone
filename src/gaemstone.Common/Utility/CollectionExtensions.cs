using System;
using System.Collections.Generic;

namespace gaemstone.Common.Utility
{
	public static class CollectionExtensions
	{
		public static TValue? GetOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue?> dict, TKey key)
			where TKey : notnull
		{
			if (!dict.TryGetValue(key, out var value))
				dict.Add(key, value = default);
			return value;
		}
		public static TValue GetOrAddNew<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
			where TKey : notnull
			where TValue : new()
		{
			if (!dict.TryGetValue(key, out var value))
				dict.Add(key, value = new());
			return value;
		}
		public static TValue GetOrAddNew<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue @default)
			where TKey : notnull
		{
			if (!dict.TryGetValue(key, out var value))
				dict.Add(key, value = @default);
			return value;
		}
		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> createFunc)
			where TKey : notnull
		{
			if (!dict.TryGetValue(key, out var value))
				dict.Add(key, value = createFunc());
			return value;
		}
		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> createFunc)
			where TKey : notnull
		{
			if (!dict.TryGetValue(key, out var value))
				dict.Add(key, value = createFunc(key));
			return value;
		}
	}
}
