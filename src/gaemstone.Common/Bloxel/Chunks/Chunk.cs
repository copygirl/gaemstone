
namespace gaemstone.Common.Bloxel.Chunks
{
	public readonly struct Chunk
	{
		public ChunkPos Position { get; }

		public Chunk(ChunkPos pos) => Position = pos;
	}
}
