using System.Collections.Generic;
using System;

namespace gaemstone.Common.Utility
{
	public static class RandomExtensions
	{
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
	}
}
