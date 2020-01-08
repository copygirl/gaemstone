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
		private IComponentStore<Camera>      _cameraStore    = null!;
		private IComponentStore<Transform>   _transformStore = null!;
		private IComponentStore<Mesh>        _meshStore      = null!;
		private IComponentStore<Texture>     _textureStore   = null!;
		private IComponentStore<SpriteIndex> _spriteStore    = null!;

		private Program _program;
		private UniformMatrix4x4 _cameraMatrixUniform;
		private UniformMatrix4x4 _modelMatrixUniform;


		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;
			_cameraStore    = universe.Components.GetStore<Camera>();
			_transformStore = universe.Components.GetStore<Transform>();
			_meshStore      = universe.Components.GetStore<Mesh>();
			_textureStore   = universe.Components.GetStore<Texture>();
			_spriteStore    = universe.Components.GetStore<SpriteIndex>();

			_game.Window.Render += OnWindowRender;

			var vertexShaderSource   = _game.GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = _game.GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var attribs  = _program.GetActiveAttributes();
			var uniforms = _program.GetActiveUniforms();
			_cameraMatrixUniform = uniforms["cameraMatrix"].Matrix4x4;
			_modelMatrixUniform  = uniforms["modelMatrix"].Matrix4x4;
		}

		public void OnUnload()
			=> _game.Window.Render -= OnWindowRender;

		public void OnUpdate(double delta) {  }

		public void OnWindowRender(double delta)
		{
			var size = _game.Window.Size;
			GFX.Viewport(size);
			GFX.Clear(Color.Indigo);
			_program.Use();

			var cameraEnumerator = _cameraStore.GetEnumerator();
			while (cameraEnumerator.MoveNext()) {
				var cameraID = cameraEnumerator.CurrentEntityID;
				var camera   = cameraEnumerator.CurrentComponent;

				// Get the camera's transform matrix and invert it.
				var cameraTransform = (Matrix4x4)_transformStore.Get(cameraID);
				Matrix4x4.Invert(cameraTransform, out cameraTransform);
				// Create the camera's projection matrix, either ortho or perspective.
				var cameraProjection = camera.IsOrthographic
					? Matrix4x4.CreateOrthographic(size.Width, -size.Height,
						camera.NearPlane, camera.FarPlane)
					: Matrix4x4.CreatePerspectiveFieldOfView(
						camera.FieldOfView * MathF.PI / 180, // Degrees => Radians
						(float)size.Width / size.Height,     // Aspect Ratio
						camera.NearPlane, camera.FarPlane);
				// Set the uniform to the combined transform and projection.
				_cameraMatrixUniform.Set(cameraTransform * cameraProjection);

				var meshEnumerator = _meshStore.GetEnumerator();
				while (meshEnumerator.MoveNext()) {
					var entityID  = meshEnumerator.CurrentEntityID;
					var mesh      = meshEnumerator.CurrentComponent;
					var modelView = _transformStore.Get(entityID).Value;
					_modelMatrixUniform.Set(modelView);

					if (_textureStore.TryGet(entityID, out var texture)) {
						using (texture.Bind()) {
							if (_spriteStore.TryGet(entityID, out var spriteIndex))
								mesh.Draw(spriteIndex.Value * 6, 6);
							else mesh.Draw();
						}
					} else {
						mesh.Draw();
					}
				}
			}

			VertexArray.Unbind();
		}
	}
}
