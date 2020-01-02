using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using gaemstone.Common.ECS.Stores;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public class Renderer : IProcessor
	{
		private Game _game = null!;
		private IComponentStore<Camera>      _cameraStore     = null!;
		private IComponentStore<FullscreenCamera>  _mainCameraStore = null!;
		private IComponentStore<Transform>   _transformStore  = null!;
		private IComponentStore<IndexedMesh> _meshStore       = null!;
		private IComponentStore<Texture>     _textureStore    = null!;

		private Program _program;
		private UniformMatrix4x4 _cameraMatrixUniform;
		private UniformBool _enableTextureUniform;


		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;
			_cameraStore     = universe.Components.GetStore<Camera>();
			_mainCameraStore = universe.Components.GetStore<FullscreenCamera>();
			_transformStore  = universe.Components.GetStore<Transform>();
			_meshStore       = universe.Components.GetStore<IndexedMesh>();
			_textureStore    = universe.Components.GetStore<Texture>();

			_game.Window.Resize += OnWindowResize;
			_game.Window.Render += OnWindowRender;

			var vertexShaderSource   = _game.GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = _game.GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var attribs  = _program.GetActiveAttributes();
			var uniforms = _program.GetActiveUniforms();
			_cameraMatrixUniform  = uniforms["cameraMatrix"].Matrix4x4;
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
			const float DEG_TO_RAD = MathF.PI / 180;
			var aspectRatio = (float)size.Width / size.Height;

			var mainCameraEnumerator = _mainCameraStore.GetEnumerator();
			while (mainCameraEnumerator.MoveNext()) {
				var cameraID   = mainCameraEnumerator.CurrentEntityID;
				var mainCamera = mainCameraEnumerator.CurrentComponent;
				_cameraStore.Set(cameraID, new Camera {
					Viewport = new Rectangle(Point.Empty, size),
					Matrix   = mainCamera.IsOrthographic
						? Matrix4x4.CreateOrthographic(size.Width, size.Height,
							mainCamera.NearPlane, mainCamera.FarPlane)
						: Matrix4x4.CreatePerspectiveFieldOfView(
							mainCamera.FieldOfView * DEG_TO_RAD, aspectRatio,
							mainCamera.NearPlane, mainCamera.FarPlane)
				});
			}
		}

		public void OnWindowRender(double delta)
		{
			GFX.Clear(Color.Indigo);
			_program.Use();

			var cameraEnumerator = _cameraStore.GetEnumerator();
			while (cameraEnumerator.MoveNext()) {
				var cameraID = cameraEnumerator.CurrentEntityID;
				var camera   = cameraEnumerator.CurrentComponent;
				Matrix4x4.Invert(_transformStore.Get(cameraID), out var view);
				var viewProjection = view * camera.Matrix;
				GFX.Viewport(camera.Viewport);

				var meshEnumerator = _meshStore.GetEnumerator();
				while (meshEnumerator.MoveNext()) {
					var entityID  = meshEnumerator.CurrentEntityID;
					var mesh      = meshEnumerator.CurrentComponent;
					var modelView = _transformStore.Get(entityID).Value;
					_cameraMatrixUniform.Set(modelView * viewProjection);

					if (_textureStore.TryGet(meshEnumerator.CurrentEntityID, out var texture)) {
						_enableTextureUniform.Set(true);
						texture.Bind();
					} else {
						_enableTextureUniform.Set(false);
					}

					mesh.Draw();
				}
			}

			VertexArray.Unbind();
			_game.Window.SwapBuffers();
		}
	}
}
