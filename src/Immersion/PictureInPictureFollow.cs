using System.Linq;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;

namespace Immersion
{
	public class PictureInPictureFollow : IProcessor
	{
		Universe _universe = null!;

		public void OnLoad(Universe universe)
			=> _universe = universe;

		public void OnUnload() {  }

		public void OnUpdate(double delta)
		{
			var cameras = _universe.GetAll<Camera>();
			var (mainCamera, _) = cameras.First();
			var cameraPos = _universe.Get<Transform>(mainCamera).Translation;

			var (smallCamera, _) = cameras.Skip(1).First();
			Matrix4x4.Invert(Matrix4x4.CreateLookAt(
				new(8, 28, 8), cameraPos, Vector3.UnitY), out var lookAt);
			_universe.Set(smallCamera, (Transform)lookAt);
		}
	}
}
