using System;
using System.Collections.Generic;
using gaemstone.Common.Utility;

namespace gaemstone.Common.Collections
{
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
			int size = HashHelper.GetPrime(capacity);
			_buckets = new int[size];
			_entries = new Entry[size];
			ArrayFill(_buckets, -1);
			_freeEntry = -1;
		}

		public void Clear()
		{
			if (_count == 0) return;
			ArrayFill(_buckets!, -1);
			Array.Clear(_entries, 0, _count);
			_count     =  0;
			_freeEntry = -1;
			_freeCount =  0;
			_version++;
		}

		private void Resize()
			=> Resize(HashHelper.ExpandPrime(_count));

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
	}

	public enum GetBehavior
	{
		Default,
		Create,
		Remove,
	}
}
