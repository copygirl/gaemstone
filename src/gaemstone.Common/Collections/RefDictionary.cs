using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace gaemstone.Common.Collections
{
	public enum GetBehavior
	{
		Default,
		Create,
		Remove,
	}

	public class RefDictionary<TKey, TValue>
		where TKey : struct
	{
		public struct Entry
		{
			internal int _next;

			public TValue Value;

			public TKey Key { get; internal set; }
			public int HashCode { get; internal set; }
			public bool HasValue => (HashCode >= 0);
		}

		private static Entry MISSING_ENTRY
			= new Entry { HashCode = -1 };


		private readonly IEqualityComparer<TKey> _comparer;

		private int[]? _buckets;
		private Entry[]? _entries;
		private int _count;
		private int _version;
		private int _freeEntry;
		private int _freeCount;

		public int Count => (_count - _freeCount);

		public int Capacity {
			get => _entries?.Length ?? 0;
			set => Resize(value);
		}


		public RefDictionary()
			: this(0, EqualityComparer<TKey>.Default) {  }
		public RefDictionary(int capacity)
			: this(capacity, EqualityComparer<TKey>.Default) {  }
		public RefDictionary(IEqualityComparer<TKey> comparer)
			: this(0, comparer) {  }

		public RefDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
			if (comparer == null) throw new ArgumentNullException(nameof(comparer));
			if (capacity > 0) Initialize(capacity);
			_comparer = comparer;
		}


		private void Initialize(int capacity)
		{
			int size = HashHelpers.GetPrime(capacity);
			_buckets = new int[size];
			_entries = new Entry[size];
			ArrayFill(_buckets, -1);
			_freeEntry = -1;
		}

		public void Clear()
		{
			if (_count == 0) return;
			ArrayFill(_buckets!, -1);
			Array.Clear(_entries!, 0, _count);
			_count     =  0;
			_freeEntry = -1;
			_freeCount =  0;
			_version++;
		}

		private void Resize()
			=> Resize(HashHelpers.ExpandPrime(_count));

		private void Resize(int newSize)
		{
			if (_entries == null) {
				Initialize(newSize);
				return;
			}

			if (newSize < _entries.Length)
				throw new ArgumentOutOfRangeException(nameof(newSize));

			var newBuckets = new int[newSize];
			var newEntries = new Entry[newSize];
			ArrayFill(newBuckets, -1);
			Array.Copy(_entries, 0, newEntries, 0, _count);

			for (int i = 0; i < _count; i++) {
				ref var entry = ref newEntries[i];
				if (entry.HashCode < 0) continue;
				var bucket = (entry.HashCode % newSize);
				entry._next = newBuckets[bucket] - 1;
				newBuckets[bucket] = i + 1;
			}

			_buckets = newBuckets;
			_entries = newEntries;
			_version++;
		}

		/// <summary> Helper function to fill an array with a single value. </summary>
		private static void ArrayFill<T>(T[] array, T value)
		{
			for (var i = 0; i < array.Length; i++)
				array[i] = value;
		}


		public ref Entry TryGetEntry(GetBehavior behavior, TKey key)
		{
			if (_buckets == null) {
				if (behavior != GetBehavior.Create)
					return ref MISSING_ENTRY;
				Initialize(0);
			}

			var hashCode   = _comparer.GetHashCode(key) & 0x7FFFFFFF;
			ref var bucket = ref _buckets![hashCode % _buckets.Length];

			var last = -1;
			for (var i = bucket - 1; i >= 0; ) {
				ref var entry = ref _entries![i];
				if ((entry.HashCode == hashCode) && _comparer.Equals(entry.Key, key)) {
					if (behavior == GetBehavior.Remove) {
						if (last < 0) bucket = entry._next + 1;
						else _entries[last]._next = entry._next;

						entry._next    = _freeEntry;
						entry.Key      = default(TKey);
						entry.HashCode = -1;
						// Not resetting allows us to return previous value.
						// entry.Value    = default(TComponent);

						_freeEntry = i;
						_freeCount++;
						_version++;
					}

					return ref entry;
				}
				last = i;
				i    = entry._next;
			}

			if (behavior != GetBehavior.Create)
				return ref MISSING_ENTRY;

			int index;
			if (_freeCount > 0) {
				index      = _freeEntry;
				_freeEntry = _entries![index]._next;
				_freeCount--;
			} else {
				if (_count == _entries!.Length) {
					Resize();
					bucket = ref _buckets[hashCode % _buckets.Length];
				}
				index = _count;
				_count++;
			}

			{
				ref var entry  = ref _entries[index];
				entry._next    = bucket - 1;
				entry.Value    = default!;
				entry.Key      = key;
				entry.HashCode = hashCode;

				bucket = index + 1;
				_version++;

				return ref entry;
			}
		}


		// Enumeration

		public Enumerator GetEnumerator()
			=> new Enumerator(this);

		public struct Enumerator
		{
			private readonly RefDictionary<TKey, TValue> _dict;
			private int _index;
			private int _version;

			internal Enumerator(RefDictionary<TKey, TValue> dict)
			{
				_dict    = dict;
				_index   = -1;
				_version = _dict._version;
			}

			public bool MoveNext()
			{
				if (_version != _dict._version) throw new InvalidOperationException(
					"Collection has been modified during enumeration");
				while (++_index < _dict._count)
					if (_dict._entries![_index].HasValue)
						return true;
				return false;
			}

			public ref Entry Current => ref _dict._entries![_index];
		}


		static class HashHelpers
		{
			// CoreLib => System.Collections.HashHelpers
			// https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Collections/HashHelpers.cs

			public const int MAX_PRIME_ARRAY_LENGTH = 0x7FEFFFFD;

			public const int HASH_PRIME = 101;

			private static readonly int[] _primes = {
				3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
				1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519,
				21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307,
				270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191,
				2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
			};

			public static bool IsPrime(int candidate)
			{
				if ((candidate & 1) != 0) {
					int limit = (int)Math.Sqrt(candidate);
					for (int divisor = 3; divisor <= limit; divisor += 2)
						if ((candidate % divisor) == 0)
							return false;
					return true;
				}
				return (candidate == 2);
			}

			public static int GetPrime(int min)
			{
				if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
				for (int i = 0; i < _primes.Length; i++) {
					int prime = _primes[i];
					if (prime >= min)
						return prime;
				}
				for (int i = (min | 1); i < int.MaxValue; i += 2)
					if (IsPrime(i) && ((i - 1) % HASH_PRIME != 0))
						return i;
				return min;
			}

			public static int ExpandPrime(int oldSize)
			{
				int newSize = 2 * oldSize;
				if (((uint)newSize > MAX_PRIME_ARRAY_LENGTH) && (MAX_PRIME_ARRAY_LENGTH > oldSize)) {
					Debug.Assert(MAX_PRIME_ARRAY_LENGTH == GetPrime(MAX_PRIME_ARRAY_LENGTH), "Invalid MaxPrimeArrayLength");
					return MAX_PRIME_ARRAY_LENGTH;
				}
				return GetPrime(newSize);
			}
		}
	}
}
