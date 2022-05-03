using gaemstone.ECS;

namespace gaemstone.Bloxel
{
	public readonly struct Prototype
	{
		public readonly EcsId.Entity Value { get; }
		public Prototype(EcsId.Entity value) => Value = value;
		public static implicit operator Prototype(in EcsId.Entity value) => new(value);
	}
}
