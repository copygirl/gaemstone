namespace gaemstone.Common
{
	public readonly struct Identifier
	{
		public string Value { get; }
		public Identifier(string value) => Value = value;
		public static implicit operator Identifier(string value) => new(value);
		public static implicit operator string(Identifier id) => id.Value;
	}
}
