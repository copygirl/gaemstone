using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;

namespace gaemstone.Common.Bloxel.WorldGen
{
	public class WorldGeneration : IProcessor
	{
		private Universe _universe = null!;

		public void OnLoad(Universe universe)
		{
			_universe = universe;
		}

		public void OnUnload() {  }

		public void OnUpdate(double delta)
		{
			throw new System.NotImplementedException();
		}
	}
}
