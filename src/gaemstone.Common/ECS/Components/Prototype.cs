using gaemstone.Common.ECS;

namespace gaemstone.Common.Components
{
	public readonly struct Prototype
	{
		public readonly EcsId Value { get; }
		public Prototype(EcsId value) => Value = value;
		public static implicit operator Prototype(in EcsId value) => new(value);
		public static implicit operator EcsId(in Prototype prototype) => prototype.Value;
	}
}