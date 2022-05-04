using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Program
	{
		public static Program Create()
			=> new(Gfx.Gl.CreateProgram());

		public static Program LinkFromShaders(string label, params Shader[] shaders)
		{
			var program = Create();
			program.Label = label;
			foreach (var shader in shaders)
				program.Attach(shader);
			program.Link();
			return program;
		}


		public uint Handle { get; }

		Program(uint handle) => Handle = handle;

		public string Label {
			get => Gfx.GetObjectLabel(ObjectIdentifier.Program, Handle);
			set => Gfx.SetObjectLabel(ObjectIdentifier.Program, Handle, value);
		}


		public void Attach(Shader shader)
			=> Gfx.Gl.AttachShader(Handle, shader.Handle);

		public void Link()
		{
			Gfx.Gl.LinkProgram(Handle);
			Gfx.Gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed linking Program \"{Label}\" ({Handle}):\n{Gfx.Gl.GetProgramInfoLog(Handle)}");
		}

		public void Detach(Shader shader)
			=> Gfx.Gl.DetachShader(Handle, shader.Handle);


		public ICollection<Shader> GetAttachedShaders()
		{
			Gfx.Gl.GetProgram(Handle, ProgramPropertyARB.AttachedShaders, out var count);
			var shaders = new uint[count];
			Gfx.Gl.GetAttachedShaders(Handle, (uint)count, out var _, out shaders[0]);
			return shaders.Select(handle => new Shader(handle)).ToArray();
		}

		public void DetachAndDeleteShaders()
		{
			var shaders = GetAttachedShaders();
			foreach (var shader in shaders) Detach(shader);
			foreach (var shader in shaders) shader.Delete();
		}


		public Uniforms GetActiveUniforms() => new(this);
		public VertexAttributes GetActiveAttributes() => new(this);

		public void Use() => Gfx.Gl.UseProgram(Handle);
		public void Delete() => Gfx.Gl.DeleteProgram(Handle);
	}
}
