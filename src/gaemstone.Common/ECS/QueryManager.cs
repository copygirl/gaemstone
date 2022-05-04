using System;
using System.Linq;

namespace gaemstone.ECS
{
	public class QueryManager
	{
		readonly Universe _universe;

		internal QueryManager(Universe universe)
			=> _universe = universe;

		public void Run(Delegate action, EntityType? with = null, EntityType? without = null)
		{
			var method    = action.GetType().GetMethod("Invoke")!;
			var generator = QueryActionGenerator.GetOrBuild(method);
			var query     = new QueryImpl(_universe, action, generator,
				with ?? _universe.EmptyType, without ?? _universe.EmptyType);
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

		readonly EntityType _filterWith;
		readonly EntityType _filterWithout;

		public QueryImpl(Universe universe, Delegate action,
		                 QueryActionGenerator generator,
		                 EntityType with, EntityType without)
		{
			_universe  = universe;
			_action    = action;
			_generator = generator;

			_filterWith    = with;
			_filterWithout = without;
		}

		public void Run()
		{
			// TODO: Support using Universe.Entity as parameter type.
			// TODO: This could be optimized by picking the least common id first.

			var with = _universe.Type(_generator.Parameters
				.Where(p => (p.Kind != QueryActionGenerator.ParamKind.Entity) && p.IsRequired)
				.Select(p => (EcsId)_universe.Lookup(p.UnderlyingType).Id)
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
