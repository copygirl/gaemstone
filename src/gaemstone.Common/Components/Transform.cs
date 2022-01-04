using System.Numerics;

namespace gaemstone.Common
{
	public struct Transform
	{
		public Matrix4x4 Value;

		public Vector3 Translation {
			get => Value.Translation;
			set => Value.Translation = value;
		}

		public static implicit operator Transform(in Matrix4x4 value) => new(){ Value = value };
		public static implicit operator Matrix4x4(in Transform transform) => transform.Value;
	}
}
