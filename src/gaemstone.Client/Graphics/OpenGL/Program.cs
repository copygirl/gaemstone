using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Program
	{
		public static Program Create()
			=> new Program(GFX.GL.CreateProgram());

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

		private Program(uint handle) => Handle = handle;

		public string Label {
			get => GFX.GetObjectLabel(ObjectLabelIdentifier.Program, Handle);
			set => GFX.SetObjectLabel(ObjectLabelIdentifier.Program, Handle, value);
		}


		public void Attach(Shader shader)
			=> GFX.GL.AttachShader(Handle, shader.Handle);

		public void Link()
		{
			GFX.GL.LinkProgram(Handle);
			GFX.GL.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out var result);
			if (result != (int)GLEnum.True) throw new Exception(
				$"Failed linking Program \"{Label}\" ({Handle}):\n{GFX.GL.GetProgramInfoLog(Handle)}");
		}

		public void Detach(Shader shader)
			=> GFX.GL.DetachShader(Handle, shader.Handle);


		public ICollection<Shader> GetAttachedShaders()
		{
			GFX.GL.GetProgram(Handle, ProgramPropertyARB.AttachedShaders, out var count);
			Span<uint> countSpan   = stackalloc uint[1];
			Span<uint> shadersSpan = stackalloc uint[count];
			GFX.GL.GetAttachedShaders(Handle, (uint)count, countSpan, shadersSpan);

			var shaders = new List<Shader>(count);
			foreach (var handle in shadersSpan) shaders.Add(new Shader(handle));
			return shaders;
		}

		public void DetachAndDeleteShaders()
		{
			var shaders = GetAttachedShaders();
			foreach (var shader in shaders) Detach(shader);
			foreach (var shader in shaders) shader.Delete();
		}


		public Uniforms GetActiveUniforms()
			=> new Uniforms(this);

		public VertexAttributes GetActiveAttributes()
			=> new VertexAttributes(this);


		public void Use()
			=> GFX.GL.UseProgram(Handle);

		public void Delete()
			=> GFX.GL.DeleteProgram(Handle);
	}
}
