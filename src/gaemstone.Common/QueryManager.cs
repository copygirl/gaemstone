using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace gaemstone.Common
{
	public class QueryManager
	{
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
		}
	}

	public interface IQuery
	{
		void Run();
	}

	public class QueryBuilder
	{
		static readonly ConditionalWeakTable<Delegate, QueryActionGenerator> _cache = new();

		readonly Universe _universe;
		readonly string _name;

		QueryActionGenerator? _generator;
		bool _wasBuilt = false;

		internal QueryBuilder(Universe universe, string name)
			{_universe = universe; _name = name; }

		public QueryBuilder Execute(Delegate action)
		{
			if (_generator != null) throw new InvalidOperationException(
				"The method 'Execute' can only be called once on this QueryBuilder");
			if (!_cache.TryGetValue(action, out _generator))
				_cache.Add(action, _generator = new(_universe, _name, action));
			return this;
		}

		public IQuery Build()
		{
			if (_generator == null) throw new InvalidOperationException(
				"The method 'Execute' must be called exactly once on this QueryBuilder");
			if (_wasBuilt) throw new InvalidOperationException(
				"The method 'Build' can only be called once on this QueryBuilder");
			_wasBuilt = true;
			return _generator.Build();
		}

		// TODO: Support filters.
	}
}
