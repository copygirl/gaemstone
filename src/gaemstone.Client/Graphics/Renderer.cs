using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public class Renderer : IProcessor
	{
		struct CameraQuery
		{
			public Camera Camera { get; }
			public Transform Transform { get; }
		}

		struct RenderableQuery
		{
			public Mesh Mesh { get; }
			public Transform Transform { get; }
			public Texture? Texture { get; }
			public SpriteIndex? SpriteIndex { get; }
		}

		Game _game = null!;
		Program _program;
		UniformMatrix4x4 _cameraMatrixUniform;
		UniformMatrix4x4 _modelMatrixUniform;

		public void OnLoad(Universe universe)
		{
			_game = (Game)universe;
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
			GFX.Viewport(new Size(size.X, size.Y));
			GFX.Clear(Color.Indigo);
			_program.Use();

			_game.Queries.Run((ref CameraQuery e) => {
				// Get the camera's transform matrix and invert it.
				Matrix4x4.Invert(e.Transform, out var cameraTransform);
				// Create the camera's projection matrix, either ortho or perspective.
				var cameraProjection = e.Camera.IsOrthographic
					? Matrix4x4.CreateOrthographic(size.X, -size.Y,
						e.Camera.NearPlane, e.Camera.FarPlane)
					: Matrix4x4.CreatePerspectiveFieldOfView(
						e.Camera.FieldOfView * MathF.PI / 180, // Degrees => Radians
						(float)size.X / size.Y,              // Aspect Ratio
						e.Camera.NearPlane, e.Camera.FarPlane);
				// Set the uniform to thequery. combined transform and projection.
				_cameraMatrixUniform.Set(cameraTransform * cameraProjection);

				_game.Queries.Run((ref RenderableQuery e) => {
					_modelMatrixUniform.Set(e.Transform);
					// If entity has Texture, bind it now.
					if (e.Texture.HasValue) e.Texture.Value.Bind();

					// If entity has SpriteIndex, only render two
					// triangles out of the mesh specified by that index.
					if (e.SpriteIndex.HasValue)
						e.Mesh.Draw(e.SpriteIndex.Value * 6, 6);
					// Otherwise just render the entire mesh like usual.
					e.Mesh.Draw();

					// If entity has Texture, unbind it after it has been rendered.
					if (e.Texture.HasValue) e.Texture.Value.Unbind();
				});
			});

			VertexArray.Unbind();
		}
	}
}
