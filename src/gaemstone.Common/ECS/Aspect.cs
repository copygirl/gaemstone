using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using gaemstone.Common.ECS.Stores;

namespace gaemstone.Common.ECS
{
	public abstract class AspectBase
		: IEntityRef
	{
		protected readonly List<IComponentStore> _stores
			= new List<IComponentStore>();

		public Universe Universe { get; }
		public Entity Entity { get; protected set; }

		public AspectBase(Universe universe)
			=> Universe = universe;

		internal void ForEach<T>(Action<T> action)
		{
			if (_stores.Count == 0) return;
			var aspect = (T)(object)this;
			// FIXME: Sorting here doesn't work if some of the components are optional.
			// _stores.Sort((a, b) => (a.Count - b.Count));
			// Enumerate the store with the fewest components.
			var enumerator = _stores[0].GetEnumerator();
			skip: while (enumerator.MoveNext()) {
				var entityID = enumerator.CurrentEntityID;
				Process(0, entityID);
				// Next, ensure other required components are in the other stores.
				// If not, skip to the next entity in the first (smallest) store.
				for (var i = 1; i < _stores.Count; i++)
					// Process both checks if a component exists
					// and sets the property associated with it.
					if (!Process(i, entityID))
						goto skip;
				// If all components exist, set the entity and invoke the action.
				Entity = Universe.Entities.GetByID(entityID)!.Value;
				action(aspect);
			}
			Entity = Entity.None;
		}

		public abstract bool Process(int storeIndex, uint entityID);
	}

	internal static class AspectHelper
	{
		internal static readonly ModuleBuilder MODULE_BUILDER
			= AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("AspectImplementations"),
				AssemblyBuilderAccess.RunAndCollect)
			.DefineDynamicModule("AspectModule");
	}

	public static class Aspect<T>
	{
		static readonly Func<Universe, AspectBase> CREATE_FUNC;
		static Aspect()
		{
			var interfaceType = typeof(T);
			if (!interfaceType.IsInterface) throw new ArgumentException(
				$"The specified generic type is not an interface");
			foreach (var member in interfaceType.GetMembers()) switch (member) {
				case PropertyInfo property: break;
				// Ignore property methods (that start with "get_" or "set_").
				case MethodInfo method when method.IsSpecialName &&
					(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")): break;
				// No other member types are are allowed.
				default: throw new ArgumentException(
					$"Interface {interfaceType} must only define properties");
			}

			var typeName = interfaceType.Name + "Impl";
			if ((typeName[0] == 'I') && Char.IsUpper(typeName[1]))
				typeName = typeName.Substring(1);

			var properties  = interfaceType.GetProperties();
			var typeBuilder = AspectHelper.MODULE_BUILDER.DefineType(
				typeName, default, typeof(AspectBase), new[]{ interfaceType });

			// Define constructor that calls the AspectBase constructor.
			var baseCtor = typeof(AspectBase).GetConstructor(
				new[]{ typeof(Universe) });
			var ctorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public, CallingConventions.Standard,
				new[]{ typeof(Universe) });
			var ctorIL = ctorBuilder.GetILGenerator();
			ctorIL.Emit(OpCodes.Ldarg_0); // this
			ctorIL.Emit(OpCodes.Ldarg_1); // Universe
			ctorIL.Emit(OpCodes.Call, baseCtor);

			// Define override for Process method.
			var processBuilder = typeBuilder.DefineMethod(nameof(AspectBase.Process),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				typeof(bool), new[]{ typeof(int), typeof(uint) });
			var processIL  = processBuilder.GetILGenerator();
			var breakLabel = processIL.DefineLabel();
			var caseLabels = Enumerable.Range(0, properties.Length)
			                                 .Select(i => processIL.DefineLabel()).ToArray();
			processIL.Emit(OpCodes.Ldarg_1); // storeIndex
			processIL.Emit(OpCodes.Switch, caseLabels);
			processIL.Emit(OpCodes.Br, breakLabel);

			var storesField   = typeof(AspectBase).GetField("_stores", BindingFlags.Instance | BindingFlags.NonPublic);
			var getEntity     = typeof(AspectBase).GetProperty(nameof(AspectBase.Entity)).GetMethod;
			var getComponents = typeof(Universe).GetProperty(nameof(Universe.Components)).GetMethod;
			var getStoreBase  = typeof(ComponentManager).GetMethod(nameof(ComponentManager.GetStore), 1, Type.EmptyTypes);
			var listAdd       = typeof(List<IComponentStore>).GetMethod(nameof(List<bool>.Add), new[]{ typeof(IComponentStore) });

			for (var propIndex = 0; propIndex < properties.Length; propIndex++) {
				var property  = properties[propIndex];
				var propType  = property.PropertyType;
				var fieldName = "_" + Char.ToLowerInvariant(property.Name[0])
				                    + property.Name.Substring(1);

				var nullableUnderlyingType = Nullable.GetUnderlyingType(propType);
				var storedType = nullableUnderlyingType ?? propType;
				var isNullableStruct    = (nullableUnderlyingType != null);
				var isNullableReference = IsNullable(interfaceType, property);

				// Define store field holding the IComponentStore<storedType>.
				var storeFieldType = typeof(IComponentStore<>).MakeGenericType(storedType);
				var storeFieldAttr = FieldAttributes.Private
				                   | FieldAttributes.InitOnly;
				var storeField = typeBuilder.DefineField(
					fieldName + "Store", storeFieldType, storeFieldAttr);

				// Initialize store field in constructor.
				var getStoreT = getStoreBase.MakeGenericMethod(new[]{ storedType });
				// TODO: Do this without a local?
				var storeLocal = ctorIL.DeclareLocal(storeFieldType);
				// _stores.Add(_someComponentStore = universe.Components.GetStore<SomeComponent>());
				ctorIL.Emit(OpCodes.Ldarg_0); // this
				ctorIL.Emit(OpCodes.Ldfld, storesField);
				ctorIL.Emit(OpCodes.Ldarg_0); // this
				ctorIL.Emit(OpCodes.Ldarg_1); // Universe
				ctorIL.Emit(OpCodes.Callvirt, getComponents);
				ctorIL.Emit(OpCodes.Callvirt, getStoreT);
				ctorIL.Emit(OpCodes.Dup);
				ctorIL.Emit(OpCodes.Stloc_S, storeLocal);
				ctorIL.Emit(OpCodes.Stfld, storeField);
				ctorIL.Emit(OpCodes.Ldloc_S, storeLocal);
				ctorIL.Emit(OpCodes.Callvirt, listAdd);

				// Define "backing" field for the generated property.
				var backingField = typeBuilder.DefineField(
					fieldName, propType, FieldAttributes.Private);

				// Define the property that implements the interface's
				// property with a getter and maybe setter as well.
				var propBuilder = typeBuilder.DefineProperty(
					property.Name, default, propType, Type.EmptyTypes);
				var propAttr = MethodAttributes.Public
				             | MethodAttributes.SpecialName
				             | MethodAttributes.HideBySig
				             | MethodAttributes.Virtual;

				// Define property getter method to return "backing" field value.
				var propGetBuilder = typeBuilder.DefineMethod(
					"get_" + property.Name, propAttr, propType, Type.EmptyTypes);
				var propGetIL = propGetBuilder.GetILGenerator();
				propGetIL.Emit(OpCodes.Ldarg_0); // this
				propGetIL.Emit(OpCodes.Ldfld, backingField);
				propGetIL.Emit(OpCodes.Ret);
				propBuilder.SetGetMethod(propGetBuilder);

				// Test if property from interface has setter.
				if (property.CanWrite) {
					var setComponent    = storeFieldType.GetMethod(nameof(IComponentStore<bool>.Set));
					var removeComponent = storeFieldType.GetMethod(nameof(IComponentStore<bool>.Remove));

					// Define property setter to call IComponentStore's Set method.
					var propSetBuilder = typeBuilder.DefineMethod(
						"set_" + property.Name, propAttr, null, new[]{ propType });
					var propSetIL = propSetBuilder.GetILGenerator();

					var falseLabel = default(Label);
					if (isNullableStruct || isNullableReference) {
						// if (value != null) {
						falseLabel = propSetIL.DefineLabel();
						if (isNullableStruct) {
							propSetIL.Emit(OpCodes.Ldarga_S, 1); // &value
							propSetIL.Emit(OpCodes.Call, propType.GetProperty(
								nameof(Nullable<bool>.HasValue)).GetMethod);
						} else {
							propSetIL.Emit(OpCodes.Ldarg_1); // value
						}
						propSetIL.Emit(OpCodes.Brfalse_S, falseLabel);
					}

					// _someComponentStore.Set(this.Entity, value);
					propSetIL.Emit(OpCodes.Ldarg_0); // this
					propSetIL.Emit(OpCodes.Ldfld, storeField);
					propSetIL.Emit(OpCodes.Ldarg_0); // this
					propSetIL.Emit(OpCodes.Call, getEntity);
					if (isNullableStruct) {
						propSetIL.Emit(OpCodes.Ldarga_S, 1); // &value
						propSetIL.Emit(OpCodes.Call, propType.GetProperty(
							nameof(Nullable<bool>.Value)).GetMethod);
					} else {
						propSetIL.Emit(OpCodes.Ldarg_1); // value
					}
					propSetIL.Emit(OpCodes.Callvirt, setComponent);
					propSetIL.Emit(OpCodes.Ret);
					propBuilder.SetSetMethod(propSetBuilder);

					// } else _someComponentStore.Remove(this.Entity);
					if (isNullableStruct || isNullableReference) {
						propSetIL.MarkLabel(falseLabel);
						propSetIL.Emit(OpCodes.Ldarg_0); // this
						propSetIL.Emit(OpCodes.Ldfld, storeField);
						propSetIL.Emit(OpCodes.Ldarg_0); // this
						propSetIL.Emit(OpCodes.Call, getEntity);
						propSetIL.Emit(OpCodes.Callvirt, removeComponent);
						propSetIL.Emit(OpCodes.Ret);
					}
				}

				// Add "case statement" to Process method.
				var tryGetMethod = storeFieldType.GetMethod(
					nameof(IComponentStore<bool>.TryGet));
				processIL.MarkLabel(caseLabels[propIndex]);
				if (isNullableStruct) {
					var valueLocal    = processIL.DeclareLocal(storedType);
					var nullableLocal = processIL.DeclareLocal(propType);
					var foundLabel = processIL.DefineLabel();
					var doneLabel  = processIL.DefineLabel();
					// if (!TryGet(entityID, out value))
					processIL.Emit(OpCodes.Ldarg_0); // this
					processIL.Emit(OpCodes.Ldarg_0); // this
					processIL.Emit(OpCodes.Ldfld, storeField);
					processIL.Emit(OpCodes.Ldarg_2); // entityID
					processIL.Emit(OpCodes.Ldloca_S, valueLocal);
					processIL.Emit(OpCodes.Callvirt, tryGetMethod);
					processIL.Emit(OpCodes.Brtrue, foundLabel);
					// then => nullable = null;
					processIL.Emit(OpCodes.Ldloca_S, nullableLocal);
					processIL.Emit(OpCodes.Initobj, propType);
					processIL.Emit(OpCodes.Ldloc_S, nullableLocal);
					processIL.Emit(OpCodes.Br, doneLabel);
					// else => nullable = value;
					processIL.MarkLabel(foundLabel);
					processIL.Emit(OpCodes.Ldloc_S, valueLocal);
					processIL.Emit(OpCodes.Newobj, propType.GetConstructor(new[]{ storedType }));
					// _someComponent = nullable;
					processIL.MarkLabel(doneLabel);
					processIL.Emit(OpCodes.Stfld, backingField);
					processIL.Emit(OpCodes.Ldc_I4_1); // return true
					processIL.Emit(OpCodes.Ret);
				} else {
					processIL.Emit(OpCodes.Ldarg_0); // this
					processIL.Emit(OpCodes.Ldfld, storeField);
					processIL.Emit(OpCodes.Ldarg_2); // entityID
					processIL.Emit(OpCodes.Ldarg_0); // this
					processIL.Emit(OpCodes.Ldflda, backingField);
					processIL.Emit(OpCodes.Callvirt, tryGetMethod);
					if (isNullableReference) {
						processIL.Emit(OpCodes.Pop);      // Ignore TryGet return value..
						processIL.Emit(OpCodes.Ldc_I4_1); // ..and always return true.
					} // Otherwise return result of TryGet.
					processIL.Emit(OpCodes.Ret);
				}
			}

			// Finish up constructor.
			ctorIL.Emit(OpCodes.Ret);

			// Finish up Process method.
			processIL.MarkLabel(breakLabel);
			processIL.Emit(OpCodes.Ldc_I4_0);
			processIL.Emit(OpCodes.Ret);

			var type = typeBuilder.CreateType();

			// Create a function delegate to construct an Aspect of this type.
			var ctor = type.GetConstructor(new[]{ typeof(Universe) });
			var create = new DynamicMethod("Create" + typeName,
				typeof(AspectBase), new[]{ typeof(Universe) },
				AspectHelper.MODULE_BUILDER);
			var createIL = create.GetILGenerator();
			createIL.Emit(OpCodes.Ldarg_0); // Universe
			createIL.Emit(OpCodes.Newobj, ctor);
			createIL.Emit(OpCodes.Ret);
			CREATE_FUNC = (Func<Universe, AspectBase>)create.CreateDelegate(
				typeof(Func<Universe, AspectBase>));
		}

		// https://stackoverflow.com/a/58454489
		public static bool IsNullable(Type enclosingType, PropertyInfo property)
		{
			if (!enclosingType.GetProperties(
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
					BindingFlags.NonPublic | BindingFlags.DeclaredOnly
				).Contains(property)) throw new ArgumentException(
					"enclosingType must be the type which defines property");

			var nullable = property.CustomAttributes.FirstOrDefault(
				x => (x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute"));
			if ((nullable != null) && (nullable.ConstructorArguments.Count == 1)) {
				var attributeArgument = nullable.ConstructorArguments[0];
				if (attributeArgument.ArgumentType == typeof(byte[])) {
					var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value;
					if ((args.Count > 0) && (args[0].ArgumentType == typeof(byte)))
						return (byte)args[0].Value == 2;
				} else if (attributeArgument.ArgumentType == typeof(byte))
					return (byte)attributeArgument.Value == 2;
			}

			var context = enclosingType.CustomAttributes.FirstOrDefault(
				x => (x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute"));
			if ((context != null) && (context.ConstructorArguments.Count == 1) &&
			    (context.ConstructorArguments[0].ArgumentType == typeof(byte)))
				return (byte)context.ConstructorArguments[0].Value == 2;

			// Couldn't find a suitable attribute.
			return false;
		}

		public static void ForEach(Universe universe, Action<T> action)
			=> CREATE_FUNC(universe).ForEach(action);
	}
}
