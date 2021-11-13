using System;
using System.Diagnostics.CodeAnalysis;

namespace gaemstone.Common.ECS.Stores
{
	public interface IComponentStore
	{
		Type ComponentType { get; }
		int Count { get; }

		event ComponentAddedHandler? ComponentAdded;
		event ComponentRemovedHandler? ComponentRemoved;

		bool TryGet(uint entityID, out object value);
		bool Has(uint entityID);
		bool Remove(uint entityID);

		Enumerator GetEnumerator();

		interface Enumerator
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
		void Set(uint entityID, T value);

		bool IComponentStore.TryGet(uint entityID, [NotNullWhen(true)] out object value)
		{
			var result = TryGet(entityID, out var _value);
			value = _value!;
			return result;
		}

		new Enumerator GetEnumerator();

		new interface Enumerator
			: IComponentStore.Enumerator
		{
			new T CurrentComponent { get; }
		}
	}

	public interface IComponentRefStore<T>
			: IComponentStore<T>
		where T : struct
	{
		ref T GetRef(uint entityID);

		new Enumerator GetEnumerator();

		new interface Enumerator
			: IComponentStore<T>.Enumerator
		{
			new ref T CurrentComponent { get; }
		}
	}

	public delegate void ComponentAddedHandler(uint entityID);
	public delegate void ComponentRemovedHandler(uint entityID);
	public delegate void ComponentChangedHandler<T>(uint entityID, ref T oldValue, ref T newValue);
}
