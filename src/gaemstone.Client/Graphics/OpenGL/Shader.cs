using System;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Shader
	{
		public static Shader Create(ShaderType type)
			=> new(GFX.GL.CreateShader(type));

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
			GFX.GL.GetShader(Handle, GLEnum.ShaderType, out var shaderType);
			return (ShaderType)shaderType;
		} }

		public string Label {
			get => GFX.GetObjectLabel(ObjectIdentifier.Shader, Handle);
			set => GFX.SetObjectLabel(ObjectIdentifier.Shader, Handle, value);
		}

		public string Source {
			get {
				GFX.GL.GetShader(Handle, GLEnum.ShaderSourceLength, out var sourceLength);
				var source = new string(' ', sourceLength);
				GFX.GL.GetShaderSource(Handle, (uint)source.Length, out _, out source);
				return source;
			}
			set => GFX.GL.ShaderSource(Handle, value);
		}

		public void Compile()
		{
			GFX.GL.CompileShader(Handle);
			GFX.GL.GetShader(Handle, ShaderParameterName.CompileStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed compiling shader \"{Label}\" ({Handle}):\n{GFX.GL.GetShaderInfoLog(Handle)}");
		}

		public void Delete()
			=> GFX.GL.DeleteShader(Handle);
	}
}
