using System.Drawing;
using System.Numerics;

namespace gaemstone.Client.Components
{
	public struct Camera
	{
		public Rectangle Viewport;
		public Matrix4x4 Projection;
	}
}
