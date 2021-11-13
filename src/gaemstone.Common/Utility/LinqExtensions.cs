using System.Collections.Generic;
using System.Linq;

namespace gaemstone.Common.Utility
{
	public static class LinqExtensions
	{
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> self, params T[] elements)
			=> elements.Concat(self);

		public static IEnumerable<T> Append<T>(this IEnumerable<T> self, params T[] elements)
			=> self.Concat(elements);

		public static IEnumerable<(int, T)> Indexed<T>(this IEnumerable<T> self)
			=> self.Select((element, index) => (index, element));
	}
}
