using System;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Texture
	{
		public static Texture Gen(TextureTarget target)
			=> new(target, GFX.GL.GenTexture());


		public TextureTarget Target { get; }
		public uint Handle { get; }

		public Texture(TextureTarget target, uint handle)
			=> (Target, Handle) = (target, handle);


		public UnbindOnDispose Bind()
		{
			GFX.GL.BindTexture(Target, Handle);
			return new(Target);
		}

		public void Unbind()
			=> GFX.GL.BindTexture(Target, 0);
		public static void Unbind(TextureTarget target)
			=> GFX.GL.BindTexture(target, 0);

		public readonly struct UnbindOnDispose
			: IDisposable
		{
			public TextureTarget Target { get; }
			public UnbindOnDispose(TextureTarget target) => Target = target;
			public void Dispose() => GFX.GL.BindTexture(Target, 0);
		}


		public TextureMagFilter MagFilter {
			get {
				GFX.GL.GetTexParameterI(Target, GetTextureParameter.TextureMagFilter, out int value);
				return (TextureMagFilter)value;
			}
			set => GFX.GL.TexParameterI(Target, TextureParameterName.TextureMagFilter, (int)value);
		}
		public TextureMinFilter MinFilter {
			get {
				GFX.GL.GetTexParameterI(Target, GetTextureParameter.TextureMinFilter, out int value);
				return (TextureMinFilter)value;
			}
			set => GFX.GL.TexParameterI(Target, TextureParameterName.TextureMinFilter, (int)value);
		}
	}
}
