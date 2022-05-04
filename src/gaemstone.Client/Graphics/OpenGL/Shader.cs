using System;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Shader
	{
		public static Shader Create(ShaderType type)
			=> new(Gfx.Gl.CreateShader(type));

		public static Shader CompileFromSource(string label, ShaderType type, string source)
		{
			var shader = Create(type);
			shader.Label  = label;
			shader.Source = source;
			shader.Compile();
			return shader;
		}


		public uint Handle { get; }

		internal Shader(uint handle) => Handle = handle;

		public ShaderType Type { get {
			Gfx.Gl.GetShader(Handle, GLEnum.ShaderType, out var shaderType);
			return (ShaderType)shaderType;
		} }

		public string Label {
			get => Gfx.GetObjectLabel(ObjectIdentifier.Shader, Handle);
			set => Gfx.SetObjectLabel(ObjectIdentifier.Shader, Handle, value);
		}

		public string Source {
			get {
				Gfx.Gl.GetShader(Handle, GLEnum.ShaderSourceLength, out var sourceLength);
				var source = new string(' ', sourceLength);
				Gfx.Gl.GetShaderSource(Handle, (uint)source.Length, out _, out source);
				return source;
			}
			set => Gfx.Gl.ShaderSource(Handle, value);
		}

		public void Compile()
		{
			Gfx.Gl.CompileShader(Handle);
			Gfx.Gl.GetShader(Handle, ShaderParameterName.CompileStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed compiling shader \"{Label}\" ({Handle}):\n{Gfx.Gl.GetShaderInfoLog(Handle)}");
		}

		public void Delete()
			=> Gfx.Gl.DeleteShader(Handle);
	}
}
