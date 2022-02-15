using System;
using System.IO;
using System.Numerics;
using gaemstone.Client.Graphics;
using gaemstone.Client.Processors;
using gaemstone.Common;
using gaemstone.Common.Utility;
using gaemstone.ECS;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace gaemstone.Client
{
	public abstract class Game : Universe
	{
		public IWindow Window { get; }

		IInputContext? _input;
		public IInputContext Input => _input
			?? throw new InvalidOperationException("Cannot access Input before Run has been called");

		public Entity MainCamera { get; private set; }


		public Game()
		{
			var options = WindowOptions.Default;
			options.Title = "gæmstone";
			options.Size  = new(1280, 720);
			options.UpdatesPerSecond = 30.0;
			options.FramesPerSecond  = 60.0;
			options.PreferredDepthBufferBits = 24;

			Window = Silk.NET.Windowing.Window.Create(options);
			Window.Load    += OnLoad;
			Window.Update  += OnUpdate;
			Window.Closing += OnClosing;

			Processors.ProcessorLoadedPre += processor => {
				var property = processor.GetType().GetProperty(nameof(Game));
				if (property?.PropertyType == typeof(Game))
					TypeWrapper.For(processor.GetType()).GetFieldForAutoProperty(property)
						.ClassSetter.Invoke(processor, this);
			};
			Processors.ProcessorUnloadedPost += processor => {
				var property = processor.GetType().GetProperty(nameof(Game));
				if (property?.PropertyType == typeof(Game))
					TypeWrapper.For(processor.GetType()).GetFieldForAutoProperty(property)
						.ClassSetter.Invoke(processor, null);
			};
		}

		public void Run()
		{
			Window.Run();
		}


		protected virtual void OnLoad()
		{
			_input = Window.CreateInput();

			GFX.Initialize(Window);
			GFX.OnDebugOutput += (source, type, id, severity, message) =>
				Console.WriteLine($"[GLDebug] [{severity}] {type}/{id}: {message}");

			// TODO: Automatically create components that have a specific attribute?
			NewComponent<Camera>();
			NewComponent<Mesh>();
			NewComponent<Texture>();
			NewComponent<TextureCoords4>();
			NewComponent<SpriteIndex>();

			Processors.Start<Renderer>();
			Processors.Start<MeshManager>();
			Processors.Start<TextureManager>();
			Processors.Start<CameraController>();

			MainCamera = Entities.New();
			MainCamera.Set((Transform)Matrix4x4.Identity);
			MainCamera.Set(Camera.Default3D);
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
			using var stream = GetResourceStream(name);
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public byte[] GetResourceAsBytes(string name)
		{
			using var stream = GetResourceStream(name);
			using var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
	}
}
