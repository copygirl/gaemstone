using System;
using System.Diagnostics;

namespace gaemstone.Common.Utility
{
	public static class HashHelper
	{
		// CoreLib => System.Numerics.Hashing.HashHelpers
		// https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Numerics/Hashing/HashHelpers.cs

		public static int Combine(int h1, int h2)
		{
			uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
			return ((int)rol5 + h1) ^ h2;
		}
		public static int Combine(int h1, int h2, int h3)
			=> Combine(Combine(h1, h2), h3);
		public static int Combine(int h1, int h2, int h3, int h4)
			=> Combine(Combine(Combine(h1, h2), h3), h4);
		public static int Combine(int h1, int h2, int h3, int h4, int h5)
			=> Combine(Combine(Combine(Combine(h1, h2), h3), h4), h5);

		public static int Combine(params int[] hashCodes)
		{
			if (hashCodes == null) throw new ArgumentNullException(nameof(hashCodes));
			if (hashCodes.Length == 0) throw new ArgumentException(
				"Argument 'hashCodes' is empty", nameof(hashCodes));
			var hash = hashCodes[0];
			for (var i = 1; i < hashCodes.Length; i++)
				hash = Combine(hash, hashCodes[i]);
			return hash;
		}

		public static int Combine<A, B>(A a, B b)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0);
		public static int Combine<A, B, C>(A a, B b, C c)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0, c?.GetHashCode() ?? 0);
		public static int Combine<A, B, C, D>(A a, B b, C c, D d)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0,
			           c?.GetHashCode() ?? 0, d?.GetHashCode() ?? 0);
		public static int Combine<A, B, C, D, E>(A a, B b, C c, D d, E e)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0, c?.GetHashCode() ?? 0,
			           d?.GetHashCode() ?? 0, e?.GetHashCode() ?? 0);

		public static int Combine(params object[] objects)
		{
			if (objects == null) throw new ArgumentNullException(nameof(objects));
			if (objects.Length == 0) throw new ArgumentException(
				"Argument 'objects' is empty", nameof(objects));
			var hash = objects[0].GetHashCode();
			for (var i = 1; i < objects.Length; i++)
				hash = Combine(hash, objects[i].GetHashCode());
			return hash;
		}


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
