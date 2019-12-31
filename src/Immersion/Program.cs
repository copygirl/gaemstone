using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using gaemstone.Bloxel;
using gaemstone.Bloxel.Chunks;
using gaemstone.Bloxel.Client;
using gaemstone.Client;
using gaemstone.Client.Components;
using gaemstone.Client.Graphics;
using gaemstone.Common.Components;
using gaemstone.Common.ECS.Stores;
using gaemstone.Common.Utility;

namespace Immersion
{
	public class Program : Game
	{
		private static void Main(string[] args)
			=> new Program().Run();


		public Random RND { get; } = new Random();
		public ChunkMeshGenerator ChunkMeshGenerator { get; }

		public Program()
		{
			Window.Title = "gæmstone: Immersion";
			Components.AddStore(new PackedArrayStore<TextureCoords4>());
			Components.AddStore(new LookupDictionaryStore<ChunkPos, Chunk>(chunk => chunk.Position));
			Components.AddStore(new DictionaryStore<ChunkPaletteStorage<Block>>());
			ChunkMeshGenerator = new ChunkMeshGenerator(this);
		}


		protected override void OnLoad()
		{
			base.OnLoad();
			var (mainCamera, _) = GetAll<MainCamera>().First();
			Set(mainCamera, (Transform)Matrix4x4.CreateTranslation(0, 26, 0));

			var heartMesh = MeshManager.Load("heart.glb").ID;
			var swordMesh = MeshManager.Load("sword.glb").ID;

			for (var x = -12; x <= 12; x++)
			for (var z = -12; z <= 12; z++) {
				var entity   = Entities.New();
				var position = Matrix4x4.CreateTranslation(x * 2, 24, z * 2);
				var rotation = Matrix4x4.CreateRotationY(RND.NextFloat(MathF.PI * 2));
				Set(entity, (Transform)(rotation * position));
				Set(entity, RND.Pick(heartMesh, swordMesh));
			}


			Texture texture;
			using (var stream = GetResourceStream("terrain.png"))
				texture = Texture.CreateFromStream(stream);

			var stone = Entities.New();
			var dirt  = Entities.New();
			var grass = Entities.New();
			Set(stone, TextureCoords4.FromGrid(new Size(64, 64), 1, 0, 16));
			Set(dirt , TextureCoords4.FromGrid(new Size(64, 64), 2, 0, 16));
			Set(grass, TextureCoords4.FromGrid(new Size(64, 64), 3, 0, 16));


			void CreateChunk(ChunkPos pos)
			{
				var chunk = Entities.New();
				var storage = new ChunkPaletteStorage<Block>(default(Block));
				for (var x = 0; x < 16; x++)
				for (var y = 0; y < 16; y++)
				for (var z = 0; z < 16; z++) {
					var yy = (pos.Y << 4) | y;
					if (RND.NextBool(0.5 - yy / 48.0))
						storage[x, y, z] = new Block((yy >  16) ? grass
						                           : (yy > -16) ? dirt
						                                        : stone);
				}

				Set(chunk, new Chunk(pos));
				Set(chunk, (Transform)Matrix4x4.CreateTranslation(pos.GetOrigin()));
				Set(chunk, storage);
			}

			var chunkStore = (LookupDictionaryStore<ChunkPos, Chunk>)Components.GetStore<Chunk>();
			void GenerateChunkMesh(ChunkPos pos)
			{
				var chunk = Entities.GetByID(chunkStore.GetEntityID(pos))!.Value;
				var chunkMesh = ChunkMeshGenerator.Generate(pos);
				if (chunkMesh == null) return;
				Set(chunk, chunkMesh.ID);
				Set(chunk, texture);
			}

			for (var x = -6; x < 6; x++)
			for (var y = -2; y < 2; y++)
			for (var z = -6; z < 6; z++)
				CreateChunk(new ChunkPos(x, y, z));

			for (var x = -6; x < 6; x++)
			for (var y = -2; y < 2; y++)
			for (var z = -6; z < 6; z++)
				GenerateChunkMesh(new ChunkPos(x, y, z));
		}


		public override Stream GetResourceStream(string name)
			=> typeof(Program).Assembly.GetManifestResourceStream("Immersion.Resources." + name)
				?? throw new ArgumentException($"Could not find embedded resource '{name}'");
	}
}
