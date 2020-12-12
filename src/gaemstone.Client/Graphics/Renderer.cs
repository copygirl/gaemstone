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
        private Game _game = null!;
        private Program _program;
        private UniformMatrix4x4 _cameraMatrixUniform;
        private UniformMatrix4x4 _modelMatrixUniform;

        public void OnLoad(Universe universe)
        {
            _game = (Game)universe;
            _game.Window.Render += OnWindowRender;

            var vertexShaderSource = _game.GetResourceAsString("default.vs.glsl");
            var fragmentShaderSource = _game.GetResourceAsString("default.fs.glsl");

            _program = Program.LinkFromShaders("main",
                Shader.CompileFromSource("vertex", ShaderType.VertexShader, vertexShaderSource),
                Shader.CompileFromSource("fragment", ShaderType.FragmentShader, fragmentShaderSource));
            _program.DetachAndDeleteShaders();

            var attribs = _program.GetActiveAttributes();
            var uniforms = _program.GetActiveUniforms();
            _cameraMatrixUniform = uniforms["cameraMatrix"].Matrix4x4;
            _modelMatrixUniform = uniforms["modelMatrix"].Matrix4x4;
        }

        public void OnUnload()
            => _game.Window.Render -= OnWindowRender;

        public void OnUpdate(double delta) { }

        public void OnWindowRender(double delta)
        {
            var size = _game.Window.Size;
            GFX.Viewport(new Size(size.X, size.Y));
            GFX.Clear(Color.Indigo);
            _program.Use();

            Aspect<ICameraAspect>.ForEach(_game, camera =>
            {
                // Get the camera's transform matrix and invert it.
                Matrix4x4.Invert(camera.Transform, out var cameraTransform);
                // Create the camera's projection matrix, either ortho or perspective.
                var cameraProjection = camera.Camera.IsOrthographic
                    ? Matrix4x4.CreateOrthographic(size.X, -size.Y,
                        camera.Camera.NearPlane, camera.Camera.FarPlane)
                    : Matrix4x4.CreatePerspectiveFieldOfView(
                        camera.Camera.FieldOfView * MathF.PI / 180, // Degrees => Radians
                        (float)size.X / size.Y,            // Aspect Ratio
                        camera.Camera.NearPlane, camera.Camera.FarPlane);
                // Set the uniform to the combined transform and projection.
                _cameraMatrixUniform.Set(cameraTransform * cameraProjection);

                Aspect<IRenderableAspect>.ForEach(_game, renderable =>
                {
                    _modelMatrixUniform.Set(renderable.Transform);
                    // If entity has Texture, bind it now.
                    if (renderable.Texture != null) renderable.Texture.Value.Bind();

                    // If entity has SpriteIndex, only render two
                    // triangles out of the mesh specified by that index.
                    if (renderable.SpriteIndex != null)
                        renderable.Mesh.Draw(renderable.SpriteIndex.Value * 6, 6);
                    // Otherwise just render the entire mesh like usual.
                    renderable.Mesh.Draw();

                    // If entity has Texture, unbind it after it has been rendered.
                    if (renderable.Texture != null) renderable.Texture.Value.Unbind();
                });
            });

            VertexArray.Unbind();
        }
    }

    public interface ICameraAspect
    {
        Camera Camera { get; }
        Transform Transform { get; set; }
    }

    public interface IRenderableAspect
    {
        Mesh Mesh { get; }
        Transform Transform { get; }
        Texture? Texture { get; }
        SpriteIndex? SpriteIndex { get; }
    }
}