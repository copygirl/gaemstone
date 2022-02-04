using System;
using System.Collections;
using System.Collections.Generic;
using gaemstone.Common.Utility;

namespace gaemstone.ECS
{
	public class ProcessorManager
		: IReadOnlyCollection<IProcessor>
	{
		readonly Dictionary<Type, IProcessor> _processors = new();

		public int Count => _processors.Count;

		public event Action<IProcessor>? ProcessorLoadedPre;
		public event Action<IProcessor>? ProcessorLoadedPost;
		public event Action<IProcessor>? ProcessorUnloadedPre;
		public event Action<IProcessor>? ProcessorUnloadedPost;

		internal ProcessorManager(Universe universe)
		{
			ProcessorLoadedPre += processor => {
				var property = processor.GetType().GetProperty(nameof(Universe));
				if (property?.PropertyType == typeof(Universe))
					TypeWrapper.For(processor.GetType()).GetFieldForAutoProperty(property)
						.ClassSetter.Invoke(processor, universe);
			};

			ProcessorUnloadedPost += processor => {
				var property = processor.GetType().GetProperty(nameof(Universe));
				if (property?.PropertyType == typeof(Universe))
					TypeWrapper.For(processor.GetType()).GetFieldForAutoProperty(property)
						.ClassSetter.Invoke(processor, null);
			};
		}


		void Start(Type type, IProcessor processor)
		{
			ProcessorLoadedPre?.Invoke(processor);
			_processors.Add(type, processor);
			processor.OnLoad();
			ProcessorLoadedPost?.Invoke(processor);
		}
		// public T Start<T>(Type type)
		// 	where T : IProcessor
		// {
		// 	if (type.IsAbstract || type.IsInterface) throw new ArgumentException(
		// 		$"Type {type} is not a concrete type that can be instanciated");
		// 	if (type.GetConstructor(Type.EmptyTypes) == null) throw new ArgumentException(
		// 		$"Type {type} does not have a parameterless constructor");

		// 	var processor = new T();
		// 	Start(typeof(T), processor);
		// 	return processor;
		// }
		public T Start<T>()
			where T : IProcessor, new()
		{
			var processor = new T();
			Start(typeof(T), processor);
			return processor;
		}

		public void Stop<T>()
			where T : IProcessor
		{
			var processor = GetOrThrow<T>();
			ProcessorUnloadedPre?.Invoke(processor);
			_processors.Remove(typeof(T));
			processor.OnUnload();
			ProcessorUnloadedPost?.Invoke(processor);
		}


		public T? GetOrNull<T>()
				where T : class, IProcessor
			=> _processors.TryGetValue(typeof(T), out var processor) ? (T)processor : null;

		public T GetOrThrow<T>()
				where T : IProcessor
			=> _processors.TryGetValue(typeof(T), out var processor)
				? (T)processor : throw new KeyNotFoundException(
					$"Processor of type {typeof(T)} not found");


		public IEnumerator<IProcessor> GetEnumerator()
			=> _processors.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
}
