using System;
using System.IO;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace gaemstone.Client.Graphics
{
	public readonly struct Texture
	{
		public static Texture Gen(TextureTarget target)
			=> new Texture(GFX.GL.GenTexture(), target);

		public static Texture CreateFromStream(Stream stream)
		{
			var texture = Gen(TextureTarget.Texture2D);
			texture.Bind();

			var image = Image.Load<Rgba32>(stream);
			unsafe { fixed (Rgba32* pixels = &image.Frames[0].GetPixelSpan()[0]) {
				GFX.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)PixelFormat.Rgba,
				                  (uint)image.Width, (uint)image.Height, 0,
				                  PixelFormat.Rgba, PixelType.Byte, pixels);
			} }

			GFX.GL.TexParameterI(TextureTarget.Texture2D,
				TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GFX.GL.TexParameterI(TextureTarget.Texture2D,
				TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

			return texture;
		}


		public uint Handle { get; }
		public TextureTarget Target { get; }

		private Texture(uint handle, TextureTarget target)
			=> (Handle, Target) = (handle, target);

		public void Bind()
			=> GFX.GL.BindTexture(Target, Handle);
	}
}
