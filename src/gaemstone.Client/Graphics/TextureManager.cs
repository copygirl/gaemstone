using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Processors;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Size = System.Drawing.Size;

namespace gaemstone.Client.Graphics
{
	public class TextureManager
		: IProcessor
	{
		private readonly Dictionary<Texture, TextureInfo> _byTexture
			= new Dictionary<Texture, TextureInfo>();
		private readonly Dictionary<string, TextureInfo> _bySourceFile
			= new Dictionary<string, TextureInfo>();


		public Texture Load(Game game, string name)
		{
			using (var stream = game.GetResourceStream(name))
				return CreateFromStream(stream, name);
		}

		public Texture CreateFromStream(Stream stream, string? sourceFile = null)
		{
			var texture = Texture.Gen(TextureTarget.Texture2D);
			texture.Bind();

			var image = Image.Load<Rgba32>(stream);
			unsafe { {
				image.Frames[0].TryGetSinglePixelSpan(out Span<Rgba32> result);
				var pixels = new ReadOnlySpan<Rgba32>(result.ToArray());
				GFX.GL.TexImage2D(texture.Target, 0, (int)PixelFormat.Rgba,
				                  (uint)image.Width, (uint)image.Height, 0,
				                  PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
			} }

			texture.MagFilter = TextureMagFilter.Nearest;
			texture.MinFilter = TextureMinFilter.Nearest;

			var info = new TextureInfo(texture, sourceFile, new Size(image.Width, image.Height));
			_byTexture.Add(texture, info);
			if (sourceFile != null) _bySourceFile.Add(sourceFile, info);

			return texture;
		}


		public TextureInfo? Lookup(Texture texture)
			=> _byTexture.TryGetValue(texture, out var value) ? value : null;
		public TextureInfo? Lookup(string sourceFile)
			=> _bySourceFile.TryGetValue(sourceFile, out var value) ? value : null;


		// IProcessor implementation

		public void OnLoad(Universe universe)
		{
			// Upload single-pixel white texture into texture slot 0, so when
			// "no" texture is bound, we can still use the texture sampler.
			var texture = new Texture(TextureTarget.Texture2D, 0);
			texture.Bind();
			unsafe {
				Span<byte> pixel = stackalloc byte[4];
				pixel.Fill(255);
				fixed (byte* ptr = pixel)
					GFX.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)PixelFormat.Rgba,
					                  1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
			}
			texture.MagFilter = TextureMagFilter.Nearest;
			texture.MinFilter = TextureMinFilter.Nearest;
		}

		public void OnUnload() {  }

		public void OnUpdate(double delta) {  }
	}

	public class TextureInfo
	{
		public Texture Texture { get; }
		public string? SourceFile { get; }
		public Size Size { get; }

		public TextureInfo(Texture texture, string? sourceFile, Size size)
			=> (Texture, SourceFile, Size) = (texture, sourceFile, size);
	}
}
