using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using gaemstone.Common.Stores;
using gaemstone.Common.Utility;

namespace gaemstone.Common
{
	public class QueryManager
	{
		public delegate void QuerySpan<T>(Span<T> span);
		public delegate void QueryRef<T>(ref T element);

		readonly Universe _universe;

		public QueryManager(Universe universe)
			=> _universe = universe;

		public QueryBuilder New(string name)
			=> new(_universe, name);

		public void Run(Delegate action)
		{
			var name = ((action.Method?.IsSpecialName == false) ? action.Method?.Name : null)
				?? "Query" + string.Concat(action.GetMethodInfo().GetParameters().Select(p => p.ParameterType.Name));
			var builder = New(name);
			builder.Execute(action);
			var query = builder.Build();
			query.Run();
			// TODO: Cache by-delegate or by-signature?
		}
	}

	public interface IQuery
	{
		void Run();
	}

	public class QueryBuilder
	{
		readonly Universe _universe;
		Delegate? _action;
		ParamInfo[]? _components;

		readonly DynamicMethod _method;
		readonly ILGenerator _il;

		readonly LocalBuilder _storesLocal; // IComponentStore[]
		readonly LocalBuilder _sortedLocal; // int[]
		LocalBuilder? _actionLocal;
		LocalBuilder[]? _parameterLocals;

		readonly LocalBuilder _enumeratorLocal; // IComponentStore.IEnumerator
		readonly LocalBuilder _entityIDLocal;   // EcsId
		readonly Label _whileLabel;
		readonly Label _moveNextLabel;

		readonly LocalBuilder _indexLocal; // int
		readonly Label _enumerateStoresLabel;
		readonly Label _switchLabel;
		readonly Label _foundLabel;


		internal QueryBuilder(Universe universe, string name)
		{
			_universe = universe;

			_method = new DynamicMethod(name, null, new[]{ typeof(QueryImpl) });
			_il     = _method.GetILGenerator();

			_storesLocal = _il.DeclareLocal(typeof(IComponentStore).MakeArrayType());
			_sortedLocal = _il.DeclareLocal(typeof(int).MakeArrayType());

			_enumeratorLocal = _il.DeclareLocal(typeof(IComponentStore.IEnumerator));
			_entityIDLocal   = _il.DeclareLocal(typeof(EcsId));
			_whileLabel      = _il.DefineLabel();
			_moveNextLabel   = _il.DefineLabel();

			_indexLocal           = _il.DeclareLocal(typeof(int));
			_enumerateStoresLabel = _il.DefineLabel();
			_switchLabel          = _il.DefineLabel();
			_foundLabel           = _il.DefineLabel();
		}

		public QueryBuilder Execute(Delegate action)
		{
			if (_action != null) throw new InvalidOperationException(
				$"The method {nameof(Execute)} can only be called once on this {nameof(QueryBuilder)}");

			var components = action.GetMethodInfo().GetParameters().Select((p, index) => {
				var underlyingType = p.ParameterType;
				var kind = ParamKind.Normal;

				if (p.IsOut)                   throw new ArgumentException($"out is not supported\nParameter: {p}");
				if (p.ParameterType.IsArray)   throw new ArgumentException($"Arrays are not supported\nParameter: {p}");
				if (p.ParameterType.IsPointer) throw new ArgumentException($"Pointers are not supported\nParameter: {p}");

				if (p.IsOptional) kind = ParamKind.Optional;
				// TODO: Actually use default values if provided.

				if (p.IsNullable()) {
					if (kind == ParamKind.Optional) throw new ArgumentException(
						$"Nullable and Optional are not supported together\nParameter: {p}");
					if (p.ParameterType.IsValueType)
						underlyingType = Nullable.GetUnderlyingType(p.ParameterType)!;
					kind = ParamKind.Nullable;
				}

				if (p.ParameterType.IsByRef) {
					if (kind == ParamKind.Optional) throw new ArgumentException(
						$"ByRef and Optional are not supported together\nParameter: {p}");
					if (kind == ParamKind.Nullable) throw new ArgumentException(
						$"ByRef and Nullable are not supported together\nParameter: {p}");
					underlyingType = p.ParameterType.GetElementType()!;
					kind = p.IsIn ? ParamKind.In : ParamKind.Ref;
				}

				return new ParamInfo(index, kind, p.ParameterType, underlyingType);
			}).ToArray();

			if (!components.Any(c => c.Kind.IsRequired())) throw new ArgumentException(
				$"At least one parameter in {action} must be required");

			_action     = action;
			_components = components;

			return this;
		}

		void InitializeStoresLocal()
		{
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldfld, typeof(QueryImpl).GetField(nameof(QueryImpl.cachedStores))!);
			_il.Emit(OpCodes.Stloc, _storesLocal);
		}

		void SortIndicesIntoSortedLocal()
		{
			// var sorted = new int[_components.Length];
			_il.Emit(OpCodes.Ldc_I4, _components!.Length);
			_il.Emit(OpCodes.Newarr, typeof(int));
			_il.Emit(OpCodes.Stloc, _sortedLocal);

			// Sort components by whether they are required (not optional, Nullable<> or nullable reference)
			// and then initialize the local "sorted" array to the indices of the action's parameters.
			var sortedByRequired = _components.OrderByDescending(c => c.Kind.IsRequired()).ToArray();
			for (var i = 0; i < sortedByRequired.Length; i++) {
				_il.Emit(OpCodes.Ldloc, _sortedLocal);
				_il.Emit(OpCodes.Ldc_I4, i);
				_il.Emit(OpCodes.Ldc_I4, sortedByRequired[i].Index);
				_il.Emit(OpCodes.Stelem_I4);
			}

			var numRequired = _components.Count(c => c.Kind.IsRequired());
			// Array.Sort(sorted, 0, numRequired, new StoreCountParameter(stores));
			_il.Emit(OpCodes.Ldloc, _sortedLocal);
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Ldc_I4, numRequired);
			_il.Emit(OpCodes.Ldloc, _storesLocal);
			_il.Emit(OpCodes.Newobj, typeof(StoreCountComparer).GetConstructors()[0]);
			_il.Emit(OpCodes.Call, typeof(Array).GetMethod(nameof(Array.Sort), 1,
				new []{ Type.MakeGenericMethodParameter(0).MakeArrayType(), typeof(int), typeof(int),
						typeof(IComparer<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!
				.MakeGenericMethod(typeof(int)));
		}

		void DeclareParameterLocals()
			=> _parameterLocals = _components!.Select(c => _il.DeclareLocal(c.ParameterType)).ToArray();

		void DeclareAndInitializeActionLocal()
		{
			_actionLocal = _il.DeclareLocal(_action!.GetType());
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldfld, typeof(QueryImpl).GetField(nameof(QueryImpl.action))!);
			_il.Emit(OpCodes.Castclass, _action!.GetType());
			_il.Emit(OpCodes.Stloc, _actionLocal);
		}

		void EntityEnumerationLoop()
		{
			// var enumerator = store[0].GetEnumerator();
			_il.Emit(OpCodes.Ldloc, _storesLocal);
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Ldelem_Ref);
			_il.Emit(OpCodes.Callvirt, typeof(IComponentStore)
				.GetMethod(nameof(IComponentStore.GetEnumerator))!);
			_il.Emit(OpCodes.Stloc, _enumeratorLocal);
			// goto: if (enumerator.MoveNext())
			_il.Emit(OpCodes.Br, _moveNextLabel);

			_il.MarkLabel(_whileLabel);
			// while (true) {

				// entityID = enumerator.CurrentEntityID;
				_il.Emit(OpCodes.Ldloc, _enumeratorLocal);
				_il.Emit(OpCodes.Callvirt, typeof(IComponentStore.IEnumerator)
					.GetProperty(nameof(IComponentStore.IEnumerator.CurrentEntityID))!.GetMethod!);
				_il.Emit(OpCodes.Stloc, _entityIDLocal);

				_il.Emit(OpCodes.Br, _enumerateStoresLabel);

				// if (enumerator.MoveNext())
				_il.MarkLabel(_moveNextLabel);
				_il.Emit(OpCodes.Ldloc, _enumeratorLocal);
				_il.Emit(OpCodes.Callvirt, typeof(IComponentStore.IEnumerator)
					.GetMethod(nameof(IComponentStore.IEnumerator.MoveNext))!);
				// then => continue;
				_il.Emit(OpCodes.Brtrue, _whileLabel);
				// else => return;
				_il.Emit(OpCodes.Ret);

			// }
		}

		void StoreEnumerationLoop()
		{
			_il.MarkLabel(_enumerateStoresLabel);

			// var index = 0;
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Stloc, _indexLocal);

			// goto switch;
			_il.Emit(OpCodes.Br, _switchLabel);

			// for (var i = 1; i < _components.Length) {
				_il.MarkLabel(_foundLabel);

				// index++;
				_il.Emit(OpCodes.Ldloc, _indexLocal);
				_il.Emit(OpCodes.Ldc_I4_1);
				_il.Emit(OpCodes.Add);
				_il.Emit(OpCodes.Dup);
				_il.Emit(OpCodes.Stloc, _indexLocal);

				// if (index < _components.Length) goto switch;
				_il.Emit(OpCodes.Ldc_I4, _components!.Length);
				_il.Emit(OpCodes.Blt, _switchLabel);
			// }

			// Run the action specified in Execute().
			_il.Emit(OpCodes.Ldloc, _actionLocal!);
			for (var i = 0; i < _components!.Length; i++)
				_il.Emit(OpCodes.Ldloc, _parameterLocals![i]);
			_il.Emit(OpCodes.Callvirt, _action!.GetType().GetMethod("Invoke")!);

			// goto moveNext;
			_il.Emit(OpCodes.Br, _moveNextLabel);
		}

		void SwitchOnStore()
		{
			var caseLabels = _components!.Select(_ => _il.DefineLabel()).ToArray();

			_il.MarkLabel(_switchLabel);
			// switch (sorted[index]) {
			_il.Emit(OpCodes.Ldloc, _sortedLocal);
			_il.Emit(OpCodes.Ldloc, _indexLocal);
			_il.Emit(OpCodes.Ldelem_I4);
			_il.Emit(OpCodes.Switch, caseLabels);

			Type type;
			for (var i = 0; i < _components!.Length; i++) {
				// case i: ...
				_il.MarkLabel(caseLabels[i]);

				var notFirstLabel = _il.DefineLabel();
				// if (index == 0) {
				_il.Emit(OpCodes.Ldloc, _indexLocal);
				_il.Emit(OpCodes.Brtrue, notFirstLabel);
					// parameters[i] = enumerator.CurrentComponent;
					_il.Emit(OpCodes.Ldloc, _enumeratorLocal);

					type = _components[i].Kind.IsByRef()
						? typeof(IComponentRefStore<>.IEnumerator)
						: typeof(IComponentStore<>.IEnumerator);
					type = type.MakeGenericType(_components[i].UnderlyingType);
					_il.Emit(OpCodes.Castclass, type);
					_il.Emit(OpCodes.Callvirt, type.GetProperty("CurrentComponent")!.GetMethod!);

					_il.Emit(OpCodes.Stloc, _parameterLocals![i]);
					_il.Emit(OpCodes.Br, _foundLabel);
				// } else {
				_il.MarkLabel(notFirstLabel);
					// var store = stores[i];
					_il.Emit(OpCodes.Ldloc, _storesLocal);
					_il.Emit(OpCodes.Ldc_I4, i);
					_il.Emit(OpCodes.Ldelem_Ref);
					if (_components[i].Kind.IsByRef()) {
						// ref var value = stores[i].TryGetRef<T>(entityID);
						type = typeof(IComponentRefStore<>).MakeGenericType(_components[i].UnderlyingType);
						_il.Emit(OpCodes.Castclass, type);
						_il.Emit(OpCodes.Ldloc, _entityIDLocal);
						_il.Emit(OpCodes.Ldfld, typeof(EcsId).GetField(nameof(EcsId.ID))!);
						_il.Emit(OpCodes.Callvirt, type.GetMethod("TryGetRef")!);

						_il.Emit(OpCodes.Dup);
						// parameters[i] = value;
						_il.Emit(OpCodes.Stloc, _parameterLocals[i]);
						// if (value == null) goto moveNext;
						_il.Emit(OpCodes.Brfalse, _moveNextLabel);
						// else goto found;
						_il.Emit(OpCodes.Br, _foundLabel);

						// TODO: It's still possible for "in" parameters to be optional, how to handle these?
					} else {
						var valueLocal = _il.DeclareLocal(_components[i].UnderlyingType);

						// var success = stores[i].TryGet(entityID, out var value);
						type = typeof(IComponentStore<>).MakeGenericType(_components[i].UnderlyingType);
						_il.Emit(OpCodes.Castclass, type);
						_il.Emit(OpCodes.Ldloc, _entityIDLocal);
						_il.Emit(OpCodes.Ldfld, typeof(EcsId).GetField(nameof(EcsId.ID))!);
						_il.Emit(OpCodes.Ldloca, valueLocal);
						_il.Emit(OpCodes.Callvirt, type.GetMethod("TryGet")!);

						switch (_components[i].Kind) {
							case ParamKind.Optional:
							case ParamKind.Nullable:
								var notFoundLabel = _il.DefineLabel();
								_il.Emit(OpCodes.Brfalse, notFoundLabel);
								// if (success) {
									// parameters[i] = value;
									_il.Emit(OpCodes.Ldloc, valueLocal);
									if (_components[i].Kind == ParamKind.Nullable) {
										type = _components[i].UnderlyingType;
										_il.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(type).GetConstructor(new[]{ type })!);
									}
									_il.Emit(OpCodes.Stloc, _parameterLocals[i]);
									// goto found;
									_il.Emit(OpCodes.Br, _foundLabel);
								// } else {
									_il.MarkLabel(notFoundLabel);
									// TODO: Provide default value from delegate signature if available.
									_il.Emit(OpCodes.Ldloca, _parameterLocals[i]);
									_il.Emit(OpCodes.Initobj, _components[i].ParameterType);
									// goto found;
									_il.Emit(OpCodes.Br, _foundLabel);
								// }
								break;
							default:
								// if (!success) goto moveNext;
								_il.Emit(OpCodes.Brfalse, _moveNextLabel);
								// parameters[i] = value;
								_il.Emit(OpCodes.Ldloc, valueLocal);
								_il.Emit(OpCodes.Stloc, _parameterLocals[i]);
								// goto found;
								_il.Emit(OpCodes.Br, _foundLabel);
								break;
						}

					}
				// }
			}
			// }
		}

		public IQuery Build()
		{
			if (_action == null) throw new InvalidOperationException(
				$"The method {nameof(Execute)} must be called exactly once on this {nameof(QueryBuilder)} before calling {nameof(Build)}");

			InitializeStoresLocal();
			SortIndicesIntoSortedLocal();
			DeclareParameterLocals();
			DeclareAndInitializeActionLocal();

			EntityEnumerationLoop();
			StoreEnumerationLoop();
			SwitchOnStore();

			_il.Emit(OpCodes.Ret);

			var runAction = _method.CreateDelegate<Action<QueryImpl>>();
			return new QueryImpl(_universe, _components!.Select(c => c.UnderlyingType).ToArray(), runAction, _action);
		}

		class QueryImpl : IQuery
		{
			readonly Universe _universe;
			readonly Type[] _storesTypes;
			readonly Action<QueryImpl> _runAction;
			public Delegate action;
			public IComponentStore[]? cachedStores;

			public QueryImpl(Universe universe, Type[] storesTypes, Action<QueryImpl> runAction, Delegate action)
				{ _universe = universe; _storesTypes = storesTypes; _runAction = runAction; this.action = action; }

			public void Run()
			{
				// Cache the IComponentStore[] array if not done already.
				cachedStores ??= _storesTypes!.Select(type => _universe.Components.GetStore(type)).ToArray();
				// TODO: Invalidate this if the component stores change?
				_runAction.Invoke(this);
			}
		}


		class StoreCountComparer : IComparer<int>
		{
			readonly IComponentStore[] _stores;
			public StoreCountComparer(IComponentStore[] stores)
				=> _stores = stores;
			public int Compare(int x, int y)
				=> _stores[x].Count.CompareTo(_stores[y].Count);
		}
	}

	class ParamInfo
	{
		public int Index { get; }
		public ParamKind Kind { get; }
		public Type ParameterType { get; }
		public Type UnderlyingType { get; }

		public ParamInfo(int index, ParamKind kind, Type paramType, Type underlyingType)
			{ Index = index; Kind = kind; ParameterType = paramType; UnderlyingType = underlyingType; }
	}

	enum ParamKind
	{
		Normal,
		Optional,
		Nullable,
		In,
		Ref
	}

	static class ParamKindExtensions
	{
		public static bool IsRequired(this ParamKind kind)
			=> (kind != ParamKind.Optional) && (kind != ParamKind.Nullable);

		public static bool IsByRef(this ParamKind kind)
			=> (kind == ParamKind.In) || (kind == ParamKind.Ref);
	}
}
