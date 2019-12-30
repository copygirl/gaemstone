using gaemstone.Common.ECS;

namespace gaemstone.Bloxel
{
	public readonly struct Block
	{
		public Entity Prototype { get; }

		public Block(Entity prototype) => Prototype = prototype;
	}
}
