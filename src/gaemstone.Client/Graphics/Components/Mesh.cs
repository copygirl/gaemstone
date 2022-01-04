using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Mesh
	{
		public VertexArray VAO { get; }
		public int Triangles { get; }
		public bool IsIndexed { get; }

		internal Mesh(VertexArray vao, int triangles, bool isIndexed = true)
			=> (VAO, Triangles, IsIndexed) = (vao, triangles, isIndexed);

		public void Draw()
			=> Draw(0, Triangles * 3);
		public void Draw(int start, int count)
		{ unsafe {
			VAO.Bind();
			if (IsIndexed) GFX.GL.DrawElements(
				PrimitiveType.Triangles, (uint)count,
				DrawElementsType.UnsignedShort, (void*)start);
			else GFX.GL.DrawArrays(
				PrimitiveType.Triangles,
				start, (uint)count);
		} }
	}
}
