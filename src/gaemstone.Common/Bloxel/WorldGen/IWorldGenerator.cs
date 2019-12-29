using System.Collections.Generic;
using gaemstone.Common.ECS;

namespace gaemstone.Common.Bloxel.WorldGen
{
	public interface IWorldGenerator
	{
		string Identifier { get; }

		IEnumerable<string> Dependencies { get; }

		IEnumerable<(Neighbor, string)> NeighborDependencies { get; }


		void Begin(Universe universe, Entity chunk);

		void Populate();

		void Apply();
	}
}
