using System.Drawing;
namespace gaemstone.Client
{
	public class Camera
	{
		public static readonly Camera Default2d = Create2d();
		public static readonly Camera Default3d = Create3d(80.0F);

		public static Camera Create2d(
			float nearPlane = -100.0F, float farPlane = 100.0F,
			Color? clearColor = null, Rectangle? viewport = null
		) => new(){
			NearPlane = nearPlane, FarPlane = farPlane,
			ClearColor = clearColor, Viewport = viewport,
		};

		public static Camera Create3d(
			float fieldOfView, float nearPlane = 0.1F, float farPlane = 200.0F,
			Color? clearColor = null, Rectangle? viewport = null
		) => new(){
			FieldOfView = fieldOfView, NearPlane = nearPlane, FarPlane = farPlane,
			ClearColor = clearColor, Viewport = viewport,
		};

		public float FieldOfView { get; set; }
		public float NearPlane { get; set; }
		public float FarPlane { get; set; }

		public Color? ClearColor { get; set; }
		public Rectangle? Viewport { get; set; }

		public bool IsOrthographic {
			get => (FieldOfView == 0.0F);
			set => FieldOfView = (value ? 0.0F : Default3d.FieldOfView);
		}

		public override string ToString()
			=> $"Camera {{ FieldOfView={FieldOfView}, NearPlane={NearPlane}, FarPlane={FarPlane} }}";
	}
}
