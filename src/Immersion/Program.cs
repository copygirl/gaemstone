using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;
using gaemstone.Bloxel;
using gaemstone.Bloxel.Chunks;
using gaemstone.Bloxel.Client;
using gaemstone.Client;
using gaemstone.Client.Graphics;
using gaemstone.Common;
using gaemstone.Common.Utility;

namespace Immersion
{
	public class Program : Game
	{
		static void Main() => new Program().Run();


		public Random Rnd { get; } = new();

		public Program()
		{
			// Fix the damn locale / culture for the entire program.
			var culture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentCulture     = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;

			Window.Title = "gæmstone: Immersion";
		}


		protected override void OnLoad()
		{
			base.OnLoad();

			NewComponent<Chunk>();
			NewComponent<ChunkPaletteStorage<Prototype>>();

			var meshManager = Processors.GetOrThrow<MeshManager>();
			var heartMesh = meshManager.Load(this, "heart.glb");
			var swordMesh = meshManager.Load(this, "sword.glb");

			for (var x = -12; x <= 12; x++)
			for (var z = -12; z <= 12; z++) {
				var position = Matrix4x4.CreateTranslation(x * 2, 25, z * 2);
				var rotation = Matrix4x4.CreateRotationY(Rnd.NextFloat(MathF.PI * 2));
				Entities.New()
					.Set((Transform)(rotation * position))
					.Set(Rnd.Pick(heartMesh, swordMesh));
			}

			MainCamera.Set((Transform)Matrix4x4.CreateTranslation(0, 26, 0));
			MainCamera.Set(heartMesh);

			var textureManager = Processors.GetOrThrow<TextureManager>();
			var texture = textureManager.Load(this, "terrain.png");

			var stone = Entities.New().Set(TextureCoords4.FromGrid(4, 4, 1, 0));
			var dirt  = Entities.New().Set(TextureCoords4.FromGrid(4, 4, 2, 0));
			var grass = Entities.New().Set(TextureCoords4.FromGrid(4, 4, 3, 0));

			void CreateChunk(ChunkPos pos)
			{
				var storage = new ChunkPaletteStorage<Prototype>(default);
				for (var x = 0; x < 16; x++)
				for (var y = 0; y < 16; y++)
				for (var z = 0; z < 16; z++) {
					var yy = (pos.Y << 4) | y;
					if (Rnd.NextBool(0.5 - yy / 48.0))
						storage[x, y, z] = new((yy >  16) ? grass
						                     : (yy > -16) ? dirt
						                                  : stone);
				}

				Entities.New()
					.Set(new Chunk(pos))
					.Set((Transform)Matrix4x4.CreateTranslation(pos.GetOrigin()))
					.Set(storage)
					.Set(texture);
			}

			var sizeH = 4;
			var sizeY = 2;
			for (var x = -sizeH; x < sizeH; x++)
			for (var y = -sizeY; y < sizeY; y++)
			for (var z = -sizeH; z < sizeH; z++)
				CreateChunk(new(x, y, z));

			Processors.Start<ChunkMeshGenerator>();
			// FIXME: PIP camera requires sorting.
			// Processors.Start<PictureInPictureFollow>();
		}


		public override Stream GetResourceStream(string name)
			=> typeof(Program).Assembly.GetManifestResourceStream("Immersion.Resources." + name)
				?? throw new ArgumentException($"Could not find embedded resource '{name}'");
	}
}
