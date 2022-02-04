using System;
using System.Linq;

namespace gaemstone.ECS
{
	public class QueryManager
	{
		readonly Universe _universe;

		internal QueryManager(Universe universe)
			=> _universe = universe;

		public void Run(Delegate action, EcsType? with = null, EcsType? without = null)
		{
			var method    = action.GetType().GetMethod("Invoke")!;
			var generator = QueryActionGenerator.GetOrBuild(method);
			var query     = new QueryImpl(_universe, action, generator,
				with ?? new(_universe), without ?? new(_universe));
			// TODO: Cache the query somehow.
			query.Run();
		}
	}

	interface IQuery
	{
		void Run();
	}

	class QueryImpl : IQuery
	{
		readonly Universe _universe;
		readonly Delegate _action;
		readonly QueryActionGenerator _generator;

		readonly EcsType _filterWith;
		readonly EcsType _filterWithout;

		public QueryImpl(Universe universe, Delegate action,
		                 QueryActionGenerator generator,
		                 EcsType with, EcsType without)
		{
			_universe  = universe;
			_action    = action;
			_generator = generator;

			_filterWith    = with;
			_filterWithout = without;
		}

		public void Run()
		{
			// TODO: This could be optimized by picking the least common ID first.

			var with = new EcsType(_universe, _generator.Parameters
				.Where(p => (p.Kind != QueryActionGenerator.ParamKind.Entity) && p.IsRequired)
				.Select(p => _universe.GetEntityWithTypeOrThrow(p.UnderlyingType))
				.Concat(_filterWith));

			var tablesAndColumns = _universe.Tables.GetAll(with.First())
				.Where(t => t.Type.Includes(with) && !t.Type.Overlaps(_filterWithout))
				.Select(t => (t, _generator.Parameters.Select(p => t.Columns.Prepend(t.Entities)
					.FirstOrDefault(a => a.GetType().GetElementType() == p.UnderlyingType)).ToArray()))
				.ToArray();

			try {
				foreach (var (table, columns) in tablesAndColumns)
					_generator.GeneratedAction(table, columns, _action);
			} catch (InvalidProgramException) {
				Console.WriteLine(_generator.ReadableString);
				throw;
			}
		}
	}
}
