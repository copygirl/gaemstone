using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace gaemstone.Common.Utility
{
	public class ILGeneratorWrapper
	{
		readonly DynamicMethod _method;
		readonly ILGenerator _il;
		readonly List<ILocal> _locals = new();
		readonly List<(int Offset, int Indent, OpCode Code, object? Arg)> _instructions = new();
		readonly Dictionary<Label, int> _labelToOffset = new();
		readonly Stack<BlockImpl> _indents = new();

		public ILGeneratorWrapper(DynamicMethod method)
		{
			_method = method;
			_il     = method.GetILGenerator();
		}

		public string ToReadableString()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Parameters:");
			foreach (var (param, index) in _method.GetParameters().Select((p, i) => (p, i)))
				sb.AppendLine($"  Argument({index}, {param.ParameterType.Name})");
			sb.AppendLine("Return:");
				sb.AppendLine($"  {_method.ReturnType.Name}");
			sb.AppendLine();

			sb.AppendLine("Locals:");
			foreach (var local in _locals)
				sb.AppendLine($"  {local}");
			sb.AppendLine();

			sb.AppendLine("Instructions:");
			foreach (var (offset, indent, code, arg) in _instructions) {
				sb.Append("  ");

				// Append instruction offset.
				if (offset < 0) sb.Append("        ");
				else sb.Append($"0x{offset:X4}  ");

				// Append instruction opcode.
				if (code == OpCodes.Nop) sb.Append("            ");
				else sb.Append($"{code.Name,-12}");

				// Append indents.
				for (var i = 0; i < indent; i++)
					sb.Append("| ");

				// Append instruction argument.
				if (code == OpCodes.Nop) sb.Append("// ");
				switch (arg) {
					case Label label: sb.Append($"Label(0x{_labelToOffset.GetValueOrDefault(label, -1):X4})"); break;
					case not null: sb.Append(arg); break;
				}

				sb.AppendLine();
			}
			return sb.ToString();
		}


		public IArgument Argument(int index) => (IArgument)Activator.CreateInstance(
			typeof(ArgumentImpl<>).MakeGenericType(_method.GetParameters()[index].ParameterType), index)!;
		public IArgument<T> Argument<T>(int index) => (IArgument<T>)Argument(index);

		public ILocal Local(Type type, string? name = null)
		{
			var local = (ILocal)Activator.CreateInstance(
				typeof(LocalImpl<>).MakeGenericType(type), _il.DeclareLocal(type), name)!;
			_locals.Add(local);
			return local;
		}
		public ILocal<T>     Local<T>(string? name = null)              => (ILocal<T>)Local(typeof(T), name);
		public ILocal<Array> LocalArray(Type type, string? name = null) => (ILocal<Array>)Local(type.MakeArrayType(), name);
		public ILocal<T[]>   LocalArray<T>(string? name = null)         => (ILocal<T[]>)Local(typeof(T).MakeArrayType(), name);

		public Label DefineLabel() => _il.DefineLabel();
		public void MarkLabel(Label label)
		{
			_instructions.Add((-1, _indents.Count, OpCodes.Nop, label));
			_labelToOffset.Add(label, _il.ILOffset);
			_il.MarkLabel(label);
		}


		void AddInstr(OpCode code, object? arg = null) => _instructions.Add((_il.ILOffset, _indents.Count, code, arg));
		public void Comment(string comment) => _instructions.Add((-1, _indents.Count, OpCodes.Nop, comment));

		internal void Emit(OpCode code)                { AddInstr(code, null);  _il.Emit(code); }
		internal void Emit(OpCode code, int arg)       { AddInstr(code, arg);   _il.Emit(code, arg); }
		internal void Emit(OpCode code, Type type)     { AddInstr(code, type);  _il.Emit(code, type); }
		internal void Emit(OpCode code, Label label)   { AddInstr(code, label); _il.Emit(code, label); }
		internal void Emit(OpCode code, ILocal local)  { AddInstr(code, local); _il.Emit(code, local.Builder); }
		internal void Emit(OpCode code, IArgument arg) { AddInstr(code, arg);   _il.Emit(code, arg.Index); }
		internal void Emit(OpCode code, MethodInfo method)      { AddInstr(code, method); _il.Emit(code, method); }
		internal void Emit(OpCode code, ConstructorInfo constr) { AddInstr(code, constr); _il.Emit(code, constr); }

		public void LoadNull() => Emit(OpCodes.Ldnull);
		public void LoadConst(int value) => Emit(OpCodes.Ldc_I4, value);

		public void Load(IArgument arg) => Emit(OpCodes.Ldarg, arg);
		public void LoadAddr(IArgument arg) => Emit(OpCodes.Ldarga, arg);

		public void Load(ILocal local) => Emit(OpCodes.Ldloc, local);
		public void LoadAddr(ILocal local) => Emit(OpCodes.Ldloca, local);
		public void Store(ILocal local) => Emit(OpCodes.Stloc, local);
		public void Set(ILocal<int> local, int value) { LoadConst(value); Store(local); }

		public void LoadLength() { Emit(OpCodes.Ldlen); Emit(OpCodes.Conv_I4); }
		public void LoadLength(IArgument<Array> array) { Load(array); LoadLength(); }
		public void LoadLength(ILocal<Array> array)    { Load(array); LoadLength(); }

		public void LoadElem(Type type) => Emit(OpCodes.Ldelem, type);
		public void LoadElem(Type type, int index)         { LoadConst(index); LoadElem(type); }
		public void LoadElem(Type type, ILocal<int> index) { Load(index); LoadElem(type); }
		public void LoadElem(Type type, IArgument<Array> array, int index)         { Load(array); LoadElem(type, index); }
		public void LoadElem(Type type, IArgument<Array> array, ILocal<int> index) { Load(array); LoadElem(type, index); }
		public void LoadElem(Type type, ILocal<Array> array, int index)            { Load(array); LoadElem(type, index); }
		public void LoadElem(Type type, ILocal<Array> array, ILocal<int> index)    { Load(array); LoadElem(type, index); }

		public void LoadElemRef() => Emit(OpCodes.Ldelem_Ref);
		public void LoadElemRef(int index)         { LoadConst(index); LoadElemRef(); }
		public void LoadElemRef(ILocal<int> index) { Load(index); LoadElemRef(); }
		public void LoadElemRef(IArgument<Array> array, int index)         { Load(array); LoadElemRef(index); }
		public void LoadElemRef(IArgument<Array> array, ILocal<int> index) { Load(array); LoadElemRef(index); }
		public void LoadElemRef(ILocal<Array> array, int index)            { Load(array); LoadElemRef(index); }
		public void LoadElemRef(ILocal<Array> array, ILocal<int> index)    { Load(array); LoadElemRef(index); }

		public void LoadElemEither(Type type)                                            { if (type.IsValueType) LoadElem(type); else LoadElemRef(); }
		public void LoadElemEither(Type type, int index)                                 { if (type.IsValueType) LoadElem(type, index); else LoadElemRef(index); }
		public void LoadElemEither(Type type, ILocal<int> index)                         { if (type.IsValueType) LoadElem(type, index); else LoadElemRef(index); }
		public void LoadElemEither(Type type, IArgument<Array> array, int index)         { if (type.IsValueType) LoadElem(type, array, index); else LoadElemRef(array, index); }
		public void LoadElemEither(Type type, IArgument<Array> array, ILocal<int> index) { if (type.IsValueType) LoadElem(type, array, index); else LoadElemRef(array, index); }
		public void LoadElemEither(Type type, ILocal<Array> array, int index)            { if (type.IsValueType) LoadElem(type, array, index); else LoadElemRef(array, index); }
		public void LoadElemEither(Type type, ILocal<Array> array, ILocal<int> index)    { if (type.IsValueType) LoadElem(type, array, index); else LoadElemRef(array, index); }

		public void LoadElemAddr(Type type) => Emit(OpCodes.Ldelema, type);
		public void LoadElemAddr(Type type, int index)         { LoadConst(index); LoadElemAddr(type); }
		public void LoadElemAddr(Type type, ILocal<int> index) { Load(index); LoadElemAddr(type); }
		public void LoadElemAddr(Type type, IArgument<Array> array, int index)         { Load(array); LoadElemAddr(type, index); }
		public void LoadElemAddr(Type type, IArgument<Array> array, ILocal<int> index) { Load(array); LoadElemAddr(type, index); }
		public void LoadElemAddr(Type type, ILocal<Array> array, int index)            { Load(array); LoadElemAddr(type, index); }
		public void LoadElemAddr(Type type, ILocal<Array> array, ILocal<int> index)    { Load(array); LoadElemAddr(type, index); }

		public void Load(PropertyInfo info) => CallVirt(info.GetMethod!);
		public void Load(ILocal obj, PropertyInfo info) { Load(obj); Load(info); }
		public void Load(IArgument obj, PropertyInfo info) { Load(obj); Load(info); }

		public void Add() => Emit(OpCodes.Add);
		public void Increment(ILocal<int> local) { Load(local); LoadConst(1); Add(); Store(local); }

		public void Init(Type type) => Emit(OpCodes.Initobj, type);
		public void Init<T>() where T : struct => Emit(OpCodes.Initobj, typeof(T));

		public void New(ConstructorInfo constructor) => Emit(OpCodes.Newobj, constructor);
		public void New(Type type) => New(type.GetConstructors().Single());
		public void New(Type type, params Type[] paramTypes) => New(type.GetConstructor(paramTypes)!);

		public void Cast(Type type) => Emit(OpCodes.Castclass, type);
		public void Cast<T>() => Cast(typeof(T));

		public void Goto(Label label) => Emit(OpCodes.Br, label);
		public void GotoIf(Label label, ILocal<int> a, Comparison op, ILocal<int> b)
			{ Load(a); Load(b); Emit(op.Code, label); }
		public void GotoIfNull(Label label, ILocal<object> local)
			{ Load(local); Emit(OpCodes.Brfalse, label); }
		public void GotoIfNotNull(Label label, ILocal<object> local)
			{ Load(local); Emit(OpCodes.Brtrue, label); }

		public void CallVirt(MethodInfo method)
			=> Emit(OpCodes.Callvirt, method);

		public void Return() => Emit(OpCodes.Ret);


		public IDisposable For(Action loadMax, out ILocal<int> current)
		{
			var r = Random.Shared.Next(0, 10000);
			Comment($"INIT for loop {r}");

			var curLocal = current = Local<int>($"index_{r}");
			var maxLocal = Local<int>($"length_{r}");

			var bodyLabel = DefineLabel();
			var testLabel = DefineLabel();

			Set(curLocal, 0);
			loadMax(); Store(maxLocal);

			Comment($"BEGIN for loop {r}");
			Goto(testLabel);
			MarkLabel(bodyLabel);
			var indent = Indent();

			return Block(() => {
				Increment(curLocal);
				MarkLabel(testLabel);
				GotoIf(bodyLabel, curLocal, Comparison.LessThan, maxLocal);
				indent.Dispose();
				Comment($"END for loop {r}");
			});
		}


		public IDisposable Block(Action onClose)
			=> new BlockImpl(onClose);
		public IDisposable Indent()
		{
			BlockImpl indent = null!;
			indent = new(() => { if (_indents.Pop() != indent) throw new InvalidOperationException(); });
			_indents.Push(indent);
			return indent;
		}

		internal class BlockImpl : IDisposable
		{
			public Action OnClose { get; }
			public BlockImpl(Action onClose) => OnClose = onClose;
			public void Dispose() => OnClose();
		}


		internal class ArgumentImpl<T>
			: IArgument<T>
		{
			public int Index { get; }
			public Type ArgumentType => typeof(T);
			public ArgumentImpl(int index) => Index = index;
			public override string ToString() => $"Argument({Index}, {ArgumentType.Name})";
		}

		internal class LocalImpl<T>
			: ILocal<T>
		{
			public LocalBuilder Builder { get; }
			public string? Name { get; }
			public Type LocalType => Builder.LocalType;
			public LocalImpl(LocalBuilder builder, string? name) { Builder = builder; Name = name; }
			public override string ToString() => $"Local({Builder.LocalIndex}, {LocalType.Name}){(Name != null ? $" // {Name}" : "")}";
		}
	}

	public class Comparison
	{
		public static Comparison NotEqual    { get; } = new(OpCodes.Bne_Un);
		public static Comparison LessThan    { get; } = new(OpCodes.Blt);
		public static Comparison LessOrEq    { get; } = new(OpCodes.Ble);
		public static Comparison Equal       { get; } = new(OpCodes.Beq);
		public static Comparison GreaterOrEq { get; } = new(OpCodes.Bge);
		public static Comparison GreaterThan { get; } = new(OpCodes.Bgt);

		public OpCode Code { get; }
		private Comparison(OpCode code) => Code = code;
	}

	public interface IArgument
	{
		int Index { get; }
		Type ArgumentType { get; }
	}
	public interface IArgument<out T>
		: IArgument {  }

	public interface ILocal
	{
		LocalBuilder Builder { get; }
		Type LocalType { get; }
	}
	public interface ILocal<out T>
		: ILocal {  }
}
