using System.Collections.Generic;
using System.Linq;

namespace gaemstone.Common.Utility
{
	public static class LinqExtensions
	{
		public static IEnumerable<(int, T)> Indexed<T>(this IEnumerable<T> self)
			=> self.Select((element, index) => (index, element));
	}
}
