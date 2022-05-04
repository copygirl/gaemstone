using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Mesh
	{
		public VertexArray Vao { get; }
		public int Triangles { get; }
		public bool IsIndexed { get; }

		internal Mesh(VertexArray vao, int triangles, bool isIndexed = true)
			=> (Vao, Triangles, IsIndexed) = (vao, triangles, isIndexed);

		public void Draw()
			=> Draw(0, Triangles * 3);
		public void Draw(int start, int count)
		{ unsafe {
			Vao.Bind();
			if (IsIndexed) Gfx.Gl.DrawElements(
				PrimitiveType.Triangles, (uint)count,
				DrawElementsType.UnsignedShort, (void*)start);
			else Gfx.Gl.DrawArrays(
				PrimitiveType.Triangles,
				start, (uint)count);
		} }
	}
}
