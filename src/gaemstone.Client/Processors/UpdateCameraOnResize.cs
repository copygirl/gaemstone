using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;

namespace gaemstone.Client.Graphics
{
	public class UpdateCameraOnResize
		: IProcessor
	{
		private Game _game = null!;
		private bool _update;

		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;
			_game.Window.Resize += OnWindowResize;
			_update = true;
		}

		public void OnUnload()
			=> _game.Window.Resize -= OnWindowResize;

		public void OnWindowResize(Size size)
			=> _update = true;

		public void OnUpdate(double delta)
		{
			var size = _game.Window.Size;
			foreach (var (entity, fsCamera) in _game.GetAll<FullscreenCamera>()) {
				// Only update cameras when _update is true, or
				// entity doesn't yet have a Camera component.
				if (!_update && _game.Has<Camera>(entity)) continue;

				_game.Set(entity, new Camera {
					Viewport = new Rectangle(Point.Empty, size),
					Matrix   = fsCamera.IsOrthographic
						? Matrix4x4.CreateOrthographic(size.Width, size.Height,
							fsCamera.NearPlane, fsCamera.FarPlane)
						: Matrix4x4.CreatePerspectiveFieldOfView(
							fsCamera.FieldOfView * MathF.PI / 180, // Degrees => Radians
							(float)size.Width / size.Height,       // Aspect Ratio
							fsCamera.NearPlane, fsCamera.FarPlane)
				});
			}
		}
	}
}
