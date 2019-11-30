using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Buffer<T>
		where T : struct
	{
		public static Buffer<T> Gen(BufferTargetARB target)
			=> new Buffer<T>(GFX.GL.GenBuffer(), target);

		public static Buffer<T> CreateFromData(T[] data,
			BufferTargetARB target = BufferTargetARB.ArrayBuffer,
			BufferUsageARB usage   = BufferUsageARB.StaticDraw)
		{
			var buffer = Gen(target);
			buffer.Bind();
			buffer.Data(usage, data);
			return buffer;
		}


		public uint Handle { get; }
		public BufferTargetARB Target { get; }

		private Buffer(uint handle, BufferTargetARB target)
			=> (Handle, Target) = (handle, target);

		public void Bind()
			=> GFX.GL.BindBuffer(Target, Handle);

		public void Data(BufferUsageARB usage, T[] data)
		{ unsafe {
			GFX.GL.BufferData(Target, (uint)(data.Length * Unsafe.SizeOf<T>()),
			                  Unsafe.AsPointer(ref data[0]), usage);
		} }
	}
}
