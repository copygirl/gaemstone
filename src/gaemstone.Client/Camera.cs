using System.Drawing;
using System.Numerics;

namespace gaemstone.Client
{
	public struct Camera
	{
		public Rectangle Viewport;
		public Matrix4x4 Projection;
	}
}
