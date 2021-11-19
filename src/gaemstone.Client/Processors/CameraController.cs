using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Common;
using gaemstone.Common.Processors;
using Silk.NET.Input;

namespace gaemstone.Client.Processors
{
	public class CameraController
		: IProcessor
	{
		Game _game = null!;
		IMouse _mouse = null!;
		IKeyboard _keyboard = null!;

		float _mouseSpeed = 4.0F;
		Vector2? _mouseGrabbedAt = null;
		PointF _mouseMoved;

		bool _moveLeft    = false; // -X
		bool _moveRight   = false; // +X
		bool _moveForward = false; // -Z
		bool _moveBack    = false; // +Z
		bool _fastMovement = false;

		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;

			_mouse    = _game.Input.Mice[0];
			_keyboard = _game.Input.Keyboards[0];

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


		void OnMouseDown(IMouse mouse, MouseButton button)
		{
			if (button != MouseButton.Right) return;
			_mouseGrabbedAt = mouse.Position;
		}

		void OnMouseUp(IMouse mouse, MouseButton button)
		{
			if (button != MouseButton.Right) return;
			_mouseGrabbedAt = null;
		}

		void OnMouseMove(IMouse mouse, Vector2 position)
		{
			if (_mouseGrabbedAt == null) return;
			var x = _mouseMoved.X + position.X - _mouseGrabbedAt.Value.X;
			var y = _mouseMoved.Y + position.Y - _mouseGrabbedAt.Value.Y;
			_mouseMoved = new(x, y);
			mouse.Position = _mouseGrabbedAt.Value;
		}

		void OnKeyDown(IKeyboard keyboard, Key key, int code)
		{
			switch (key) {
				case Key.A: _moveLeft    = true; break;
				case Key.D: _moveRight   = true; break;
				case Key.W: _moveForward = true; break;
				case Key.S: _moveBack    = true; break;
				case Key.ShiftLeft: _fastMovement = true; break;
			}
		}

		void OnKeyUp(IKeyboard keyboard, Key key, int code)
		{
			switch (key) {
				case Key.A: _moveLeft    = false; break;
				case Key.D: _moveRight   = false; break;
				case Key.W: _moveForward = false; break;
				case Key.S: _moveBack    = false; break;
				case Key.ShiftLeft: _fastMovement = false; break;
			}
		}

		struct CameraQuery
		{
			public Camera Camera { get; }
			public Transform Transform { get; set; }
		}

		public void OnUpdate(double delta)
		{
			_game.Queries.Run((Span<CameraQuery> query) => {
				if (query.Length == 0) return; // No cameras.
				ref var e = ref query[0]; // Only first camera found is affected.

				var xMovement = _mouseMoved.X * (float)delta * _mouseSpeed;
				var yMovement = _mouseMoved.Y * (float)delta * _mouseSpeed;
				_mouseMoved = PointF.Empty;

				if (e.Camera.IsOrthographic) {
					e.Transform *= Matrix4x4.CreateTranslation(-xMovement, -yMovement, 0);
				} else {
					var speed = (float)delta * (_fastMovement ? 12 : 4);
					var forwardMovement = ((_moveForward ? -1 : 0) + (_moveBack  ? 1 : 0)) * speed;
					var sideMovement    = ((_moveLeft    ? -1 : 0) + (_moveRight ? 1 : 0)) * speed;

					var yawRotation   = Matrix4x4.CreateRotationY(-xMovement / 100, e.Transform.Translation);
					var pitchRotation = Matrix4x4.CreateRotationX(-yMovement / 100);
					var translation   = Matrix4x4.CreateTranslation(sideMovement, 0, forwardMovement);

					e.Transform = translation * pitchRotation * e.Transform * yawRotation;
				}
			});
		}
	}
}
