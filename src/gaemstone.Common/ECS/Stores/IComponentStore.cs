using System;
using System.Diagnostics.CodeAnalysis;

namespace gaemstone.Common.ECS.Stores
{
	public interface IComponentStore
	{
		Type ComponentType { get; }
		int Count { get; }

		bool Has(uint entityID);
		bool TryGet(uint entityID, [NotNullWhen(true)] out object? value);
		bool TryAdd(uint entityID, object value);
		bool Set(uint entityID, object value, [NotNullWhen(true)] out object? previous);
		bool TryRemove(uint entityID, [NotNullWhen(true)] out object? previous);

		IEnumerator GetEnumerator();

		interface IEnumerator
		{
			object CurrentComponent { get; }
			uint CurrentEntityID { get; }
			bool MoveNext();
		}
	}

	public interface IComponentStore<T>
		: IComponentStore
	{
		bool TryGet(uint entityID, [NotNullWhen(true)] out T value);
		bool TryAdd(uint entityID, T value);
		bool Set(uint entityID, T value, [NotNullWhen(true)] out T previous);
		bool TryRemove(uint entityID, [NotNullWhen(true)] out T previous);

		bool IComponentStore.TryGet(uint entityID, [NotNullWhen(true)] out object? value)
			{ var found = TryGet(entityID, out var _value); value = _value; return found; }
		bool IComponentStore.TryAdd(uint entityID, object value) => TryAdd(entityID, (T)value);
		bool IComponentStore.Set(uint entityID, object value, [NotNullWhen(true)] out object? previous)
			{ var found = Set(entityID, (T)value, out var _prev); previous = _prev; return found; }
		bool IComponentStore.TryRemove(uint entityID, [NotNullWhen(true)] out object? previous)
			{ var found = TryRemove(entityID, out var _prev); previous = _prev; return found; }

		new IEnumerator GetEnumerator();

		new interface IEnumerator
			: IComponentStore.IEnumerator
		{
			new T CurrentComponent { get; }
		}
	}

	public interface IComponentRefStore<T>
		: IComponentStore<T>
		where T : struct
	{
		ref T TryGetRef(uint entityID);
		ref T GetOrCreateRef(uint entityID);

		new IEnumerator GetEnumerator();

		new interface IEnumerator
			: IComponentStore<T>.IEnumerator
		{
			new ref T CurrentComponent { get; }
		}
	}
}
