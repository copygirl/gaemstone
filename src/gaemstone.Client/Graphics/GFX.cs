using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public static class GFX
	{
		public static GL GL { get; private set; } = null!;
		public static bool IsInitialized => (GL != null);

		public static event Action<DebugSource, DebugType, int, DebugSeverity, string>? OnDebugOutput;

		public static void Initialize(IGLContextSource glContextSource)
		{
			GL = GL.GetApi(glContextSource);

			GL.Enable(GLEnum.DebugOutput);
			unsafe {
				GL.DebugMessageCallback(
					(source, type, id, severity, length, message, userParam) =>
						OnDebugOutput?.Invoke(
							(DebugSource)source, (DebugType)type, id,
							(DebugSeverity)severity, Marshal.PtrToStringAnsi(message)!),
					null);
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
			GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
		}

		public static void Viewport(Size size)
			=> GL.Viewport(size);
		public static void Viewport(Rectangle rectangle)
			=> GL.Viewport(rectangle);

		private static int MAX_LABEL_LENGTH;
		private static string? LABEL_BUFFER;
		public static string GetObjectLabel(ObjectLabelIdentifier identifier, uint handle)
		{
			if (MAX_LABEL_LENGTH == 0) {
				// One-time initialization.
				MAX_LABEL_LENGTH = GL.GetInteger(GLEnum.MaxLabelLength);
				LABEL_BUFFER     = new string(' ', MAX_LABEL_LENGTH);
			}
			GL.GetObjectLabel((GLEnum)identifier, handle, (uint)MAX_LABEL_LENGTH, out var length, out LABEL_BUFFER);
			return LABEL_BUFFER.Substring(0, (int)length);
		}
		public static void SetObjectLabel(ObjectLabelIdentifier identifier, uint handle, string label)
			=> GL.ObjectLabel((GLEnum)identifier, handle, (uint)label.Length, label);
	}

	public enum ObjectLabelIdentifier
	{
		Buffer            = GLEnum.Buffer,
		Shader            = GLEnum.Shader,
		Program           = GLEnum.Program,
		// FIXME: GLEnum.VertexArray doesn't exist.
		// VertexArray       = GLEnum.VertexArray,
		Query             = GLEnum.Query,
		ProgramPipeline   = GLEnum.ProgramPipeline,
		TransformFeedback = GLEnum.TransformFeedback,
		Sampler           = GLEnum.Sampler,
		Texture           = GLEnum.Texture,
		Renderbuffer      = GLEnum.Renderbuffer,
		Framebuffer       = GLEnum.Framebuffer
	}

	public enum DebugSource
	{
		Api            = GLEnum.DebugSourceApi,
		WindowSystem   = GLEnum.DebugSourceWindowSystem,
		ShaderCompiler = GLEnum.DebugSourceShaderCompiler,
		ThirdParty     = GLEnum.DebugSourceThirdParty,
		Application    = GLEnum.DebugSourceApplication,
		Other          = GLEnum.DebugSourceOther,
	}

	public enum DebugType
	{
		Error              = GLEnum.DebugTypeError,
		DeprecatedBehavior = GLEnum.DebugTypeDeprecatedBehavior,
		UndefinedBehavior  = GLEnum.DebugTypeUndefinedBehavior,
		Portability        = GLEnum.DebugTypePortability,
		Performance        = GLEnum.DebugTypePerformance,
		Marker             = GLEnum.DebugTypeMarker,
		PushGroup          = GLEnum.DebugTypePushGroup,
		PopGroup           = GLEnum.DebugTypePopGroup,
		Other              = GLEnum.DebugTypeOther,
	}

	public enum DebugSeverity
	{
		High         = GLEnum.DebugSeverityHigh,
		Medium       = GLEnum.DebugSeverityMedium,
		Low          = GLEnum.DebugSeverityLow,
		Notification = GLEnum.DebugSeverityNotification,
	}
}
