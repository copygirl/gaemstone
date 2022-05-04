using System;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Texture
	{
		public static Texture Gen(TextureTarget target)
			=> new(target, Gfx.Gl.GenTexture());


		public TextureTarget Target { get; }
		public uint Handle { get; }

		public Texture(TextureTarget target, uint handle)
			=> (Target, Handle) = (target, handle);


		public UnbindOnDispose Bind()
		{
			Gfx.Gl.BindTexture(Target, Handle);
			return new(Target);
		}

		public void Unbind()
			=> Gfx.Gl.BindTexture(Target, 0);
		public static void Unbind(TextureTarget target)
			=> Gfx.Gl.BindTexture(target, 0);

		public readonly struct UnbindOnDispose
			: IDisposable
		{
			public TextureTarget Target { get; }
			public UnbindOnDispose(TextureTarget target) => Target = target;
			public void Dispose() => Gfx.Gl.BindTexture(Target, 0);
		}


		public TextureMagFilter MagFilter {
			get {
				Gfx.Gl.GetTexParameterI(Target, GetTextureParameter.TextureMagFilter, out int value);
				return (TextureMagFilter)value;
			}
			set => Gfx.Gl.TexParameterI(Target, TextureParameterName.TextureMagFilter, (int)value);
		}
		public TextureMinFilter MinFilter {
			get {
				Gfx.Gl.GetTexParameterI(Target, GetTextureParameter.TextureMinFilter, out int value);
				return (TextureMinFilter)value;
			}
			set => Gfx.Gl.TexParameterI(Target, TextureParameterName.TextureMinFilter, (int)value);
		}
	}
}
