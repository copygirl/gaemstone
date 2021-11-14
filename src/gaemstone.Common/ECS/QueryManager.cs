using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using gaemstone.Common.ECS.Stores;
using gaemstone.Common.Utility;

namespace gaemstone.Common.ECS
{
	public class QueryManager
	{
		public delegate void QuerySpan<T>(Span<T> span);
		public delegate void QueryRef<T>(ref T element);

		readonly Universe _universe;

		public QueryManager(Universe universe)
			=> _universe = universe;


		public void Run<T>(QuerySpan<T> action) where T : struct
			=> QueryBuilder<T>.Run(_universe, action);

		public void Run<T>(QueryRef<T> action) where T : struct
			=> Run((Span<T> span) => { for (int i = 0; i < span.Length; i++) action(ref span[i]); });


		public static class QueryBuilder<T>
			where T : struct
		{
			// Lazily initialized to contain information about the fields of type T.
			static IFieldWrapper[]? _fields;

			public static void Run(Universe universe, QuerySpan<T> action)
			{
				if (_fields == null) {
					_fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.Select(field => IFieldWrapper.Create(universe, field)).ToArray();
					if (_fields.All(wrapper => wrapper.IsOptional)) throw new Exception(
						$"At least one component in {typeof(T)} must be required (non-nullable)");
				}

				// Sort fields by required-first, then small-count-first.
				// This way we get the least amount of iterations.
				var sorted    = _fields.OrderBy(f => f.IsOptional).ThenBy(w => w.Store.Count).ToArray();
				var entities  = sorted[0].Store.GetEnumerator();
				var entityIDs = new List<uint>();
				var elements  = new List<T>();
				while (entities.MoveNext()) {
					var element  = new T();
					var entityID = entities.CurrentEntityID;
					sorted[0].Set(ref element, entities.CurrentComponent);
					foreach (var nextField in sorted.Skip(1)) {
						if (nextField.Store.TryGet(entityID, out var value))
							nextField.Set(ref element, value);
						else if (!nextField.IsOptional) goto skip;
					}
					entityIDs.Add(entityID);
					elements.Add(element);
					skip: {  }
				}

				// Actually run the query action.
				var elementsAsSpan = CollectionsMarshal.AsSpan(elements);
				action(elementsAsSpan);

				// TODO: Needed when we're going over AppDomain boundaries.
				// MemoryMarshal.AsBytes(...);
				// We also need to send the modified buffer back the other way.

				// Any fields that are settable (not readonly) need to be put back into the store.
				foreach (var field in _fields.Where(f => f.IsSettable))
					for (var i = 0; i < entityIDs.Count; i++)
						field.Write(entityIDs[i], in elementsAsSpan[i]);
			}

			public interface IFieldWrapper
			{
				bool IsSettable { get; }
				bool IsOptional { get; }
				IComponentStore Store { get; }

				static IFieldWrapper Create(Universe universe, FieldInfo field)
				{
					var isOptional = field.IsNullable();
					var underlyingType = (isOptional && field.FieldType.IsValueType)
						? Nullable.GetUnderlyingType(field.FieldType)! : field.FieldType;
					var type = typeof(FieldWrapper<>).MakeGenericType(typeof(T), underlyingType);
					return (IFieldWrapper)Activator.CreateInstance(type, universe, field, isOptional)!;
				}

				void Set(ref T element, object value);
				void Write(uint entityID, in T element);
			}

			public class FieldWrapper<TValue> : IFieldWrapper
			{
				public delegate TValue GetAction(in T element);
				public delegate void SetAction(ref T element, TValue value);

				public bool IsSettable { get; }
				public bool IsOptional { get; }
				public IComponentStore<TValue> Store { get; }
				public GetAction? GeneratedGetAction { get; }
				public SetAction GeneratedSetAction { get; }

				IComponentStore QueryBuilder<T>.IFieldWrapper.Store => Store;

				public FieldWrapper(Universe universe, FieldInfo field, bool isOptional)
				{
					IsSettable = !field.IsInitOnly;
					IsOptional = isOptional;
					if (IsSettable && isOptional) throw new Exception(
						$"{field.Name} in query type {typeof(T)} can't be both settable and optional.");
					Store = universe.Components.GetStore<TValue>();

					if (IsSettable) {
						var getMethod = new DynamicMethod(
							$"Get{typeof(T).Name}{field.Name}",
							typeof(TValue), new []{ typeof(T).MakeByRefType() },
							typeof(T).Module, true);
						var getIL = getMethod.GetILGenerator();
						getIL.Emit(OpCodes.Ldarg_0);
						getIL.Emit(OpCodes.Ldfld, field);
						getIL.Emit(OpCodes.Ret);
						GeneratedGetAction = getMethod.CreateDelegate<GetAction>();
					}

					var setMethod = new DynamicMethod(
						$"Set{typeof(T).Name}{field.Name}",
						null, new []{ typeof(T).MakeByRefType(), typeof(TValue) },
						typeof(T).Module, true);
					var setIL = setMethod.GetILGenerator();
					setIL.Emit(OpCodes.Ldarg_0);
					setIL.Emit(OpCodes.Ldarg_1);
					if (IsOptional && typeof(TValue).IsValueType)
						setIL.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(typeof(TValue)).GetConstructor(new []{ typeof(TValue) })!);
					setIL.Emit(OpCodes.Stfld, field);
					setIL.Emit(OpCodes.Ret);
					GeneratedSetAction = setMethod.CreateDelegate<SetAction>();
				}

				public void Set(ref T element, object value)
					=> GeneratedSetAction(ref element, (TValue)value);

				public void Write(uint entityID, in T element)
					=> Store.Set(entityID, GeneratedGetAction!(element));
			}
		}
	}
}
