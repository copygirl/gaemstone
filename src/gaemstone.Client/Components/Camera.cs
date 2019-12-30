using System.Drawing;
using System.Numerics;

namespace gaemstone.Client.Components
{
	public struct Camera
	{
		public Rectangle Viewport;
		public Matrix4x4 Matrix;
	}

	public struct MainCamera
	{
		public static readonly MainCamera Default2D
			= new MainCamera { NearPlane = -100.0F, FarPlane = 100.0F };
		public static readonly MainCamera Default3D
			= new MainCamera { FieldOfView = 80.0F, NearPlane = 0.1F, FarPlane = 100.0F };

		public float FieldOfView;
		public float NearPlane, FarPlane;

		public bool IsOrthographic {
			get => (FieldOfView == 0.0F);
			set => FieldOfView = (value ? 0.0F : Default3D.FieldOfView);
		}
	}
}
