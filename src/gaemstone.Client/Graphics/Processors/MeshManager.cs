using System;
using System.Numerics;
using Silk.NET.OpenGL;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gaemstone.Client.Graphics
{
	public class MeshManager
		: IProcessor
	{
		const uint PositionAttribIndex = 0;
		const uint NormalAttribIndex   = 1;
		const uint UvAttribIndex       = 2;


		public Mesh Load(Game game, string name)
		{ unsafe {
			ModelRoot root;
			using (var stream = game.GetResourceStream(name))
				root = ModelRoot.ReadGLB(stream, new());
			var primitive = root.LogicalMeshes[0].Primitives[0];

			var indices  = primitive.IndexAccessor;
			var vertices = primitive.VertexAccessors["POSITION"];
			var normals  = primitive.VertexAccessors["NORMAL"];

			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(indices.SourceBufferView.Content,
				                      BufferTargetARB.ElementArrayBuffer);

				Buffer.CreateFromData(vertices.SourceBufferView.Content);
				Gfx.Gl.EnableVertexAttribArray(PositionAttribIndex);
				Gfx.Gl.VertexAttribPointer(PositionAttribIndex, 3,
					(VertexAttribPointerType)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, (void*)vertices.ByteOffset);

				Buffer.CreateFromData(normals.SourceBufferView.Content);
				Gfx.Gl.EnableVertexAttribArray(NormalAttribIndex);
				Gfx.Gl.VertexAttribPointer(NormalAttribIndex, 3,
					(VertexAttribPointerType)vertices.Encoding, vertices.Normalized,
					(uint)vertices.SourceBufferView.ByteStride, (void*)vertices.ByteOffset);
			}

			var numTriangles = primitive.IndexAccessor.Count / 3;
			return new(vertexArray, numTriangles);
		} }

		public Mesh Create(
			ReadOnlySpan<ushort> indices, ReadOnlySpan<Vector3> vertices,
			ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector2> uvs)
		{ unsafe {
			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(indices, BufferTargetARB.ElementArrayBuffer);
				Buffer.CreateFromData(vertices);
				Gfx.Gl.EnableVertexAttribArray(PositionAttribIndex);
				Gfx.Gl.VertexAttribPointer(PositionAttribIndex, 3,
					VertexAttribPointerType.Float, false, 0, (void*)0);
				if (!normals.IsEmpty) {
					Buffer.CreateFromData(normals);
					Gfx.Gl.EnableVertexAttribArray(NormalAttribIndex);
					Gfx.Gl.VertexAttribPointer(NormalAttribIndex, 3,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
				if (!uvs.IsEmpty) {
					Buffer.CreateFromData(uvs);
					Gfx.Gl.EnableVertexAttribArray(UvAttribIndex);
					Gfx.Gl.VertexAttribPointer(UvAttribIndex, 2,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
			}
			return new(vertexArray, indices.Length / 3);
		} }

		public Mesh Create(ReadOnlySpan<Vector3> vertices,
			ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector2> uvs)
		{ unsafe {
			var vertexArray = VertexArray.Gen();
			using (vertexArray.Bind()) {
				Buffer.CreateFromData(vertices);
				Gfx.Gl.EnableVertexAttribArray(PositionAttribIndex);
				Gfx.Gl.VertexAttribPointer(PositionAttribIndex, 3,
					VertexAttribPointerType.Float, false, 0, (void*)0);
				if (!normals.IsEmpty) {
					Buffer.CreateFromData(normals);
					Gfx.Gl.EnableVertexAttribArray(NormalAttribIndex);
					Gfx.Gl.VertexAttribPointer(NormalAttribIndex, 3,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
				if (!uvs.IsEmpty) {
					Buffer.CreateFromData(uvs);
					Gfx.Gl.EnableVertexAttribArray(UvAttribIndex);
					Gfx.Gl.VertexAttribPointer(UvAttribIndex, 2,
						VertexAttribPointerType.Float, false, 0, (void*)0);
				}
			}
			return new(vertexArray, vertices.Length / 3, false);
		} }


		// IProcessor implementation
		// (currently not used for anything)

		public void OnLoad() {  }
		public void OnUnload() {  }
		public void OnUpdate(double delta) {  }
	}
}
