namespace gaemstone.Client.Components
{
	public class Camera
	{
		public static readonly Camera Default2D = Create2D();
		public static readonly Camera Default3D = Create3D(80.0F);

		public static Camera Create2D(float nearPlane = -100.0F, float farPlane = 100.0F)
			=> new Camera { NearPlane = nearPlane, FarPlane = farPlane };
		public static Camera Create3D(float fieldOfView, float nearPlane = 0.1F, float farPlane = 200.0F)
			=> new Camera { FieldOfView = fieldOfView, NearPlane = nearPlane, FarPlane = farPlane };

		public float FieldOfView { get; set; }
		public float NearPlane { get; set; }
		public float FarPlane { get; set; }

		public bool IsOrthographic {
			get => (FieldOfView == 0.0F);
			set => FieldOfView = (value ? 0.0F : Default3D.FieldOfView);
		}
	}
}
