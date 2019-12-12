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
		private Program _program;
		private UniformMatrix4x4 _mvpUniform;
		private UniformBool _enableTextureUniform;

		public Game Game { get; private set; } = null!;

		public void OnLoad(Universe universe)
		{
			Game = (Game)universe;
			Game.Window.Resize += OnWindowResize;
			Game.Window.Render += OnWindowRender;

			GFX.Initialize();
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			var vertexShaderSource   = Game.GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = Game.GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var attribs = _program.GetActiveAttributes();
			Game.MeshManager.ProgramAttributes = attribs;

			var uniforms = _program.GetActiveUniforms();
			_mvpUniform           = uniforms["modelViewProjection"].Matrix4x4;
			_enableTextureUniform = uniforms["enableTexture"].Bool;

			OnWindowResize(Game.Window.Size);
		}

		public void OnUnload()
		{
			Game.Window.Resize -= OnWindowResize;
			Game.Window.Render -= OnWindowRender;
		}

		public void OnUpdate(double delta) {  }


		public void OnWindowResize(Size size)
		{
			const float DEGREES_TO_RADIANS = MathF.PI / 180;
			var aspectRatio = (float)size.Width / size.Height;
			Game.Cameras.Set(Game.MainCamera.ID, new Camera {
				Viewport   = new Rectangle(Point.Empty, size),
				Projection = Matrix4x4.CreatePerspectiveFieldOfView(
					60.0F * DEGREES_TO_RADIANS, aspectRatio, 0.1F, 100.0F),
			});
		}

		public void OnWindowRender(double delta)
		{
			GFX.Clear(Color.Indigo);
			_program.Use();

			var cameraEnumerator = Game.Cameras.GetEnumerator();
			while (cameraEnumerator.MoveNext()) {
				var cameraID       = cameraEnumerator.CurrentEntityID;
				ref var camera     = ref cameraEnumerator.CurrentComponent;
				ref var view       = ref Game.Transforms.GetRef(cameraID).Value;
				ref var projection = ref camera.Projection;
				// TODO: "view" probably needs to be inverted once the transform represents a normal
				//       entity transform instead of being manually created from Matrix4x4.LookAt.
				GFX.Viewport(camera.Viewport);

				var meshEnumerator = Game.Meshes.GetEnumerator();
				while (meshEnumerator.MoveNext()) {
					var entityID      = meshEnumerator.CurrentEntityID;
					ref var mesh      = ref meshEnumerator.CurrentComponent;
					ref var modelView = ref Game.Transforms.GetRef(entityID).Value;
					var meshInfo      = Game.MeshManager.Find(mesh);
					_mvpUniform.Set(modelView * view * projection);

					if (Game.Textures.TryGet(meshEnumerator.CurrentEntityID, out var texture)) {
						_enableTextureUniform.Set(true);
						texture.Bind();
					} else {
						_enableTextureUniform.Set(false);
					}

					meshInfo.Draw();
				}
			}

			Game.Window.SwapBuffers();
		}
	}
}
