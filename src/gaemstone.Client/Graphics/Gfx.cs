using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public static class Gfx
	{
		static GL? _gl;
		public static GL Gl => _gl
			?? throw new InvalidOperationException("OpenGL has not been initialized");
		public static bool IsInitialized => (_gl != null);

		public static event Action<DebugSource, DebugType, int, DebugSeverity, string>? OnDebugOutput;

		public static void Initialize(IGLContextSource glContextSource)
		{
			_gl = GL.GetApi(glContextSource);

			// FIXME: Debugger currently doesn't like us specifying a callback.
			if (!Debugger.IsAttached) {
				Gl.Enable(GLEnum.DebugOutput);
				Gl.DebugMessageCallback(
					(source, type, id, severity, length, message, userParam) =>
						OnDebugOutput?.Invoke(
							(DebugSource)source, (DebugType)type, id,
							(DebugSeverity)severity, Marshal.PtrToStringAnsi(message, length)),
					false);
			}

			Gl.Enable(EnableCap.CullFace);
			Gl.CullFace(CullFaceMode.Back);

			Gl.Enable(EnableCap.DepthTest);
			Gl.DepthFunc(DepthFunction.Less);

			Gl.Enable(EnableCap.Blend);
			Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		public static void Clear(Color color)
		{
			Gl.ClearColor(color);
			Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
		}
		public static void Clear(Vector4 color)
		{
			Gl.ClearColor(color.X, color.Y, color.Z, color.W);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public static void Clear(Color color, Rectangle area)
		{
			Gl.Enable(EnableCap.ScissorTest);
			Gl.Scissor(area.X, area.Y, (uint)area.Width, (uint)area.Height);
			Gl.ClearColor(color);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Gl.Disable(EnableCap.ScissorTest);
		}
		public static void Clear(Vector4 color, Rectangle area)
		{
			Gl.Enable(EnableCap.ScissorTest);
			Gl.Scissor(area.X, area.Y, (uint)area.Width, (uint)area.Height);
			Gl.ClearColor(color.X, color.Y, color.Z, color.W);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Gl.Disable(EnableCap.ScissorTest);
		}

		public static void Viewport(Size size)
			=> Gl.Viewport(size);
		public static void Viewport(Rectangle rectangle)
			=> Gl.Viewport(rectangle);

		static int MAX_LABEL_LENGTH;
		static string? LABEL_BUFFER;
		public static string GetObjectLabel(ObjectIdentifier identifier, uint handle)
		{
			if (MAX_LABEL_LENGTH == 0) {
				// One-time initialization.
				MAX_LABEL_LENGTH = Gl.GetInteger(GLEnum.MaxLabelLength);
				LABEL_BUFFER     = new(' ', MAX_LABEL_LENGTH);
			}
			Gl.GetObjectLabel(identifier, handle, (uint)MAX_LABEL_LENGTH, out var length, out LABEL_BUFFER);
			return LABEL_BUFFER[..(int)length];
		}
		public static void SetObjectLabel(ObjectIdentifier identifier, uint handle, string label)
			=> Gl.ObjectLabel(identifier, handle, (uint)label.Length, label);
	}
}
