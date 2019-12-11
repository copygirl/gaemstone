using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using gaemstone.Client.Components;
using Silk.NET.OpenGL;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gaemstone.Client.Graphics
{
	public class MeshManager
	{
		private uint _counter = 0;
		private Dictionary<Mesh, MeshInfo> _meshes
			= new Dictionary<Mesh, MeshInfo>();

		public Game Game { get; }
		public VertexAttributes? ProgramAttributes { get; internal set; }

		public MeshManager(Game game)
			=> Game = game;


		public MeshInfo Load(string name)
		{
			if (ProgramAttributes == null) throw new InvalidOperationException(
				$"{nameof(ProgramAttributes)} has not been set");

			ModelRoot root;
			using (var stream = Game.GetResourceStream(name))
				root = ModelRoot.ReadGLB(stream, new SharpGLTF.Schema2.ReadSettings());
			var primitive = root.LogicalMeshes[0].Primitives[0];

			var vertices  = primitive.VertexAccessors["POSITION"].Count;
			var triangles = primitive.IndexAccessor.Count / 3;
			var indexBufferData  = primitive.IndexAccessor.SourceBufferView.Content;
			var vertexBufferData = primitive.VertexAccessors["POSITION"].SourceBufferView.Content;
			var colorBufferData  = primitive.VertexAccessors["NORMAL"].SourceBufferView.Content;

			var vertexArray  = VertexArray.GenAndBind();
			var indexBuffer  = Buffer.CreateFromData(indexBufferData, BufferTargetARB.ElementArrayBuffer);
			var vertexBuffer = Buffer.CreateFromData(vertexBufferData);
			ProgramAttributes["position"].Pointer(3, VertexAttribPointerType.Float);
			var colorBuffer  = Buffer.CreateFromData(colorBufferData);
			ProgramAttributes["normal"].Pointer(3, VertexAttribPointerType.Float);

			var mesh = new Mesh { Index = _counter++ };
			var meshInfo = new MeshInfo(mesh, vertexArray, vertices, triangles);
			_meshes.Add(mesh, meshInfo);

			return meshInfo;
		}

		public MeshInfo Create(Span<ushort> indices, Span<Vector3> vertices, Span<Vector3> normals)
		{
			if (ProgramAttributes == null) throw new InvalidOperationException(
				$"{nameof(ProgramAttributes)} has not been set");

			var vertexArray  = VertexArray.GenAndBind();
			var indexBuffer  = Buffer.CreateFromData(indices, BufferTargetARB.ElementArrayBuffer);
			var vertexBuffer = Buffer.CreateFromData(vertices);
			ProgramAttributes["position"].Pointer(3, VertexAttribPointerType.Float);
			var normalBuffer = Buffer.CreateFromData(normals);
			ProgramAttributes["normal"].Pointer(3, VertexAttribPointerType.Float);

			var mesh = new Mesh { Index = _counter++ };
			var meshInfo = new MeshInfo(mesh, vertexArray, vertices.Length, indices.Length / 3);
			_meshes.Add(mesh, meshInfo);

			return meshInfo;
		}

		public MeshInfo Find(Mesh mesh)
			=> _meshes[mesh];
	}

	public class MeshInfo
	{
		public Mesh ID { get; }
		public VertexArray VAO { get; }
		public int Vertices { get; }
		public int Triangles { get; }

		internal MeshInfo(Mesh id, VertexArray vao, int vertices, int triangles)
		{
			ID  = id;
			VAO = vao;
			Vertices  = vertices;
			Triangles = triangles;
		}

		public void Draw()
		{
			VAO.Bind();
			GFX.GL.DrawElements(
				(GLEnum)PrimitiveType.Triangles, (uint)Triangles * 3,
				(GLEnum)DrawElementsType.UnsignedShort, 0);
		}
	}
}
