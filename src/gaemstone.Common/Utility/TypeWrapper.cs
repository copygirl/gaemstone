using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace gaemstone.Common.Utility
{
	public interface ITypeWrapper
	{
		Type Type { get; }

		IFieldWrapper GetFieldForAutoProperty(string propertyName);
		IFieldWrapper GetFieldForAutoProperty(PropertyInfo property);
		IFieldWrapper GetField(string fieldName);
		IFieldWrapper GetField(FieldInfo field);
	}

	public interface IFieldWrapper
	{
		ITypeWrapper DeclaringType { get; }
		FieldInfo FieldInfo { get; }
		PropertyInfo? PropertyInfo { get; }

		Func<object, object?> ClassGetter { get; }
		Action<object, object?> ClassSetter { get; }
	}

	public static class TypeWrapper
	{
		static readonly Dictionary<Type, ITypeWrapper> _typeCache = new();

		public static ITypeWrapper For(Type type)
		{
			if (!_typeCache.TryGetValue(type, out var wrapper))
				_typeCache.Add(type, wrapper = (ITypeWrapper)typeof(TypeWrapper<>)
					.MakeGenericType(type).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
					.GetMethod!.Invoke(null, Array.Empty<object>())!);
			return wrapper;
		}

		public static TypeWrapper<T> For<T>() => TypeWrapper<T>.Instance;
	}

	public class TypeWrapper<TType> : ITypeWrapper
	{
		internal static TypeWrapper<TType> Instance { get; } = new();

		readonly Dictionary<FieldInfo, IFieldWrapperForType> _fieldCache = new();

		public Type Type => typeof(TType);

		TypeWrapper() {  }

		IFieldWrapper ITypeWrapper.GetFieldForAutoProperty(string propertyName) => GetFieldForAutoProperty(propertyName);
		IFieldWrapper ITypeWrapper.GetFieldForAutoProperty(PropertyInfo property) => GetFieldForAutoProperty(property);
		IFieldWrapper ITypeWrapper.GetField(string fieldName) => GetField(fieldName);
		IFieldWrapper ITypeWrapper.GetField(FieldInfo field) => GetField(field);

		public IFieldWrapperForType GetFieldForAutoProperty(string propertyName)
		{
			var property = Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null) throw new MissingMemberException(Type.FullName, propertyName);
			var field = Type.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null) throw new ArgumentException($"Could not find backing field for property {property}");
			return GetField(field, property);
		}
		public IFieldWrapperForType GetFieldForAutoProperty(PropertyInfo property)
		{
			if (property.DeclaringType != Type) throw new ArgumentException(
				$"Specified PropertyInfo {property} needs to be a member of type {Type}", nameof(property));
			var field = Type.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null) throw new ArgumentException($"Could not find backing field for property {property}");
			return GetField(field, property);
		}

		public IFieldWrapperForType GetField(string fieldName)
		{
			var field = Type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null) throw new MissingFieldException(Type.FullName, fieldName);
			return GetField(field, null);
		}
		public IFieldWrapperForType GetField(FieldInfo field)
		{
			if (field.DeclaringType != Type) throw new ArgumentException(
				$"Specified FieldInfo {field} needs to be a member of type {Type}", nameof(field));
			return GetField(field, null);
		}


		public FieldWrapper<TField> GetFieldForAutoProperty<TField>(string propertyName)
		{
			var property = Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null) throw new MissingMemberException(Type.FullName, propertyName);
			var field = Type.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null) throw new ArgumentException($"Could not find backing field for property {property}");
			return GetField<TField>(field, property);
		}
		public FieldWrapper<TField> GetFieldForAutoProperty<TField>(PropertyInfo property)
		{
			if (property.DeclaringType != Type) throw new ArgumentException(
				$"Specified PropertyInfo {property} needs to be a member of type {Type}", nameof(property));
			var field = Type.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null) throw new ArgumentException($"Could not find backing field for property {property}");
			return GetField<TField>(field, property);
		}

		public FieldWrapper<TField> GetField<TField>(string fieldName)
		{
			var field = Type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null) throw new MissingFieldException(Type.FullName, fieldName);
			return GetField<TField>(field, null);
		}
		public FieldWrapper<TField> GetField<TField>(FieldInfo field)
		{
			if (field.DeclaringType != Type) throw new ArgumentException(
				$"Specified FieldInfo {field} needs to be a member of type {Type}", nameof(field));
			return GetField<TField>(field, null);
		}


		IFieldWrapperForType GetField(FieldInfo field, PropertyInfo? property)
		{
			if (_fieldCache.TryGetValue(field, out var cached)) return cached;
			var type    = typeof(FieldWrapper<>).MakeGenericType(typeof(TType), field.FieldType);
			var wrapper = (IFieldWrapperForType)Activator.CreateInstance(
				type, BindingFlags.Instance | BindingFlags.NonPublic,
				null, new object?[]{ this, field, property }, null)!;
			_fieldCache.Add(field, wrapper);
			return wrapper;
		}

		FieldWrapper<TField> GetField<TField>(FieldInfo field, PropertyInfo? property)
		{
			if (_fieldCache.TryGetValue(field, out var cached)) return (FieldWrapper<TField>)cached;
			if (field.FieldType != typeof(TField)) throw new ArgumentException(
				$"FieldType ({field.FieldType}) does not match TField ({typeof(TField)})", nameof(TField));
			var wrapper = new FieldWrapper<TField>(this, field, property);
			_fieldCache.Add(field, wrapper);
			return wrapper;
		}


		public interface IFieldWrapperForType : IFieldWrapper
		{
			delegate object? ValueGetterAction(in TType obj);
			delegate void ValueSetterAction(ref TType obj, object? value);

			Func<object, object?>   IFieldWrapper.ClassGetter => (obj) => ClassGetter((TType)obj);
			Action<object, object?> IFieldWrapper.ClassSetter => (obj, value) => ClassSetter((TType)obj, value);
			new Func<TType, object?>   ClassGetter { get; }
			new Action<TType, object?> ClassSetter { get; }

			ValueGetterAction ByRefGetter { get; }
			ValueSetterAction ByRefSetter { get; }
		}

		public class FieldWrapper<TField> : IFieldWrapperForType
		{
			public delegate TField ValueGetterAction(in TType obj);
			public delegate void ValueSetterAction(ref TType obj, TField value);

			Func<TType, TField>?   _classGetter;
			Action<TType, TField>? _classSetter;
			ValueGetterAction? _byRefGetter;
			ValueSetterAction? _byRefSetter;

			public ITypeWrapper DeclaringType { get; }
			public FieldInfo FieldInfo { get; }
			public PropertyInfo? PropertyInfo { get; }

			internal FieldWrapper(ITypeWrapper type, FieldInfo field, PropertyInfo? property)
				{ DeclaringType = type; FieldInfo = field; PropertyInfo = property; }

			Func<TType, object?>   IFieldWrapperForType.ClassGetter => (obj) => ClassGetter(obj);
			Action<TType, object?> IFieldWrapperForType.ClassSetter => (obj, value) => ClassSetter(obj, (TField)value!);
			public Func<TType, TField>   ClassGetter => _classGetter ??= BuildGetter<Func<TType, TField>>(false);
			public Action<TType, TField> ClassSetter => _classSetter ??= BuildSetter<Action<TType, TField>>(false);

			IFieldWrapperForType.ValueGetterAction IFieldWrapperForType.ByRefGetter => (in TType obj) => ByRefGetter(in obj);
			IFieldWrapperForType.ValueSetterAction IFieldWrapperForType.ByRefSetter => (ref TType obj, object? value) => ByRefSetter(ref obj, (TField)value!);
			public ValueGetterAction ByRefGetter => _byRefGetter ??= BuildGetter<ValueGetterAction>(true);
			public ValueSetterAction ByRefSetter => _byRefSetter ??= BuildSetter<ValueSetterAction>(true);


			TDelegate BuildGetter<TDelegate>(bool byRef)
				where TDelegate : Delegate
			{
				if (DeclaringType.Type.IsValueType && !byRef) throw new InvalidOperationException(
					$"Can't build getter for value type ({DeclaringType.Type}) without using ref");
				var method = new DynamicMethod(
					$"Get_{DeclaringType.Type.Name}_{FieldInfo.Name}{(byRef ? "_ByRef" : "")}",
					typeof(TField), new []{ byRef ? typeof(TType).MakeByRefType() : typeof(TType) },
					typeof(TType).Module, true);
				var il = method.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				if (byRef && !DeclaringType.Type.IsValueType)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldfld, FieldInfo);
				il.Emit(OpCodes.Ret);
				return method.CreateDelegate<TDelegate>();
			}

			TDelegate BuildSetter<TDelegate>(bool byRef)
				where TDelegate : Delegate
			{
				if (DeclaringType.Type.IsValueType && !byRef) throw new InvalidOperationException(
					$"Can't build setter for value type ({DeclaringType.Type}) without using ref");
				var method = new DynamicMethod(
					$"Set_{DeclaringType.Type.Name}_{FieldInfo.Name}{(byRef ? "_ByRef" : "")}",
					null, new []{ (byRef ? typeof(TType).MakeByRefType() : typeof(TType)), typeof(TField) },
					typeof(TType).Module, true);
				var il = method.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				if (byRef && !DeclaringType.Type.IsValueType)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Stfld, FieldInfo);
				il.Emit(OpCodes.Ret);
				return method.CreateDelegate<TDelegate>();
			}
		}
	}
}
