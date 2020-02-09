using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using gaemstone.Client.Components;
using gaemstone.Client.Graphics;
using gaemstone.Client.Processors;
using gaemstone.Common.Components;
using gaemstone.Common.ECS;
using gaemstone.Common.ECS.Stores;
using Silk.NET.Windowing.Common;

namespace gaemstone.Client
{
	public abstract class Game : Universe
	{
		public IWindow Window { get; }

		public Game()
		{
			var options = WindowOptions.Default;
			options.Title = "gæmstone";
			options.Size  = new Size(1280, 720);
			options.API   = GraphicsAPI.Default;
			options.UpdatesPerSecond = 30.0;
			options.FramesPerSecond  = 60.0;
			options.ShouldSwapAutomatically = true;

			Window = Silk.NET.Windowing.Window.Create(options);
			Window.Load    += OnLoad;
			Window.Update  += OnUpdate;
			Window.Closing += OnClosing;

			Components.AddStore(new PackedArrayStore<Transform>());
			Components.AddStore(new PackedArrayStore<Mesh>());
			Components.AddStore(new PackedArrayStore<Texture>());
			Components.AddStore(new PackedArrayStore<SpriteIndex>());
			Components.AddStore(new DictionaryStore<Camera>());
		}

		public void Run()
		{
			Window.Run();
		}


		protected virtual void OnLoad()
		{
			GFX.Initialize();
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			Processors.Start<Renderer>();
			Processors.Start<TextureManager>();
			Processors.Start<MeshManager>();
			Processors.Start<CameraController>();

			var mainCamera = Entities.New();
			Set(mainCamera, (Transform)Matrix4x4.Identity);
			Set(mainCamera, Camera.Default3D);
		}

		protected virtual void OnClosing()
		{

		}

		protected virtual void OnUpdate(double delta)
		{
			foreach (var processor in Processors)
				processor.OnUpdate(delta);
		}


		public abstract Stream GetResourceStream(string name);

		public string GetResourceAsString(string name)
		{
			using (var stream = GetResourceStream(name))
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}

		public byte[] GetResourceAsBytes(string name)
		{
			using (var stream = GetResourceStream(name))
			using (var memoryStream = new MemoryStream()) {
				stream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
	}
}
