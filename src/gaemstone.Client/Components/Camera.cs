using System.Drawing;
using gaemstone.Common.Stores;

namespace gaemstone.Client
{
	[Store(typeof(DictionaryStore<>))]
	public class Camera
	{
		public static readonly Camera Default2D = Create2D();
		public static readonly Camera Default3D = Create3D(80.0F);

		public static Camera Create2D(
			float nearPlane = -100.0F, float farPlane = 100.0F,
			Color? clearColor = null, Rectangle? viewport = null
		) => new(){
			NearPlane = nearPlane, FarPlane = farPlane,
			ClearColor = clearColor, Viewport = viewport,
		};

		public static Camera Create3D(
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
			set => FieldOfView = (value ? 0.0F : Default3D.FieldOfView);
		}
	}
}
