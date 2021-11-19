using gaemstone.Common.Stores;

namespace gaemstone.Client.Graphics
{
	[Store(typeof(PackedArrayStore<>))]
	public readonly struct SpriteIndex
	{
		public readonly int Value;
		SpriteIndex(int value) => Value = value;
		public static implicit operator SpriteIndex(in int value) => new(value);
		public static implicit operator int(in SpriteIndex transform) => transform.Value;
	}
}
