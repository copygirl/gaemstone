using System.Drawing;
using System.Numerics;
using gaemstone;
using gaemstone.Client;

namespace Immersion
{
	public class PictureInPictureFollow
		: IProcessor
	{
		public Game Game { get; } = null!;

		public Universe.Entity PipCamera { get; private set; }

		public void OnLoad()
		{
			// Pick the Mesh from the first entity that has one.
			// (This will be either a heart or sword.)
			// TODO: Right now MeshManager.Load will always load in a new Mesh.
			// var (_, mesh) = Game.GetAll<Mesh>().First();
			// Game.Set(Game.MainCamera, mesh);

			PipCamera = Game.Entities.New()
				.Set(Camera.Create3d(
					fieldOfView: 90.0F,
					clearColor: Color.Black,
					viewport: new(8, 8, 320, 180)
				));
		}

		public void OnUnload()
		{
			PipCamera.Delete();
			PipCamera = default;
		}

		public void OnUpdate(double delta)
		{
			var cameraPos = Game.MainCamera.Get<Transform>().Translation;
			var lookAt = Matrix4x4.CreateLookAt(new(8, 28, 8), cameraPos, Vector3.UnitY);
			Matrix4x4.Invert(lookAt, out lookAt);
			PipCamera.Set((Transform)lookAt);
		}
	}
}
