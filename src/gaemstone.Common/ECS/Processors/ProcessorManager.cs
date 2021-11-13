using System;
using System.Collections;
using System.Collections.Generic;

namespace gaemstone.Common.ECS.Processors
{
	public class ProcessorManager
		: IReadOnlyCollection<IProcessor>
	{
		private readonly Universe _universe;
		private readonly Dictionary<Type, IProcessor> _processors;

		public int Count => _processors.Count;

		public event Action<IProcessor>? ProcessorLoaded;
		public event Action<IProcessor>? ProcessorUnloaded;

		public ProcessorManager(Universe universe)
		{
			_universe   = universe;
			_processors = new Dictionary<Type, IProcessor>();
		}


		private void Start(Type type, IProcessor processor)
		{
			_processors.Add(type, processor);
			processor.OnLoad(_universe);
			ProcessorLoaded?.Invoke(processor);
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
			_processors.Remove(typeof(T));
			processor.OnUnload();
			ProcessorUnloaded?.Invoke(processor);
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
