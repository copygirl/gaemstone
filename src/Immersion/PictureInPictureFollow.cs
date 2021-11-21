using System.Drawing;
using System.Linq;
using System.Numerics;
using gaemstone.Client;
using gaemstone.Client.Graphics;
using gaemstone.Common;
using gaemstone.Common.Processors;

namespace Immersion
{
	public class PictureInPictureFollow : IProcessor
	{
		public Universe Universe { get; } = null!;

		EcsId _mainCamera;
		EcsId _pipCamera;

		public void OnLoad()
		{
			// Pick the Mesh from the first entity that has one.
			// (This will be either a heart or sword.)
			// TODO: Right now MeshManager.Load will always load in a new Mesh.
			var (_, mesh) = Universe.GetAll<Mesh>().First();

			(_mainCamera, _) = Universe.GetAll<Camera>().First();
			Universe.Set(_mainCamera, mesh);

			_pipCamera = Universe.Entities.New();
			Universe.Set(_pipCamera, Camera.Create3D(90.0F,
				clearColor: Color.Black, viewport: new(8, 8, 320, 180)));
		}

		public void OnUnload()
			=> Universe.Entities.Destroy(_pipCamera);

		public void OnUpdate(double delta)
		{
			var cameraPos = Universe.Get<Transform>(_mainCamera).Translation;
			var lookAt = Matrix4x4.CreateLookAt(new(8, 28, 8), cameraPos, Vector3.UnitY);
			Matrix4x4.Invert(lookAt, out lookAt);
			Universe.Set(_pipCamera, (Transform)lookAt);
		}
	}
}
