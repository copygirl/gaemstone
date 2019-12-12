
namespace gaemstone.Client.Graphics
{
	public readonly struct VertexArray
	{
		public static VertexArray Gen()
			=> new VertexArray(GFX.GL.GenVertexArray());

		public static VertexArray GenAndBind()
		{
			var vertexArray = Gen();
			vertexArray.Bind();
			return vertexArray;
		}


		public uint Handle { get; }

		internal VertexArray(uint handle) => Handle = handle;

		public void Bind()
			=> GFX.GL.BindVertexArray(Handle);
	}
}
