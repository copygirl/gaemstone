using System.Drawing;
using System.Numerics;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.Input;
using Silk.NET.Input.Common;

namespace gaemstone.Client.Processors
{
	public class CameraController
		: IProcessor
	{
		private Game _game = null!;
		private IMouse _mouse = null!;
		private IKeyboard _keyboard = null!;

		private PointF? _mouseGrabbedAt = null;
		private PointF _mouseMoved;

		private bool _moveLeft    = false; // -X
		private bool _moveRight   = false; // +X
		private bool _moveForward = false; // -Z
		private bool _moveBack    = false; // +Z
		private bool _fastMovement = false;

		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;

			var input = _game.Window.GetInput();
			_mouse    = input.Mice[0];
			_keyboard = input.Keyboards[0];

			_mouse.MouseDown += OnMouseDown;
			_mouse.MouseUp   += OnMouseUp;
			_mouse.MouseMove += OnMouseMove;

			_keyboard.KeyDown += OnKeyDown;
			_keyboard.KeyUp   += OnKeyUp;
		}

		public void OnUnload()
		{
			_mouse.MouseDown -= OnMouseDown;
			_mouse.MouseUp   -= OnMouseUp;
			_mouse.MouseMove -= OnMouseMove;

			_keyboard.KeyDown -= OnKeyDown;
			_keyboard.KeyUp   -= OnKeyUp;
		}


		private void OnMouseDown(IMouse mouse, MouseButton button)
		{
			if (button != MouseButton.Right) return;
			_mouseGrabbedAt = mouse.Position;
		}

		private void OnMouseUp(IMouse mouse, MouseButton button)
		{
			if (button != MouseButton.Right) return;
			_mouseGrabbedAt = null;
		}

		private void OnMouseMove(IMouse mouse, PointF position)
		{
			if (_mouseGrabbedAt == null) return;
			var x = _mouseMoved.X + position.X - _mouseGrabbedAt.Value.X;
			var y = _mouseMoved.Y + position.Y - _mouseGrabbedAt.Value.Y;
			_mouseMoved = new PointF(x, y);
			mouse.Position = _mouseGrabbedAt.Value;
		}


		private void OnKeyDown(IKeyboard keyboard, Key key, int code)
		{
			switch (key) {
				case Key.A: _moveLeft    = true; break;
				case Key.D: _moveRight   = true; break;
				case Key.W: _moveForward = true; break;
				case Key.S: _moveBack    = true; break;
				case Key.ShiftLeft: _fastMovement = true; break;
			}
		}

		private void OnKeyUp(IKeyboard keyboard, Key key, int code)
		{
			switch (key) {
				case Key.A: _moveLeft    = false; break;
				case Key.D: _moveRight   = false; break;
				case Key.W: _moveForward = false; break;
				case Key.S: _moveBack    = false; break;
				case Key.ShiftLeft: _fastMovement = false; break;
			}
		}


		public void OnUpdate(double delta)
		{
			var cameraID      = _game.MainCamera.ID;
			ref var transform = ref _game.Transforms.GetRef(cameraID);

			var xMovement = -_mouseMoved.X * (float)delta / 100;
			var yMovement = -_mouseMoved.Y * (float)delta / 100;
			_mouseMoved = PointF.Empty;

			var speed = (float)delta * (_fastMovement ? 12 : 4);
			var forwardMovement = ((_moveForward ? -1 : 0) + (_moveBack  ? 1 : 0)) * speed;
			var sideMovement    = ((_moveLeft    ? -1 : 0) + (_moveRight ? 1 : 0)) * speed;

			var yawRotation   = Matrix4x4.CreateRotationY(xMovement, transform.Value.Translation);
			var pitchRotation = Matrix4x4.CreateRotationX(yMovement);
			var translation   = Matrix4x4.CreateTranslation(sideMovement, 0, forwardMovement);
			transform = translation * pitchRotation * transform * yawRotation;
		}
	}
}
