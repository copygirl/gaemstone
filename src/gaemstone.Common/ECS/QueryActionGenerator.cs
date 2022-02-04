using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using gaemstone.Common.Utility;

namespace gaemstone.ECS
{
	internal delegate void QueryAction(Table table, Array?[] columns, Delegate action);

	internal class QueryActionGenerator
	{
		static readonly ConditionalWeakTable<MethodInfo, QueryActionGenerator> _cache = new();

		public MethodInfo Method { get; }
		public ParamInfo[] Parameters { get; }
		public QueryAction GeneratedAction { get; }
		public string ReadableString { get; }

		QueryActionGenerator(MethodInfo method)
		{
			Method = method;
			Parameters = BuildParameterInfo(method);
			if (!Parameters.Any(c => c.IsRequired && (c.Kind != ParamKind.Entity)))
				throw new ArgumentException($"At least one parameter in {method} must be required");
			(GeneratedAction, ReadableString) = Build();
		}

		public static QueryActionGenerator GetOrBuild(MethodInfo method)
			=>_cache.GetValue(method, m => new QueryActionGenerator(m));

		static ParamInfo[] BuildParameterInfo(MethodInfo method)
			=> method.GetParameters().Select((p, index) => {
				var underlyingType = p.ParameterType;
				var kind = ParamKind.Normal;

				if (p.IsOut)                   throw new ArgumentException("out is not supported\nParameter: " + p);
				if (p.ParameterType.IsArray)   throw new ArgumentException("Arrays are not supported\nParameter: " + p);
				if (p.ParameterType.IsPointer) throw new ArgumentException("Pointers are not supported\nParameter: " + p);

				if (p.ParameterType == typeof(EcsId)) {
					if (index != 0) throw new ArgumentException("EcsId must be the first parameter");
					return new ParamInfo(0, ParamKind.Entity, typeof(EcsId), typeof(EcsId));
				}

				if (p.IsOptional) kind = ParamKind.Optional;
				// TODO: Actually use default values if provided.

				if (p.IsNullable()) {
					if (kind == ParamKind.Optional) throw new ArgumentException(
						"Nullable and Optional are not supported together\nParameter: " + p);
					if (p.ParameterType.IsValueType)
						underlyingType = Nullable.GetUnderlyingType(p.ParameterType)!;
					kind = ParamKind.Nullable;
				}

				if (p.ParameterType.IsByRef) {
					if (kind == ParamKind.Optional) throw new ArgumentException(
						"ByRef and Optional are not supported together\nParameter: " + p);
					if (kind == ParamKind.Nullable) throw new ArgumentException(
						"ByRef and Nullable are not supported together\nParameter: " + p);
					underlyingType = p.ParameterType.GetElementType()!;
					kind = p.IsIn ? ParamKind.In : ParamKind.Ref;
				}

				if (underlyingType.IsPrimitive) throw new ArgumentException(
					"Primitives are not supported\nParameter: " + p);

				return new ParamInfo(index, kind, p.ParameterType, underlyingType);
			}).ToArray();

		(QueryAction, string) Build()
		{
			var name   = "<>Query_" + string.Join("_", Parameters.Select(c => c.UnderlyingType.Name));
			var method = new DynamicMethod(name, null, new[]{ typeof(Table), typeof(Array?[]), typeof(Delegate) });
			var emit   = new ILGeneratorWrapper(method);

			var tableArg   = emit.Argument<Table>(0);
			var columnsArg = emit.Argument<Array?[]>(1);
			var actionArg  = emit.Argument<Delegate>(2);

			var columnLocals = Parameters.Select((c, i) => {
				var local = emit.LocalArray(c.UnderlyingType, $"column_{i}");
				emit.Comment($"column_{i} = ({local.LocalType.Name})columns[{i}];");
				emit.LoadElemRef(columnsArg, i);
				emit.Cast(local.LocalType);
				emit.Store(local);
				return local;
			}).ToArray();

			// Create locals for optional value types, so we can call initobj on them if they don't exist in the columns.
			var missingValueLocals = Parameters.Select((c, i) => {
				if (c.IsRequired || !c.UnderlyingType.IsValueType) return null;
				var local = emit.Local(c.UnderlyingType, $"missingValue_{i}");
				emit.Comment($"missingValue_{i} = default;");
				emit.LoadAddr(local);
				emit.Init(local.LocalType);
				return local;
			}).ToArray();

			var countProp = typeof(Table).GetProperty(nameof(Table.Count))!;
			using (emit.For(() => emit.Load(tableArg, countProp), out var currentLocal)) {
				// Run the action specified in Execute().
				emit.Load(actionArg);
				for (var i = 0; i < Parameters.Length; i++) {
					var type = Parameters[i].UnderlyingType;

					if (Parameters[i].IsByRef)
						emit.LoadElemAddr(type, columnLocals[i], currentLocal);
					else if (Parameters[i].IsRequired)
						emit.LoadElemEither(type, columnLocals[i], currentLocal);
					else {
						var elseLabel = emit.DefineLabel();
						var doneLabel = emit.DefineLabel();

						emit.GotoIfNull(elseLabel, columnLocals[i]);
							emit.LoadElemEither(type, columnLocals[i], currentLocal);
						emit.Goto(doneLabel);
						emit.MarkLabel(elseLabel);
							if (!type.IsValueType) emit.LoadNull();
							else emit.Load(missingValueLocals[i]!);
						emit.MarkLabel(doneLabel);

						if (Parameters[i].Kind == ParamKind.Nullable)
							emit.New(Parameters[i].ParameterType);
					}
				}
				emit.CallVirt(Method);
			}

			emit.Return();

			return (method.CreateDelegate<QueryAction>(), emit.ToReadableString());
		}

		internal class ParamInfo
		{
			public int Index { get; }
			public ParamKind Kind { get; }
			public Type ParameterType { get; }
			public Type UnderlyingType { get; }

			public ParamInfo(int index, ParamKind kind, Type paramType, Type underlyingType)
				{ Index = index; Kind = kind; ParameterType = paramType; UnderlyingType = underlyingType; }

			public bool IsRequired => (Kind != ParamKind.Optional) && (Kind != ParamKind.Nullable);
			public bool IsByRef    => (Kind == ParamKind.In)       || (Kind == ParamKind.Ref);
		}

		internal enum ParamKind
		{
			Normal,
			Entity,
			Optional,
			Nullable,
			In,
			Ref
		}
	}
}
