using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using gaemstone.Client.Bloxel.Blocks;
using gaemstone.Client.Bloxel.Chunks;
using gaemstone.Client.Components;
using gaemstone.Client.Graphics;
using gaemstone.Client.Processors;
using gaemstone.Common.Bloxel.Chunks;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Stores;
using gaemstone.Common.Utility;
using Silk.NET.Windowing.Common;

namespace gaemstone.Client
{
	public class Game : Universe
	{
		private static void Main(string[] args)
			=> new Game(args).Run();


		public IWindow Window { get; }
		public Random RND { get; } = new Random();

		public MeshManager MeshManager { get; }
		public ChunkMeshGenerator ChunkMeshGenerator { get; }

		public Entity MainCamera { get; private set; }


		private Game(string[] args)
		{
			Window = Silk.NET.Windowing.Window.Create(new WindowOptions {
				Title = "gæmstone",
				Size  = new Size(1280, 720),
				API   = GraphicsAPI.Default,
				UpdatesPerSecond = 30.0,
				FramesPerSecond  = 60.0,
			});
			Window.Load    += OnLoad;
			Window.Update  += OnUpdate;
			Window.Closing += OnClosing;

			Components.AddStore(new PackedArrayStore<Transform>());
			Components.AddStore(new PackedArrayStore<Camera>());
			Components.AddStore(new PackedArrayStore<Mesh>());
			Components.AddStore(new PackedArrayStore<Texture>());
			Components.AddStore(new PackedArrayStore<TextureCell>());
			Components.AddStore(new LookupDictionaryStore<ChunkPos, Chunk>(chunk => chunk.Position));
			Components.AddStore(new DictionaryStore<ChunkPaletteStorage<Block>>());

			MeshManager        = new MeshManager(this);
			ChunkMeshGenerator = new ChunkMeshGenerator(this);
		}

		public void Run()
		{
			Window.Run();
		}


		public Stream GetResourceStream(string name)
			=> typeof(Game).Assembly.GetManifestResourceStream("gaemstone.Client.Resources." + name)
				?? throw new ArgumentException($"Could not find embedded resource '{name}'");

		public string GetResourceAsString(string name)
		{
			using (var stream = GetResourceStream(name))
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}


		private void OnLoad()
		{
			MainCamera = Entities.New();
			Set(MainCamera, (Transform)Matrix4x4.CreateTranslation(0, 26, 0));

			// TODO: This currently has to sit exactly here.
			//       Renderer requires MainCamera, and it initializes GFX,
			//       which is required for MeshManager to create meshes.
			Processors.Start<Renderer>();
			Processors.Start<CameraController>();

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
			Set(stone, new TextureCell(1, 0, 16, new Size(64, 64)));
			Set(dirt , new TextureCell(2, 0, 16, new Size(64, 64)));
			Set(grass, new TextureCell(3, 0, 16, new Size(64, 64)));


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

		private void OnClosing()
		{

		}

		private void OnUpdate(double delta)
		{
			foreach (var processor in Processors)
				processor.OnUpdate(delta);
		}
	}
}
