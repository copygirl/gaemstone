using System;

namespace gaemstone.Client.Graphics
{
	public readonly struct VertexArray
	{
		public static VertexArray Gen()
			=> new(Gfx.Gl.GenVertexArray());


		public uint Handle { get; }

		internal VertexArray(uint handle) => Handle = handle;

		public UnbindOnDispose Bind()
		{
			Gfx.Gl.BindVertexArray(Handle);
			return new();
		}

		public static void Unbind()
			=> Gfx.Gl.BindVertexArray(0);


		public readonly struct UnbindOnDispose : IDisposable
			{ public void Dispose() => VertexArray.Unbind(); }
	}
}
