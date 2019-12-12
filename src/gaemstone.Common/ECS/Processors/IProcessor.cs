
namespace gaemstone.Common.ECS.Processors
{
	public interface IProcessor
	{
		void OnLoad(Universe universe);
		void OnUnload();

		void OnUpdate(double delta);
	}
}
