namespace gaemstone.Common.Processors
{
	public interface IProcessor
	{
		// TODO: Dependencies on other processors.

		void OnLoad(Universe universe);
		void OnUnload();

		void OnUpdate(double delta);
	}
}
