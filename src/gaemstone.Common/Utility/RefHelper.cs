using System.Runtime.CompilerServices;

namespace gaemstone.Common.Utility
{
	public static class RefHelper
	{
		public static ref T Null<T>()
		{ unsafe {
			return ref Unsafe.AsRef<T>(null);
		} }

		public static bool IsNull<T>(ref T value)
		{ unsafe {
			return Unsafe.AsPointer(ref value) == null;
		} }
	}
}
