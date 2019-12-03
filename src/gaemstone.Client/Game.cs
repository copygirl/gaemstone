using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Client.Graphics;
using gaemstone.Common.ECS;
using gaemstone.Common.Utility;
using Silk.NET.OpenGL;
using Silk.NET.Windowing.Common;
using Buffer = gaemstone.Client.Graphics.Buffer;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gaemstone.Client
{
	public class Game
	{
		private static void Main(string[] args)
			=> new Game(args).Run();


		public IWindow Window { get; }
		public Random RND { get; } = new Random();

		public EntityManager Entities { get; }
		public ComponentManager Components { get; }
		public PackedArrayComponentStore<Transform> Transforms { get; }
		public PackedArrayComponentStore<Camera> Cameras { get; }
		// TODO: Camera is uncommon. Handle uncommon components differently.

		public Entity MainCamera { get; }


		private Game(string[] args)
		{
			Window = Silk.NET.Windowing.Window.Create(new WindowOptions {
				Title = "gæmstone",
				Size  = new Size(1280, 720),
				API   = GraphicsAPI.Default,
				UpdatesPerSecond = 30.0,
				FramesPerSecond  = 60.0,
			});
			Window.Load    += OnLoad;
			Window.Resize  += OnResize;
			Window.Update  += OnUpdate;
			Window.Render  += OnRender;
			Window.Closing += OnClosing;

			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Transforms = new PackedArrayComponentStore<Transform>();
			Cameras    = new PackedArrayComponentStore<Camera>();
			Components.AddStore(Transforms);
			Components.AddStore(Cameras);

			MainCamera = Entities.New();
			Transforms.Set(MainCamera.ID, Matrix4x4.CreateLookAt(
				cameraPosition : new Vector3(3, 2, 2),
				cameraTarget   : new Vector3(0, 0, 0),
				cameraUpVector : new Vector3(0, 1, 0)));

			for (var x = -8; x <= 8; x++)
			for (var z = -8; z <= 8; z++) {
				var entity   = Entities.New();
				var position = Matrix4x4.CreateTranslation(x * 0.5F, 0, z * 0.5F);
				var rotation = Matrix4x4.CreateRotationY(RND.NextFloat(MathF.PI * 2));
				Transforms.Set(entity.ID, rotation * position);
			}
		}

		public void Run()
		{
			Window.Run();
		}


		private Program _program;
		private UniformMatrix4x4 _mvpUniform;

		private VertexArray _vertexArray;
		private int _indicesCount;
		private Buffer _indexBuffer;
		private Buffer _vertexBuffer;
		private Buffer _colorBuffer;

		private void OnLoad()
		{
			GFX.Initialize();
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			Stream GetResourceStream(string name)
				=> typeof(Game).Assembly.GetManifestResourceStream("gaemstone.Client.Resources." + name)!;

			string GetResourceAsString(string name)
			{
				using (var stream = GetResourceStream(name))
				using (var reader = new StreamReader(stream))
					return reader.ReadToEnd();
			}

			var vertexShaderSource   = GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var uniforms = _program.GetActiveUniforms();
			var attribs  = _program.GetActiveAttributes();
			_mvpUniform  = uniforms["modelViewProjection"].Matrix4x4;


			ModelRoot root;
			using (var stream = GetResourceStream("sword.glb"))
				root = ModelRoot.ReadGLB(stream, new SharpGLTF.Schema2.ReadSettings());
			var primitive = root.LogicalMeshes[0].Primitives[0];

			_indicesCount = primitive.IndexAccessor.Count;
			var indicesBufferData  = primitive.IndexAccessor.SourceBufferView.Content;
			var verticesBufferData = primitive.VertexAccessors["POSITION"].SourceBufferView.Content;
			var colorsBufferData   = primitive.VertexAccessors["NORMAL"].AsVector3Array().ToArray();


			_vertexArray = VertexArray.Gen();
			_vertexArray.Bind();

			_indexBuffer = Buffer.CreateFromData(indicesBufferData, BufferTargetARB.ElementArrayBuffer);
			_vertexBuffer = Buffer.CreateFromData(verticesBufferData);
			attribs["position"].Pointer(3, VertexAttribPointerType.Float);
			_colorBuffer = Buffer.CreateFromData(colorsBufferData);
			attribs["color"].Pointer(3, VertexAttribPointerType.Float);

			OnResize(Window.Size);
		}

		private void OnClosing()
		{

		}

		private void OnResize(Size size)
		{
			const float DEGREES_TO_RADIANS = MathF.PI / 180;
			var aspectRatio = (float)Window.Size.Width / Window.Size.Height;
			Cameras.Set(MainCamera.ID, new Camera {
				Viewport   = new Rectangle(Point.Empty, size),
				Projection = Matrix4x4.CreatePerspectiveFieldOfView(
					60.0F * DEGREES_TO_RADIANS, aspectRatio, 0.1F, 100.0F),
			});
		}

		private void OnUpdate(double delta)
		{

		}

		private void OnRender(double delta)
		{
			GFX.Clear(Color.Indigo);
			_program.Use();

			for (var cameraIndex = 0; cameraIndex < Cameras.Count; cameraIndex++) {
				var cameraID       = Cameras.GetEntityIDByIndex(cameraIndex);
				ref var camera     = ref Cameras.GetComponentByIndex(cameraIndex);
				ref var view       = ref Transforms.Get(cameraID).Value;
				ref var projection = ref camera.Projection;
				// TODO: view probably needs to be inverted once the transform represents a normal
				//       entity transform instead of being manually created from Matrix4x4.LookAt.
				GFX.Viewport(camera.Viewport);

				for (var transformIndex = 0; transformIndex < Transforms.Count; transformIndex++) {
					// Right now we render a mesh on every entity that has
					// a Transform component, so skip the camera itself.
					// TODO: Implement Mesh component.
					var entityID = Transforms.GetEntityIDByIndex(transformIndex);
					if (cameraID == entityID) continue;

					ref var modelView = ref Transforms.GetComponentByIndex(transformIndex);
					_mvpUniform.Set(modelView * view * projection);
					GFX.GL.DrawElements((GLEnum)PrimitiveType.Triangles, (uint)_indicesCount,
					                    (GLEnum)DrawElementsType.UnsignedShort, 0);
				}
			}

			Window.SwapBuffers();
		}
	}
}
