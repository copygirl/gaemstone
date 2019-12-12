using System;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Buffer
	{
		public static Buffer Gen(BufferTargetARB target)
			=> new Buffer(GFX.GL.GenBuffer(), target);

		public static Buffer CreateFromData<T>(T[] data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			=> CreateFromData<T>((Span<T>)data, target, usage);
		public static Buffer CreateFromData<T>(ArraySegment<T> data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			=> CreateFromData<T>((Span<T>)data, target, usage);
		public static Buffer CreateFromData<T>(Span<T> data,
			BufferTargetARB target = BufferTargetARB.ArrayBuffer,
			BufferUsageARB usage   = BufferUsageARB.StaticDraw)
		{
			var buffer = Gen(target);
			buffer.Bind();
			buffer.Data(data, usage);
			return buffer;
		}


		public uint Handle { get; }
		public BufferTargetARB Target { get; }

		private Buffer(uint handle, BufferTargetARB target)
			=> (Handle, Target) = (handle, target);

		public void Bind()
			=> GFX.GL.BindBuffer(Target, Handle);

		public void Data<T>(T[] data, BufferUsageARB usage)
			=> Data<T>((Span<T>)data, usage);
		public void Data<T>(ArraySegment<T> data, BufferUsageARB usage)
			=> Data<T>((Span<T>)data, usage);
		public void Data<T>(Span<T> data, BufferUsageARB usage)
		{ unsafe {
			GFX.GL.BufferData(Target, (uint)(data.Length * Unsafe.SizeOf<T>()),
			                  Unsafe.AsPointer(ref data[0]), usage);
		} }
	}
}
