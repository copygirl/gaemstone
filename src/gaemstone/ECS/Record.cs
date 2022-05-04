namespace gaemstone.ECS
{
	public struct Record
	{
		public ushort Generation;
		public Table Table;
		public int Row;

		public bool Occupied => (Table != null);
		public EntityType Type => Table.Type;
	}
}
