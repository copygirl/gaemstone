using System;
using System.Numerics;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.OpenGL;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gaemstone.Client.Graphics
{
	public class MeshManager
		: IProcessor
	{
		private const uint POSITION_ATTRIB_INDEX = 0;
		private const uint NORMAL_ATTRIB_INDEX   = 1;
		private const uint UV_ATTRIB_INDEX       = 2;


		public Mesh Load(Game game, string name)
		{ unsafe {
			ModelRoot root;
			using (var stream = game.GetResourceStream(name))
				root = ModelRoot.ReadGLB(stream, new SharpGLTF.Schema2.ReadSettings());
			var primitive = root.LogicalMeshes[0].Primitives[0];

			var indices  = primitive.IndexAccessor;
			var vertices = primitive.VertexAccessors["POSITION"];
			var normals  = primitive.VertexAccessors["NORMAL"];

			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(indices.SourceBufferView.Content,
				                      BufferTargetARB.ElementArrayBuffer);

				Buffer.CreateFromData(vertices.SourceBufferView.Content);
				GFX.GL.EnableVertexAttribArray(POSITION_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(POSITION_ATTRIB_INDEX, 3,
					(VertexAttribPointerType)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, (void*)vertices.ByteOffset);

				Buffer.CreateFromData(normals.SourceBufferView.Content);
				GFX.GL.EnableVertexAttribArray(NORMAL_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(NORMAL_ATTRIB_INDEX, 3,
					(VertexAttribPointerType)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, (void*)vertices.ByteOffset);
			}

			var numVertices  = vertices.Count;
			var numTriangles = primitive.IndexAccessor.Count / 3;
			return new Mesh(vertexArray, numTriangles);
		} }

		public Mesh Create(
			ReadOnlySpan<ushort> indices, ReadOnlySpan<Vector3> vertices,
			ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector2> uvs)
		{ unsafe {
			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(indices, BufferTargetARB.ElementArrayBuffer);
				Buffer.CreateFromData(vertices);
				GFX.GL.EnableVertexAttribArray(POSITION_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(POSITION_ATTRIB_INDEX, 3,
					VertexAttribPointerType.Float, false, 0, (void*)0);
				if (!normals.IsEmpty) {
					Buffer.CreateFromData(normals);
					GFX.GL.EnableVertexAttribArray(NORMAL_ATTRIB_INDEX);
					GFX.GL.VertexAttribPointer(NORMAL_ATTRIB_INDEX, 3,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
				if (!uvs.IsEmpty) {
					Buffer.CreateFromData(uvs);
					GFX.GL.EnableVertexAttribArray(UV_ATTRIB_INDEX);
					GFX.GL.VertexAttribPointer(UV_ATTRIB_INDEX, 2,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
			}
			return new Mesh(vertexArray, indices.Length / 3);
		} }

		public Mesh Create(ReadOnlySpan<Vector3> vertices,
			ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector2> uvs)
		{ unsafe {
			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(vertices);
				GFX.GL.EnableVertexAttribArray(POSITION_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(POSITION_ATTRIB_INDEX, 3,
					VertexAttribPointerType.Float, false, 0, (void*)0);
				if (!normals.IsEmpty) {
					Buffer.CreateFromData(normals);
					GFX.GL.EnableVertexAttribArray(NORMAL_ATTRIB_INDEX);
					GFX.GL.VertexAttribPointer(NORMAL_ATTRIB_INDEX, 3,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
				if (!uvs.IsEmpty) {
					Buffer.CreateFromData(uvs);
					GFX.GL.EnableVertexAttribArray(UV_ATTRIB_INDEX);
					GFX.GL.VertexAttribPointer(UV_ATTRIB_INDEX, 2,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
			}
			return new Mesh(vertexArray, vertices.Length / 3, false);
		} }


		// IProcessor implementation
		// (currently not used for anything)

		public void OnLoad(Universe universe) {  }

		public void OnUnload() {  }

		public void OnUpdate(double delta) {  }
	}

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
