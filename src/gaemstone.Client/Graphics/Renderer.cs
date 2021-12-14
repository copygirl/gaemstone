using System;
using System.Drawing;
using System.Numerics;
using gaemstone.Common;
using gaemstone.Common.Processors;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public class Renderer : IProcessor
	{
		public Game Game { get; } = null!;

		Program _program;
		UniformMatrix4x4 _cameraMatrixUniform;
		UniformMatrix4x4 _modelMatrixUniform;

		IQuery? _cameraQuery;
		IQuery? _renderableQuery;

		public void OnLoad()
		{
			Game.Window.Render += OnWindowRender;

			var vertexShaderSource   = Game.GetResourceAsString("default.vs.glsl");
			var fragmentShaderSource = Game.GetResourceAsString("default.fs.glsl");

			_program = Program.LinkFromShaders("main",
				Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
				Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
			_program.DetachAndDeleteShaders();

			var attribs  = _program.GetActiveAttributes();
			var uniforms = _program.GetActiveUniforms();
			_cameraMatrixUniform = uniforms["cameraMatrix"].Matrix4x4;
			_modelMatrixUniform  = uniforms["modelMatrix"].Matrix4x4;

			_cameraQuery     = Game.Queries.New("QueryRenderCamera").Execute(RenderCamera).Build();
			_renderableQuery = Game.Queries.New("QueryRenderEntity").Execute(RenderEntity).Build();
		}

		public void OnUnload()
			=> Game.Window.Render -= OnWindowRender;

		public void OnUpdate(double delta) {  }

		public void OnWindowRender(double delta)
		{
			GFX.Clear(Color.Indigo);
			_program.Use();

			_cameraQuery!.Run();

			VertexArray.Unbind();
		}

		public void RenderCamera(Camera camera, in Transform transform)
		{
			var clearColor = camera.ClearColor ?? Color.Indigo;
			var viewport   = camera.Viewport ?? new(0, 0, Game.Window.Size.X, Game.Window.Size.Y);
			GFX.Viewport(viewport);
			GFX.Clear(clearColor, viewport);

			// Get the camera's transform matrix and invert it.
			Matrix4x4.Invert(transform, out var cameraTransform);
			// Create the camera's projection matrix, either ortho or perspective.
			var cameraProjection = camera.IsOrthographic
				? Matrix4x4.CreateOrthographic(
					viewport.Size.Width, -viewport.Size.Height,
					camera.NearPlane, camera.FarPlane)
				: Matrix4x4.CreatePerspectiveFieldOfView(
					camera.FieldOfView * MathF.PI / 180,               // Degrees => Radians
					(float)viewport.Size.Width / viewport.Size.Height, // Aspect Ratio
					camera.NearPlane, camera.FarPlane);
			// Set the uniform to thequery. combined transform and projection.
			_cameraMatrixUniform.Set(cameraTransform * cameraProjection);

			_renderableQuery!.Run();
		}

		public void RenderEntity(
			in Mesh mesh, in Transform transform,
			Texture? texture, SpriteIndex? spriteIndex)
		{
			_modelMatrixUniform.Set(transform);
			// If entity has Texture, bind it now.
			if (texture.HasValue) texture.Value.Bind();

			// If entity has SpriteIndex, only render two
			// triangles out of the mesh specified by that index.
			if (spriteIndex.HasValue)
				mesh.Draw(spriteIndex.Value * 6, 6);
			// Otherwise just render the entire mesh like usual.
			mesh.Draw();

			// If entity has Texture, unbind it after it has been rendered.
			if (texture.HasValue) texture.Value.Unbind();

			// FIXME: It appears that a texture was still bound even if none was bound by this method.
		}
	}
}
