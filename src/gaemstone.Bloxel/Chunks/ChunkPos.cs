using System;
using System.Numerics;

namespace gaemstone.Bloxel.Chunks
{
	public readonly struct ChunkPos : IEquatable<ChunkPos>
	{
		public static readonly ChunkPos ORIGIN = new(0, 0, 0);

		public int X { get; }
		public int Y { get; }
		public int Z { get; }

		public ChunkPos(int x, int y, int z) => (X, Y, Z) = (x, y, z);
		public void Deconstruct(out int x, out int y, out int z) => (x, y, z) = (X, Y, Z);

		public Vector3 GetOrigin() => new(X << 4, Y << 4, Z << 4);
		public Vector3 GetCenter() => new((X << 4) + 0.5F, (Y << 4) + 0.5F, (Z << 4) + 0.5F);


		public ChunkPos Add(int x, int y, int z) => new(X + x, Y + y, Z + z);
		public ChunkPos Add(in ChunkPos other)   => new(X + other.X, Y + other.Y, Z + other.Z);
		public ChunkPos Add(BlockFacing facing)
			{ var (x, y, z) = facing; return Add(x, y, z); }
		public ChunkPos Add(Neighbor neighbor)
			{ var (x, y, z) = neighbor; return Add(x, y, z); }

		public ChunkPos Subtract(int x, int y, int z) => new(X - x, Y - y, Z - z);
		public ChunkPos Subtract(in ChunkPos other)   => new(X - other.X, Y - other.Y, Z - other.Z);
		public ChunkPos Subtract(BlockFacing facing)
			{ var (x, y, z) = facing; return Subtract(x, y, z); }
		public ChunkPos Subtract(Neighbor neighbor)
			{ var (x, y, z) = neighbor; return Subtract(x, y, z); }


		public bool Equals(ChunkPos other)
			=> (X == other.X) && (Y == other.Y) && (Z == other.Z);
		public override bool Equals(object? obj)
			=> (obj is ChunkPos pos) && Equals(pos);

		public override int GetHashCode() => HashCode.Combine(X, Y, Z);
		public override string ToString() => $"ChunkPos ({X}:{Y}:{Z})";
		public string ToShortString() => $"{X}:{Y}:{Z}";


		public static ChunkPos operator +(ChunkPos left, ChunkPos right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, ChunkPos right) => left.Subtract(right);
		public static ChunkPos operator +(ChunkPos left, BlockFacing right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, BlockFacing right) => left.Subtract(right);
		public static ChunkPos operator +(ChunkPos left, Neighbor right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, Neighbor right) => left.Subtract(right);

		public static bool operator ==(ChunkPos left, ChunkPos right) => left.Equals(right);
		public static bool operator !=(ChunkPos left, ChunkPos right) => !left.Equals(right);
	}

	public static class ChunkPosExtensions
	{
		public static ChunkPos ToChunkPos(this Vector3 pos)
			=> new((int)MathF.Floor(pos.X) >> 4, (int)MathF.Floor(pos.Y) >> 4, (int)MathF.Floor(pos.Z) >> 4);

		public static ChunkPos ToChunkPos(this BlockPos self)
			=> new(self.X >> 4, self.Y >> 4, self.Z >> 4);
		public static BlockPos ToChunkRelative(this BlockPos self)
			=> new(self.X & 0b1111, self.Y & 0b1111, self.Z & 0b1111);
		public static BlockPos ToChunkRelative(this BlockPos self, ChunkPos chunk)
			=> new(self.X - (chunk.X << 4), self.Y - (chunk.Y << 4), self.Z - (chunk.Z << 4));
	}
}
