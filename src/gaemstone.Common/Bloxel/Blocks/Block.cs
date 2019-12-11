using gaemstone.Common.ECS;

namespace gaemstone.Client.Bloxel.Blocks
{
	public readonly struct Block
	{
		public Entity Prototype { get; }

		public Block(Entity prototype) => Prototype = prototype;
	}
}
