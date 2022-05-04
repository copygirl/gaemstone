namespace gaemstone
{
	public interface IProcessor
	{
		// TODO: Dependencies on other processors.

		void OnLoad();
		void OnUnload();

		// TODO: Use TimeSpan instead of double.
		void OnUpdate(double delta);
	}
}
