using System;
using System.Collections.Generic;
using System.Linq;

namespace gaemstone.Common
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
				with ?? EcsType.Empty, without ?? EcsType.Empty);
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

		IEnumerable<(Archetype, Array?[])>? _cachedArchetypes;

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
			// TODO: Invalidate this when affected archetypes are changed.
			//       Basically, when new ones are added that match the query.
			if (_cachedArchetypes == null) {
				var node = _universe.Archetypes[_filterWith];
				foreach (var parameter in _generator.Parameters) {
					if (parameter.Kind == QueryActionGenerator.ParamKind.Entity) continue;
					var entity = _universe.GetEntityForComponentTypeOrThrow(parameter.UnderlyingType);
					if (parameter.IsRequired) node = node.With(entity);
				}

				_cachedArchetypes = node
					.Where(archetype => !_filterWithout.Overlaps(archetype.Type))
					.Select(archetype => (archetype, _generator.Parameters
						.Select(parameter => archetype.Columns.Prepend(archetype.Entities)
							.FirstOrDefault(array => array.GetType().GetElementType() == parameter.UnderlyingType))
							.ToArray()))
					.ToArray();
			}

			try {
				foreach (var (archetype, columns) in _cachedArchetypes)
					_generator.GeneratedAction(archetype, columns, _action);
			} catch (InvalidProgramException) {
				Console.WriteLine(_generator.ReadableString);
				throw;
			}
		}
	}
}
