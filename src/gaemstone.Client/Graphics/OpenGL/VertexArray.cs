using System;

namespace gaemstone.Client.Graphics
{
	public readonly struct VertexArray
	{
		public static VertexArray Gen()
			=> new VertexArray(GFX.GL.GenVertexArray());


		public uint Handle { get; }

		internal VertexArray(uint handle) => Handle = handle;

		public UnbindOnDispose Bind()
		{
			GFX.GL.BindVertexArray(Handle);
			return new UnbindOnDispose();
		}

		public static void Unbind()
			=> GFX.GL.BindVertexArray(0);


		public readonly struct UnbindOnDispose
			: IDisposable
		{
			public void Dispose()
				=> VertexArray.Unbind();
		}
	}
}
