namespace gaemstone.Common.Processors
{
	public interface IProcessor
	{
		// TODO: Dependencies on other processors.

		void OnLoad();
		void OnUnload();

		void OnUpdate(double delta);
	}
}
