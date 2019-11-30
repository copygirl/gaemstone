using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Client.Graphics;
using gaemstone.Common.ECS;
using Silk.NET.OpenGL;
using Silk.NET.Windowing.Common;

namespace gaemstone.Client
{
	public class Game
	{
		private const float DEGREES_TO_RADIANS = MathF.PI / 180;
		private const float RADIANS_TO_DEGREES = 180 / MathF.PI;

		private static void Main(string[] args)
			=> new Game(args).Run();


		public IWindow Window { get; }

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
				cameraPosition : new Vector3(4, 3, 3),
				cameraTarget   : new Vector3(0, 0, 0),
				cameraUpVector : new Vector3(0, 1, 0)));

			for (var x = -2; x <= 2; x++)
			for (var z = -2; z <= 2; z++) {
				var entity = Entities.New();
				Transforms.Set(entity.ID, Matrix4x4.CreateTranslation(x * 4, 0, z * 4));
			}

			// Destroy one of the entities.
			Entities.Destroy(Entities.GetByID(12)!.Value);
		}

		public void Run()
		{
			Window.Run();
		}


		private Program _program;
		private UniformMatrix4x4 _mvpUniform;
		private VertexArray _vertexArray;
		private Buffer<Vector3> _vertexBuffer;
		private Buffer<Vector3> _colorBuffer;

		private readonly Vector3[] _vertexBufferData = {
			// -X
			new Vector3(-1, 1, 1), new Vector3(-1,-1, 1), new Vector3(-1, 1,-1),
			new Vector3(-1,-1,-1), new Vector3(-1, 1,-1), new Vector3(-1,-1, 1),
			// -Y
			new Vector3(-1,-1,-1), new Vector3(-1,-1, 1), new Vector3( 1,-1,-1),
			new Vector3( 1,-1, 1), new Vector3( 1,-1,-1), new Vector3(-1,-1, 1),
			// -Z
			new Vector3(-1, 1,-1), new Vector3(-1,-1,-1), new Vector3( 1, 1,-1),
			new Vector3( 1,-1,-1), new Vector3( 1, 1,-1), new Vector3(-1,-1,-1),
			// +X
			new Vector3( 1, 1,-1), new Vector3( 1,-1,-1), new Vector3( 1, 1, 1),
			new Vector3( 1,-1, 1), new Vector3( 1, 1, 1), new Vector3( 1,-1,-1),
			// +Y
			new Vector3(-1, 1, 1), new Vector3(-1, 1,-1), new Vector3( 1, 1, 1),
			new Vector3( 1, 1,-1), new Vector3( 1, 1, 1), new Vector3(-1, 1,-1),
			// +Z
			new Vector3( 1, 1, 1), new Vector3(-1, 1, 1), new Vector3( 1,-1, 1),
			new Vector3(-1,-1, 1), new Vector3( 1,-1, 1), new Vector3(-1, 1, 1),
		};

		private static readonly Random _rnd = new Random();
		private readonly Vector3[] _colorBufferData
			= Enumerable.Range(0, 6 * 2 * 3) // Sides * TrianglesPerSide * VerticesPerTriangle
				.Select(i => new Vector3((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble()))
				.ToArray();

		private void OnLoad()
		{
			GFX.Initialize();
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			string GetResourceAsString(string name)
			{
				name = "gaemstone.Client.Resources." + name;
				using (var stream = typeof(Game).Assembly.GetManifestResourceStream(name)!)
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

			_vertexArray = VertexArray.Gen();
			_vertexArray.Bind();

			_vertexBuffer = Buffer<Vector3>.CreateFromData(_vertexBufferData);
			attribs["position"].Pointer(3, VertexAttribPointerType.Float);

			_colorBuffer = Buffer<Vector3>.CreateFromData(_colorBufferData);
			attribs["color"].Pointer(3, VertexAttribPointerType.Float);

			OnResize(Window.Size);
		}

		private void OnClosing()
		{

		}

		private void OnResize(Size size)
		{
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
					GFX.GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexBufferData.Length);
				}
			}

			Window.SwapBuffers();
		}
	}
}
