using System.Numerics;

namespace gaemstone.Client.Components
{
	public struct Transform
	{
		public Matrix4x4 Value;

		public static implicit operator Transform(in Matrix4x4 value)
			=> new Transform { Value = value };
		public static implicit operator Matrix4x4(in Transform transform)
			=> transform.Value;
	}
}
