using System;
using System.Numerics;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.OpenGL;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gaemstone.Client.Graphics
{
	public class MeshLoader
		: IProcessor
	{
		private const uint POSITION_ATTRIB_INDEX = 0;
		private const uint NORMAL_ATTRIB_INDEX   = 1;
		private const uint UV_ATTRIB_INDEX       = 2;


		public IndexedMesh Load(Game game, string name)
		{
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
					(GLEnum)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, vertices.ByteOffset);

				Buffer.CreateFromData(normals.SourceBufferView.Content);
				GFX.GL.EnableVertexAttribArray(NORMAL_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(NORMAL_ATTRIB_INDEX, 3,
					(GLEnum)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, vertices.ByteOffset);
			}

			var numVertices  = vertices.Count;
			var numTriangles = primitive.IndexAccessor.Count / 3;
			return new IndexedMesh(vertexArray, numTriangles);
		}

		public IndexedMesh Create(Span<ushort> indices, Span<Vector3> vertices,
		                          Span<Vector3> normals, Span<Vector2> uvs)
		{
			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(indices, BufferTargetARB.ElementArrayBuffer);

				Buffer.CreateFromData(vertices);
				GFX.GL.EnableVertexAttribArray(POSITION_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(POSITION_ATTRIB_INDEX, 3,
					(GLEnum)VertexAttribType.Float, false, 0, 0);

				Buffer.CreateFromData(normals);
				GFX.GL.EnableVertexAttribArray(NORMAL_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(NORMAL_ATTRIB_INDEX, 3,
					(GLEnum)VertexAttribType.Float, false, 0, 0);

				Buffer.CreateFromData(uvs);
				GFX.GL.EnableVertexAttribArray(UV_ATTRIB_INDEX);
				GFX.GL.VertexAttribPointer(UV_ATTRIB_INDEX, 2,
					(GLEnum)VertexAttribType.Float, false, 0, 0);
			}

			return new IndexedMesh(vertexArray, indices.Length / 3);
		}


		// IProcessor implementation
		// (currently not used for anything)

		public void OnLoad(Universe universe) {  }

		public void OnUnload() {  }

		public void OnUpdate(double delta) {  }
	}

	public readonly struct IndexedMesh
	{
		public VertexArray VAO { get; }
		public int Triangles { get; }

		internal IndexedMesh(VertexArray vao, int triangles)
			=> (VAO, Triangles) = (vao, triangles);

		public void Draw()
		{
			VAO.Bind();
			GFX.GL.DrawElements(
				(GLEnum)PrimitiveType.Triangles, (uint)Triangles * 3,
				(GLEnum)DrawElementsType.UnsignedShort, 0);
		}
	}
}
