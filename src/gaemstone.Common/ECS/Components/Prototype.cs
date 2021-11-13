using gaemstone.Common.ECS;

namespace gaemstone.Common.Components
{
	public readonly struct Prototype
	{
		public readonly Entity Value { get; }

		public Prototype(Entity value) => Value = value;

		public static implicit operator Prototype(in Entity value)
			=> new Prototype(value);
		public static implicit operator Entity(in Prototype prototype)
			=> prototype.Value;
	}
}
