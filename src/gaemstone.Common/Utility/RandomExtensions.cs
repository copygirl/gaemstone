using System;
using System.Collections.Generic;

namespace gaemstone.Common.Utility
{
	public static class RandomExtensions
	{
		public static bool NextBool(this Random rnd, double chance)
			=> rnd.NextDouble() < chance;

		public static double NextDouble(this Random rnd, double max)
			=> rnd.NextDouble() * max;
		public static double NextDouble(this Random rnd, double min, double max)
			=> min + rnd.NextDouble() * (max - min);

		public static float NextFloat(this Random rnd)
			=> (float)rnd.NextDouble();
		public static float NextFloat(this Random rnd, float max)
			=> (float)rnd.NextDouble() * max;
		public static float NextFloat(this Random rnd, float min, float max)
			=> min + (float)rnd.NextDouble() * (max - min);

		public static T Pick<T>(this Random rnd, params T[] elements)
			=> elements[rnd.Next(elements.Length)];
		public static T Pick<T>(this Random rnd, IReadOnlyList<T> elements)
			=> elements[rnd.Next(elements.Count)];
		public static T Pick<T>(this Random rnd, Span<T> elements)
			=> elements[rnd.Next(elements.Length)];

#pragma warning disable CS8509
		public static T Pick<T>(this Random rnd, T elem1, T elem2)
			=> rnd.Next(2) switch { 0 => elem1, 1 => elem2 };
		public static T Pick<T>(this Random rnd, T elem1, T elem2, T elem3)
			=> rnd.Next(3) switch { 0 => elem1, 1 => elem2, 2 => elem3 };
		public static T Pick<T>(this Random rnd, T elem1, T elem2, T elem3, T elem4)
			=> rnd.Next(4) switch { 0 => elem1, 1 => elem2, 2 => elem3, 3 => elem4 };
#pragma warning restore CS8509
	}
}
