using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public static class GFX
	{
		static GL? _gl;
		public static GL GL => _gl
			?? throw new InvalidOperationException("OpenGL has not been initialized");
		public static bool IsInitialized => (_gl != null);

		public static event Action<DebugSource, DebugType, int, DebugSeverity, string>? OnDebugOutput;

		public static void Initialize(IGLContextSource glContextSource)
		{
			_gl = GL.GetApi(glContextSource);

			// FIXME: Debugger currently doesn't like us specifying a callback.
			if (!Debugger.IsAttached) {
				GL.Enable(GLEnum.DebugOutput);
				GL.DebugMessageCallback(
					(source, type, id, severity, length, message, userParam) =>
						OnDebugOutput?.Invoke(
							(DebugSource)source, (DebugType)type, id,
							(DebugSeverity)severity, Marshal.PtrToStringAnsi(message, length)),
					false);
			}

			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		public static void Clear(Color color)
		{
			GL.ClearColor(color);
			GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
		}
		public static void Clear(Vector4 color)
		{
			GL.ClearColor(color.X, color.Y, color.Z, color.W);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public static void Clear(Color color, Rectangle area)
		{
			GL.Enable(EnableCap.ScissorTest);
			GL.Scissor(area.X, area.Y, (uint)area.Width, (uint)area.Height);
			GL.ClearColor(color);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Disable(EnableCap.ScissorTest);
		}
		public static void Clear(Vector4 color, Rectangle area)
		{
			GL.Enable(EnableCap.ScissorTest);
			GL.Scissor(area.X, area.Y, (uint)area.Width, (uint)area.Height);
			GL.ClearColor(color.X, color.Y, color.Z, color.W);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Disable(EnableCap.ScissorTest);
		}

		public static void Viewport(Size size)
			=> GL.Viewport(size);
		public static void Viewport(Rectangle rectangle)
			=> GL.Viewport(rectangle);

		static int MAX_LABEL_LENGTH;
		static string? LABEL_BUFFER;
		public static string GetObjectLabel(ObjectIdentifier identifier, uint handle)
		{
			if (MAX_LABEL_LENGTH == 0) {
				// One-time initialization.
				MAX_LABEL_LENGTH = GL.GetInteger(GLEnum.MaxLabelLength);
				LABEL_BUFFER     = new(' ', MAX_LABEL_LENGTH);
			}
			GL.GetObjectLabel(identifier, handle, (uint)MAX_LABEL_LENGTH, out var length, out LABEL_BUFFER);
			return LABEL_BUFFER[..(int)length];
		}
		public static void SetObjectLabel(ObjectIdentifier identifier, uint handle, string label)
			=> GL.ObjectLabel(identifier, handle, (uint)label.Length, label);
	}
}
