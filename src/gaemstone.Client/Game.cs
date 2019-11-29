using System.Linq;
using System.Numerics;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
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
		public GL GL { get; private set; }

		public Matrix4x4 Projection { get; private set; }
		public Matrix4x4 View { get; private set; }

		public EntityManager Entities { get; }
		public ComponentManager Components { get; }
		public PackedArrayComponentStore<Vector3> Positions { get; }


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

			// TODO: Only set in Run. Fix by extracting into separate "Graphics" class?
			GL = null!;

			Entities   = new EntityManager();
			Components = new ComponentManager(Entities);
			Positions  = new PackedArrayComponentStore<Vector3>();
			Components.AddStore(Positions);

			for (var x = -2; x <= 2; x++)
			for (var z = -2; z <= 2; z++) {
				var entity = Entities.New();
				Components.Set<Vector3>(entity.ID, true);
				Positions.Set(entity.ID, new Vector3(x * 4, 0, z * 4));
			}

			var entityID = 11u;
			// TODO: Add code to remove entity.
			Components.Set<Vector3>(entityID, false);
			Positions.Remove(entityID);
		}

		public void Run()
		{
			Window.Run();
		}


		private static readonly string VERTEX_SHADER_SOURCE = @"
			#version 330 core
			layout(location = 0) in vec3 position;
			layout(location = 1) in vec3 color;
			uniform mat4 modelViewProjection;
			out vec4 fragmentColor;
			void main(void)
			{
				gl_Position   = modelViewProjection * vec4(position, 1.0);
				fragmentColor = vec4(color, 1.0);
			}
		";

		private static readonly string FRAGMENT_SHADER_SOURCE = @"
			#version 330
			in vec4 fragmentColor;
			out vec4 outputColor;
			void main()
			{
				outputColor = fragmentColor;
			}
		";

		private uint _program;
		private uint _vertexArray;
		private uint _vertexBuffer;
		private uint _colorBuffer;
		private int _matrixUniform;

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

		private unsafe void OnLoad()
		{
			GL = Silk.NET.OpenGL.GL.GetApi();

			GL.Enable(GLEnum.DebugOutput);
			GL.DebugMessageCallback((source, type, id, severity, length, message, userParam)
				=> Console.WriteLine("[GLDebug] [{0}] {1}/{2}: {3}",
					severity.ToString().Substring(13),
					type.ToString().Substring(9), id,
					Marshal.PtrToStringAnsi(message)), null);

			GL.Enable(GLEnum.DepthTest);
			GL.DepthFunc(GLEnum.Less);

			var vertexShader   = CompileShaderFromSource("vertex", VERTEX_SHADER_SOURCE, ShaderType.VertexShader);
			var fragmentShader = CompileShaderFromSource("fragment", FRAGMENT_SHADER_SOURCE, ShaderType.FragmentShader);
			_program = LinkProgram("main", vertexShader, fragmentShader);
			_matrixUniform = GL.GetUniformLocation(_program, "modelViewProjection");

			GL.GenVertexArrays(1, out _vertexArray);
			GL.BindVertexArray(_vertexArray);

			GL.GenBuffers(1, out _vertexBuffer);
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
			fixed (Vector3* vertices = _vertexBufferData)
				GL.BufferData(BufferTargetARB.ArrayBuffer,
				              (uint)(_vertexBufferData.Length * sizeof(Vector3)),
				              vertices, BufferUsageARB.StaticDraw);

			GL.GenBuffers(1, out _colorBuffer);
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, _colorBuffer);
			fixed (Vector3* colors = _colorBufferData)
				GL.BufferData(BufferTargetARB.ArrayBuffer,
				              (uint)(_colorBufferData.Length * sizeof(Vector3)),
				              colors, BufferUsageARB.StaticDraw);

			OnResize(Window.Size);
			View = Matrix4x4.CreateLookAt(
				new Vector3(4, 3, 3),
				new Vector3(0, 0, 0),
				new Vector3(0, 1, 0));
		}

		private void OnClosing()
		{

		}

		private void OnResize(Size size)
		{
			GL.Viewport(size);

			var aspectRatio = (float)Window.Size.Width / Window.Size.Height;
			Projection = Matrix4x4.CreatePerspectiveFieldOfView(
				60.0F * DEGREES_TO_RADIANS, aspectRatio, 0.1F, 100.0F);
		}

		private void OnUpdate(double delta)
		{

		}

		private unsafe void OnRender(double delta)
		{
			GL.ClearColor(0.0F, 0.4F, 0.2F, 1.0F);
			GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

			GL.UseProgram(_program);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);

			GL.EnableVertexAttribArray(1);
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, _colorBuffer);
			GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, null);

			for (var i = 0; i < Positions.Count; i++) {
				var position = Positions.GetComponentByIndex(i);
				var model    = Matrix4x4.CreateTranslation(position);
				var modelViewProjection = model * View * Projection;
				GL.UniformMatrix4(_matrixUniform, 1, false, in modelViewProjection.M11);
				GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexBufferData.Length);
			}

			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);

			Window.SwapBuffers();
		}


		private uint CompileShaderFromSource(string name, string source, ShaderType type)
		{
			var shader = GL.CreateShader(type);
			GL.ObjectLabel(ObjectIdentifier.Shader, shader, (uint)name.Length, name);

			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);

			GL.GetShader(shader, ShaderParameterName.CompileStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed compiling shader '{name}':\n{GL.GetShaderInfoLog(shader)}");

			return shader;
		}

		private uint LinkProgram(string name, params uint[] shaders)
		{
			var program = GL.CreateProgram();
			GL.ObjectLabel(ObjectIdentifier.Program, program, (uint)name.Length, name);

			foreach (var shader in shaders)
				GL.AttachShader(program, shader);
			GL.LinkProgram(program);

			GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed linking program '{name}':\n{GL.GetProgramInfoLog(program)}");

			foreach (var shader in shaders)
				GL.DetachShader(program, shader);
			foreach (var shader in shaders)
				GL.DeleteShader(shader);

			return program;
		}
	}
}
