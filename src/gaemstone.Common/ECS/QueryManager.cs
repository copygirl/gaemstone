using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using gaemstone.Common.ECS.Stores;

namespace gaemstone.Common.ECS
{
	public class QueryManager
	{
		internal static readonly ModuleBuilder MODULE_BUILDER
			= AssemblyBuilder
				.DefineDynamicAssembly(
					new AssemblyName("QueryImplementations"),
					AssemblyBuilderAccess.RunAndCollect)
				.DefineDynamicModule("QueryModule");

		private readonly Universe _universe;

		public QueryManager(Universe universe)
			=> _universe = universe;

		public void Run<T>(Action<T> action)
			=> QueryBuilder<T>.Run(_universe, action);


		public class QueryBuilder<T>
		{
			private static readonly Type QUERY_TYPE;
			private static readonly Type ENTITY_TYPE;

			static QueryBuilder()
			{
				var interfaceType = typeof(T);
				if (!interfaceType.IsInterface) throw new ArgumentException(
					$"{interfaceType} is not an interface");
				foreach (var member in interfaceType.GetMembers()) switch (member) {
					case PropertyInfo property: break;
					// Ignore property methods (that start with "get_" or "set_").
					case MethodInfo method when method.IsSpecialName &&
						(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")): break;
					// No other member types are are allowed.
					default: throw new ArgumentException(
						$"Interface {interfaceType} must only define properties");
				}

				var properties = interfaceType.GetProperties().Select(
					(prop, index) => new PropInfo(index, interfaceType, prop)).ToArray();
				if (!properties.Any(p => !p.IsNullable)) throw new ArgumentException(
					$"Interface {interfaceType} must define at least one non-nullable property");

				var baseTypeName = interfaceType.Name;
				if ((baseTypeName[0] == 'I') && Char.IsUpper(baseTypeName[1]))
					baseTypeName = baseTypeName.Substring(1);

				var queryType = QueryManager.MODULE_BUILDER.DefineType(
					baseTypeName + "Query", TypeAttributes.Public, null);
				var entityType = QueryManager.MODULE_BUILDER.DefineType(
					baseTypeName + "Entity",
					TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed,
					null, new []{ typeof(IEntityRef), interfaceType });

				var (universeField, storeFields) = BuildQueryConstructor(queryType, properties);
				var (entityCtor, queryField, entityField) = BuildEntityConstructor(
					entityType, properties, queryType, universeField);

				BuildQueryRunMethod(queryType, properties, entityType, universeField, storeFields, entityCtor);
				BuildEntityProperties(entityType, properties, queryType, storeFields, queryField, entityField);

				QUERY_TYPE  = queryType.CreateType();
				ENTITY_TYPE = entityType.CreateType();
			}

			public static void Run(Universe universe, Action<T> action)
			{
				var query = Activator.CreateInstance(QUERY_TYPE, universe);
				QUERY_TYPE.GetMethod("Run").Invoke(query, new []{ action });
			}

			private class PropInfo
			{
				public int Index { get; }
				public string Name => Property.Name;
				public PropertyInfo Property { get; }
				public Type Type => Property.PropertyType;

				public Type? UnderlyingType { get; }
				public Type StoredType => UnderlyingType ?? Type;
				public bool IsNullableStruct => (UnderlyingType != null);
				public bool IsNullableReference { get; }
				public bool IsNullable => (IsNullableStruct || IsNullableReference);

				public PropInfo(int index, Type interfaceType, PropertyInfo property)
				{
					Index = index;
					Property = property;
					UnderlyingType = Nullable.GetUnderlyingType(Type);
					IsNullableReference = IsNullable(interfaceType, property);
				}
			}

			private static (FieldBuilder, FieldBuilder[]) BuildQueryConstructor(
				TypeBuilder type, PropInfo[] properties)
			{
				var universeField = type.DefineField(
					nameof(Universe), typeof(Universe),
					FieldAttributes.Public | FieldAttributes.InitOnly);
				var storeFields = new FieldBuilder[properties.Length];

				var ctor = type.DefineConstructor(
					MethodAttributes.Public, CallingConventions.Standard,
					new []{ typeof(Universe) });
				var il = ctor.GetILGenerator();
				// base();
				il.Emit(OpCodes.Ldarg_0); // this
				il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
				// this.Universe = universe;
				il.Emit(OpCodes.Ldarg_0); // this
				il.Emit(OpCodes.Ldarg_1); // Universe
				il.Emit(OpCodes.Stfld, universeField);

				var getComponents = typeof(Universe).GetProperty(nameof(Universe.Components)).GetMethod;
				var getStoreBase  = typeof(ComponentManager).GetMethod(nameof(ComponentManager.GetStore), 1, Type.EmptyTypes);
				foreach (var property in properties) {
					// Define "TStore" property to hold the IComponentStore<T>.
					var storeField = type.DefineField(
						property.Name + "Store", typeof(IComponentStore<>).MakeGenericType(property.StoredType),
						FieldAttributes.Public | FieldAttributes.InitOnly);
					storeFields[property.Index] = storeField;

					// this.TStore = universe.Components.GetStore<T>();
					il.Emit(OpCodes.Ldarg_0); // this
					il.Emit(OpCodes.Ldarg_1); // Universe
					il.Emit(OpCodes.Callvirt, getComponents);
					il.Emit(OpCodes.Callvirt, getStoreBase.MakeGenericMethod(new[]{ property.StoredType }));
					il.Emit(OpCodes.Stfld, storeField);
				}

				// Finish up the constructor.
				il.Emit(OpCodes.Ret);

				return (universeField, storeFields);
			}

			private static void BuildQueryRunMethod(TypeBuilder type,
				PropInfo[] properties, TypeBuilder entityType,
				FieldBuilder universeField, FieldBuilder[] storeFields,
				ConstructorBuilder entityCtor)
			{
				// Only the required components matter here, so no nullables!
				var requiredProperties = properties.Where(p => !p.IsNullable).ToArray();

				var method = type.DefineMethod("Run", MethodAttributes.Public,
					null, new []{ typeof(Action<T>) });
				var il = method.GetILGenerator();

				var smallestStoreLocal = il.DeclareLocal(typeof(IComponentStore));
				var enumeratorLocal    = il.DeclareLocal(typeof(IComponentStore.Enumerator));
				var entityIDLocal      = il.DeclareLocal(typeof(uint));
				var entityNullable     = il.DeclareLocal(typeof(Entity?));

				// IComponentStore smallestStore = T0Store;
				il.Emit(OpCodes.Ldarg_0); // this
				il.Emit(OpCodes.Ldfld, storeFields[requiredProperties[0].Index]);
				il.Emit(OpCodes.Stloc_0); // => smallestStoreLocal

				var getCount = typeof(IComponentStore).GetProperty(nameof(IComponentStore.Count)).GetMethod;
				foreach (var property in requiredProperties.Skip(1)) {
					var doneLabel = il.DefineLabel();
					// if (TIStore.Count < smallestStore.Count)
					il.Emit(OpCodes.Ldarg_0); // this
					il.Emit(OpCodes.Ldfld, storeFields[property.Index]);
					il.Emit(OpCodes.Callvirt, getCount);
					il.Emit(OpCodes.Ldloc_0); // smallestStore
					il.Emit(OpCodes.Callvirt, getCount);
					il.Emit(OpCodes.Bge_S, doneLabel);
					// then => smallestStore = TIStore;
					il.Emit(OpCodes.Ldarg_0); // this
					il.Emit(OpCodes.Ldfld, storeFields[property.Index]);
					il.Emit(OpCodes.Stloc_0); // => smallestStore
					il.MarkLabel(doneLabel);
				}

				var whileLabel = il.DefineLabel();
				var testLabel  = il.DefineLabel();
				// var enumerator = smallestStore.GetEnumerator();
				il.Emit(OpCodes.Ldloc_0); // smallestStore
				il.Emit(OpCodes.Callvirt, typeof(IComponentStore)
					.GetMethod(nameof(IComponentStore.GetEnumerator)));
				il.Emit(OpCodes.Stloc_1); // => enumerator
				il.Emit(OpCodes.Br, testLabel);

				// loop
				il.MarkLabel(whileLabel);
				// entityID = enumerator.CurrentEntityID;
				il.Emit(OpCodes.Ldloc_1); // enumerator
				il.Emit(OpCodes.Callvirt, typeof(IComponentStore.Enumerator)
					.GetProperty(nameof(IComponentStore.Enumerator.CurrentEntityID)).GetMethod);
				il.Emit(OpCodes.Stloc_2); // => entityID

				foreach (var property in requiredProperties) {
					var doneLabel = il.DefineLabel();
					// if ((smallestStore != TIStore) && ...
					il.Emit(OpCodes.Ldloc_0); // smallestStore
					il.Emit(OpCodes.Ldarg_0); // this
					il.Emit(OpCodes.Ldfld, storeFields[property.Index]);
					il.Emit(OpCodes.Beq_S, doneLabel);
					// ... && !TIStore.Has(entityID))
					il.Emit(OpCodes.Ldarg_0); // this
					il.Emit(OpCodes.Ldfld, storeFields[property.Index]);
					il.Emit(OpCodes.Ldloc_2); // entityID
					il.Emit(OpCodes.Callvirt, typeof(IComponentStore)
						.GetMethod(nameof(IComponentStore.Has)));
					// then => continue;
					il.Emit(OpCodes.Brfalse, testLabel);
					il.MarkLabel(doneLabel);
				}

				// var entityNullable = Universe.Entities.GetByID(entityID);
				il.Emit(OpCodes.Ldarg_0); // this
				il.Emit(OpCodes.Ldfld, universeField);
				il.Emit(OpCodes.Callvirt, typeof(Universe).GetProperty(nameof(Universe.Entities)).GetMethod);
				il.Emit(OpCodes.Ldloc_2); // entityID
				il.Emit(OpCodes.Callvirt, typeof(EntityManager).GetMethod(nameof(EntityManager.GetByID)));
				il.Emit(OpCodes.Stloc_3); // => entityNullable

				// action(new QueriedEntity(this, entityNullable.Value));
				il.Emit(OpCodes.Ldarg_1); // action
				il.Emit(OpCodes.Ldarg_0); // this
				il.Emit(OpCodes.Ldloca_S, 3); // &entityNullable
				il.Emit(OpCodes.Call, typeof(Nullable<Entity>).GetProperty(nameof(Nullable<Entity>.Value)).GetMethod);
				il.Emit(OpCodes.Newobj, entityCtor);
				il.Emit(OpCodes.Callvirt, typeof(Action<T>).GetMethod(nameof(Action<T>.Invoke)));

				il.MarkLabel(testLabel);
				// if (enumerator.MoveNext())
				il.Emit(OpCodes.Ldloc_1); // enumerator
				il.Emit(OpCodes.Callvirt, typeof(IComponentStore.Enumerator)
					.GetMethod(nameof(IComponentStore.Enumerator.MoveNext)));
				// then => continue;
				il.Emit(OpCodes.Brtrue, whileLabel);
				// else => return;
				il.Emit(OpCodes.Ret);
			}

			private static (ConstructorBuilder, FieldBuilder, FieldBuilder) BuildEntityConstructor(
				TypeBuilder type, PropInfo[] properties,
				TypeBuilder queryType, FieldBuilder universeField)
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(IsReadOnlyAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
				var queryField = type.DefineField(
					"_query", queryType, FieldAttributes.InitOnly);
				var entityField = type.DefineField(
					"_entity", typeof(Entity), FieldAttributes.InitOnly);

				// Define "Entity" property that just returns the "_entity" backing field.
				var entityProp = type.DefineProperty(
					nameof(IEntityRef.Entity), default, typeof(Entity), Type.EmptyTypes);
				var entityPropGet = type.DefineMethod("get_Entity",
					MethodAttributes.Public | MethodAttributes.Virtual |
					MethodAttributes.SpecialName | MethodAttributes.HideBySig,
					typeof(Entity), Type.EmptyTypes);
				var entityPropGetIL = entityPropGet.GetILGenerator();
				entityPropGetIL.Emit(OpCodes.Ldarg_0); // this
				entityPropGetIL.Emit(OpCodes.Ldfld, entityField);
				entityPropGetIL.Emit(OpCodes.Ret);
				entityProp.SetGetMethod(entityPropGet);

				// Define "Universe" property that returns "_query.Universe".
				var universeProp = type.DefineProperty(
					nameof(IEntityRef.Universe), default, typeof(Universe), Type.EmptyTypes);
				var universePropGet = type.DefineMethod("get_Universe",
					MethodAttributes.Public | MethodAttributes.Virtual |
					MethodAttributes.SpecialName | MethodAttributes.HideBySig,
					typeof(Universe), Type.EmptyTypes);
				var universePropGetIL = universePropGet.GetILGenerator();
				universePropGetIL.Emit(OpCodes.Ldarg_0); // this
				universePropGetIL.Emit(OpCodes.Ldfld, queryField);
				universePropGetIL.Emit(OpCodes.Ldfld, universeField);
				universePropGetIL.Emit(OpCodes.Ret);
				universeProp.SetGetMethod(universePropGet);

				var ctor = type.DefineConstructor(
					MethodAttributes.Public, CallingConventions.Standard,
					new[]{ queryType, typeof(Entity) });
				var ctorIL = ctor.GetILGenerator();
				ctorIL.Emit(OpCodes.Ldarg_0); // this
				ctorIL.Emit(OpCodes.Ldarg_1); // Query
				ctorIL.Emit(OpCodes.Stfld, queryField);
				ctorIL.Emit(OpCodes.Ldarg_0); // this
				ctorIL.Emit(OpCodes.Ldarg_2); // Entity
				ctorIL.Emit(OpCodes.Stfld, entityField);
				ctorIL.Emit(OpCodes.Ret);

				return (ctor, queryField, entityField);
			}

			private static void BuildEntityProperties(TypeBuilder type,
				PropInfo[] properties, TypeBuilder queryType, FieldBuilder[] storeFields,
				FieldBuilder queryField, FieldBuilder entityField)
			{
				var getEntityID = typeof(Entity).GetProperty(nameof(Entity.ID)).GetMethod;
				var storeRemove = typeof(IComponentStore).GetMethod(nameof(IComponentStore.Remove));

				var propertyMethodAttributes
					= MethodAttributes.Public | MethodAttributes.Final |
					  MethodAttributes.NewSlot | MethodAttributes.Virtual |
					  MethodAttributes.SpecialName | MethodAttributes.HideBySig;
				foreach (var property in properties) {
					var prop = type.DefineProperty(
						property.Name, default, property.Type, Type.EmptyTypes);

					var storeType   = typeof(IComponentStore<>).MakeGenericType(property.StoredType);
					var storeGet    = storeType.GetMethod(nameof(IComponentStore<object>.Get));
					var storeTryGet = storeType.GetMethod(nameof(IComponentStore<object>.TryGet));

					var getMethod = type.DefineMethod("get_" + property.Name,
						propertyMethodAttributes, property.Type, Type.EmptyTypes);
					var getIL = getMethod.GetILGenerator();
					// Prepare call to _query.TStore.<method>(_entity.ID, ...)
					getIL.Emit(OpCodes.Ldarg_0); // this
					getIL.Emit(OpCodes.Ldfld, queryField);
					getIL.Emit(OpCodes.Ldfld, storeFields[property.Index]);
					getIL.Emit(OpCodes.Ldarg_0); // this
					getIL.Emit(OpCodes.Ldflda, entityField);
					getIL.Emit(OpCodes.Call, getEntityID);
					if (property.IsNullableStruct) {
						var valueLocal    = getIL.DeclareLocal(property.StoredType);
						var nullableLocal = getIL.DeclareLocal(property.Type);
						var elseLabel     = getIL.DefineLabel();
						// if (_query.TStore.TryGet(_entity.ID, out var value))
						getIL.Emit(OpCodes.Ldloca_S, valueLocal);
						getIL.Emit(OpCodes.Callvirt, storeTryGet);
						getIL.Emit(OpCodes.Brfalse_S, elseLabel);
						// then => return new Nullable<T>(value);
						getIL.Emit(OpCodes.Ldloc_0); // value
						getIL.Emit(OpCodes.Newobj, property.Type.GetConstructor(new[]{ property.StoredType }));
						getIL.Emit(OpCodes.Ret);
						// else => return default(Nullable<T>);
						getIL.MarkLabel(elseLabel);
						getIL.Emit(OpCodes.Ldloca_S, nullableLocal);
						getIL.Emit(OpCodes.Initobj, property.Type);
						getIL.Emit(OpCodes.Ldloc_1);
						getIL.Emit(OpCodes.Ret);
					} else if (property.IsNullableReference) {
						var valueLocal = getIL.DeclareLocal(property.Type);
						// _query.TStore.TryGet(_entity.ID, out var value);
						getIL.Emit(OpCodes.Ldloca_S, valueLocal);
						getIL.Emit(OpCodes.Callvirt, storeTryGet);
						// return value;
						getIL.Emit(OpCodes.Ldloc_0);
						getIL.Emit(OpCodes.Ret);
					} else {
						// return _query.TStore.Get(_entity.ID);
						getIL.Emit(OpCodes.Callvirt, storeGet);
						getIL.Emit(OpCodes.Ret);
					}
					prop.SetGetMethod(getMethod);

					if (property.Property.CanWrite) {
						var setMethod = type.DefineMethod("set_" + property.Name,
							propertyMethodAttributes, null, new []{ property.Type });
						var setIL = setMethod.GetILGenerator();
						// Prepare call to _query.TStore.<method>(_entity.ID, ...)
						setIL.Emit(OpCodes.Ldarg_0); // this
						setIL.Emit(OpCodes.Ldfld, queryField);
						setIL.Emit(OpCodes.Ldfld, storeFields[property.Index]);
						setIL.Emit(OpCodes.Ldarg_0); // this
						setIL.Emit(OpCodes.Ldflda, entityField);
						setIL.Emit(OpCodes.Call, getEntityID);
						if (property.IsNullable) {
							var doneLabel = setIL.DefineLabel();
							// if (value == null)
							if (property.IsNullableStruct) {
								setIL.Emit(OpCodes.Ldarga_S, 1); // &value
								setIL.Emit(OpCodes.Call, property.Type.GetProperty(
									nameof(Nullable<bool>.HasValue)).GetMethod);
							} else {
								setIL.Emit(OpCodes.Ldarg_1); // value
							}
							setIL.Emit(OpCodes.Brtrue_S, doneLabel);
							// then => _query.TStore.Remove(_entity.ID);
							//         return;
							setIL.Emit(OpCodes.Callvirt, storeRemove);
							setIL.Emit(OpCodes.Ret);
							// else => continue..
							setIL.MarkLabel(doneLabel);
						}

						if (property.IsNullableStruct) {
							setIL.Emit(OpCodes.Ldarga_S, 1); // &value
							setIL.Emit(OpCodes.Call, property.Type.GetProperty(
								nameof(Nullable<bool>.Value)).GetMethod);
						} else {
							setIL.Emit(OpCodes.Ldarg_1); // value
						}
						// _query.TStore.Set(_entity.ID, value);
						setIL.Emit(OpCodes.Callvirt, storeType.GetMethod(nameof(IComponentStore<object>.Set)));
						setIL.Emit(OpCodes.Ret);

						prop.SetSetMethod(setMethod);
					}
				}
			}
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
	}
}
