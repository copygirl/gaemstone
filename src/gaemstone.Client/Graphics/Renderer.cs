using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public class Renderer : IProcessor
	{
		private Game _game = null!;
		private Program _program;
		private UniformMatrix4x4 _mvpUniform;
		private UniformBool _enableTextureUniform;


		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;
			_game.Window.Resize += OnWindowResize;
			_game.Window.Render += OnWindowRender;

			GFX.Initialize();
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			var vertexShaderSource   = _game.GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = _game.GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var attribs = _program.GetActiveAttributes();
			_game.MeshManager.ProgramAttributes = attribs;

			var uniforms = _program.GetActiveUniforms();
			_mvpUniform           = uniforms["modelViewProjection"].Matrix4x4;
			_enableTextureUniform = uniforms["enableTexture"].Bool;

			OnWindowResize(_game.Window.Size);
		}

		public void OnUnload()
		{
			_game.Window.Resize -= OnWindowResize;
			_game.Window.Render -= OnWindowRender;
		}

		public void OnUpdate(double delta) {  }


		public void OnWindowResize(Size size)
		{
			const float DEGREES_TO_RADIANS = MathF.PI / 180;
			var aspectRatio = (float)size.Width / size.Height;
			_game.Cameras.Set(_game.MainCamera.ID, new Camera {
				Viewport   = new Rectangle(Point.Empty, size),
				Projection = Matrix4x4.CreatePerspectiveFieldOfView(
					60.0F * DEGREES_TO_RADIANS, aspectRatio, 0.1F, 100.0F),
			});
		}

		public void OnWindowRender(double delta)
		{
			GFX.Clear(Color.Indigo);
			_program.Use();

			var cameraEnumerator = _game.Cameras.GetEnumerator();
			while (cameraEnumerator.MoveNext()) {
				var cameraID   = cameraEnumerator.CurrentEntityID;
				ref var camera = ref cameraEnumerator.CurrentComponent;
				Matrix4x4.Invert(_game.Transforms.Get(cameraID), out var view);
				var viewProjection = view * camera.Projection;
				GFX.Viewport(camera.Viewport);

				var meshEnumerator = _game.Meshes.GetEnumerator();
				while (meshEnumerator.MoveNext()) {
					var entityID      = meshEnumerator.CurrentEntityID;
					ref var mesh      = ref meshEnumerator.CurrentComponent;
					ref var modelView = ref _game.Transforms.GetRef(entityID).Value;
					_mvpUniform.Set(modelView * viewProjection);

					if (_game.Textures.TryGet(meshEnumerator.CurrentEntityID, out var texture)) {
						_enableTextureUniform.Set(true);
						texture.Bind();
					} else {
						_enableTextureUniform.Set(false);
					}

					var meshInfo = _game.MeshManager.Find(mesh);
					meshInfo.Draw();
				}
			}

			VertexArray.Unbind();
			_game.Window.SwapBuffers();
		}
	}
}
