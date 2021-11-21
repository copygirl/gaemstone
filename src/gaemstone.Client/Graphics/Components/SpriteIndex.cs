using gaemstone.Common.Stores;

namespace gaemstone.Client.Graphics
{
	[Store]
	public readonly struct SpriteIndex
	{
		public readonly int Value;
		SpriteIndex(int value) => Value = value;
		public static implicit operator SpriteIndex(in int value) => new(value);
		public static implicit operator int(in SpriteIndex transform) => transform.Value;
	}
}