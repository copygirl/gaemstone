using System;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public readonly struct Buffer
	{
		public static Buffer Gen(BufferTargetARB target)
			=> new(GFX.GL.GenBuffer(), target);

		// These overloads are available because without them, the implicit casting
		// (say from T[] to ReadOnlySpan<T>) causes the generic type resolving to break.
		public static Buffer CreateFromData<T>(T[] data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			where T : unmanaged
			=> CreateFromData<T>((ReadOnlySpan<T>)data, target, usage);
		public static Buffer CreateFromData<T>(ArraySegment<T> data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			where T : unmanaged
			=> CreateFromData<T>((ReadOnlySpan<T>)data, target, usage);
		public static Buffer CreateFromData<T>(Span<T> data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			where T : unmanaged
			=> CreateFromData<T>((ReadOnlySpan<T>)data, target, usage);
		public static Buffer CreateFromData<T>(ReadOnlySpan<T> data,
				BufferTargetARB target = BufferTargetARB.ArrayBuffer,
				BufferUsageARB usage   = BufferUsageARB.StaticDraw)
			where T : unmanaged
		{
			var buffer = Gen(target);
			buffer.Bind();
			buffer.Data(data, usage);
			return buffer;
		}


		public uint Handle { get; }
		public BufferTargetARB Target { get; }

		Buffer(uint handle, BufferTargetARB target)
			=> (Handle, Target) = (handle, target);

		public void Bind()
			=> GFX.GL.BindBuffer(Target, Handle);

		public void Data<T>(T[] data, BufferUsageARB usage)
			where T : unmanaged
			=> Data<T>((ReadOnlySpan<T>)data, usage);
		public void Data<T>(ArraySegment<T> data, BufferUsageARB usage)
			where T : unmanaged
			=> Data<T>((ReadOnlySpan<T>)data, usage);
		public void Data<T>(Span<T> data, BufferUsageARB usage)
			where T : unmanaged
			=> Data<T>((ReadOnlySpan<T>)data, usage);
		public void Data<T>(ReadOnlySpan<T> data, BufferUsageARB usage)
			where T : unmanaged
			=> GFX.GL.BufferData<T>(Target, (UIntPtr)(data.Length * Unsafe.SizeOf<T>()), data, usage);
	}
}
