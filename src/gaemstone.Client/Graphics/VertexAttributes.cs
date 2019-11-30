using System.Collections;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace gaemstone.Client.Graphics
{
	public class VertexAttributes
		: IReadOnlyList<VertexAttributeInfo>
	{
		private readonly VertexAttributeInfo[] _activeAttribs;
		private readonly Dictionary<string, VertexAttributeInfo> _attribsByName
			= new Dictionary<string, VertexAttributeInfo>();

		public int Count => _activeAttribs.Length;

		public VertexAttributeInfo this[int index] => _activeAttribs[index];
		public VertexAttributeInfo this[string name] => _attribsByName[name];

		internal VertexAttributes(Program program)
		{
			GFX.GL.GetProgram(program.Handle, ProgramPropertyARB.ActiveAttributeMaxLength, out var attribMaxLength);
			var nameBuffer = new string(' ', attribMaxLength);
			GFX.GL.GetProgram(program.Handle, ProgramPropertyARB.ActiveAttributes, out var attribCount);
			_activeAttribs = new VertexAttributeInfo[attribCount];
			for (uint attribIndex = 0; attribIndex < attribCount; attribIndex++) {
				GFX.GL.GetActiveAttrib(program.Handle, attribIndex, (uint)attribMaxLength,
				                       out var length, out var size, out AttributeType type, out nameBuffer);
				var name     = nameBuffer.Substring(0, (int)length);
				var location = GFX.GL.GetAttribLocation(program.Handle, name);
				var attrib   = new VertexAttributeInfo(program, attribIndex, location, size, type, name);
				_activeAttribs[attribIndex] = attrib;
				_attribsByName[nameBuffer]  = attrib;
			}
		}

		public IEnumerator<VertexAttributeInfo> GetEnumerator()
			=> ((IEnumerable<VertexAttributeInfo>)_activeAttribs).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}

	public class VertexAttributeInfo
	{
		public Program Program { get; }
		public uint Index { get; }
		public int Location { get; }
		public int Size { get; }
		public AttributeType Type { get; }
		public string Name { get; }

		internal VertexAttributeInfo(Program program, uint index, int location, int size, AttributeType type, string name)
			=> (Program, Index, Location, Size, Type, Name) = (program, index, location, size, type, name);

		public void Pointer(int size, VertexAttribPointerType type,
		                    bool normalized = false, uint stride = 0, int offset = 0)
		{
			GFX.GL.EnableVertexAttribArray(Index);
			GFX.GL.VertexAttribPointer(Index, size, (GLEnum)type, normalized, stride, offset);
		}
	}
}
