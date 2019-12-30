using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace gaemstone.Bloxel
{
	[Flags]
	public enum Neighbor : byte
	{
		None = 0,

		// FACINGS
		East  = 0b000011, // +X
		West  = 0b000010, // -X
		Up    = 0b001100, // +Y
		Down  = 0b001000, // -Y
		South = 0b110000, // +Z
		North = 0b100000, // -Z

		// CARDINALS
		SouthEast = South | East, // +X +Z
		SouthWest = South | West, // -X +Z
		NorthEast = North | East, // +X -Z
		NorthWest = North | West, // -X -Z

		// ALL_AXIS_PLANES
		UpEast  = Up | East , // +X +Y
		UpWest  = Up | West , // -X +Y
		UpSouth = Up | South, // +Z +Y
		UpNorth = Up | North, // -Z +Y

		DownEast  = Down | East , // +X -Y
		DownWest  = Down | West , // -X -Y
		DownSouth = Down | South, // +Z -Y
		DownNorth = Down | North, // -Z -Y

		// ALL
		UpSouthEast = Up | South | East, // +X +Y +Z
		UpSouthWest = Up | South | West, // -X +Y +Z
		UpNorthEast = Up | North | East, // +X +Y -Z
		UpNorthWest = Up | North | West, // -X +Y -Z

		DownSouthEast = Down | South | East, // +X -Y +Z
		DownSouthWest = Down | South | West, // -X -Y +Z
		DownNorthEast = Down | North | East, // +X -Y -Z
		DownNorthWest = Down | North | West, // -X -Y -Z
	}

	public static class Neighbors
	{
		public static readonly ImmutableHashSet<Neighbor> HORIZONTALS
			= ImmutableHashSet.Create(Neighbor.East , Neighbor.West ,
			                        Neighbor.South, Neighbor.North);

		public static readonly ImmutableHashSet<Neighbor> VERTICALS
			= ImmutableHashSet.Create(Neighbor.Up, Neighbor.Down);

		public static readonly ImmutableHashSet<Neighbor> FACINGS
			= HORIZONTALS.Union(VERTICALS);

		public static readonly ImmutableHashSet<Neighbor> CARDINALS
			= HORIZONTALS.Union(new []{
				Neighbor.SouthEast, Neighbor.SouthWest,
				Neighbor.NorthEast, Neighbor.NorthWest });

		public static readonly ImmutableHashSet<Neighbor> ALL_AXIS_PLANES
			= FACINGS.Union(new []{
				Neighbor.SouthEast, Neighbor.SouthWest,
				Neighbor.NorthEast, Neighbor.NorthWest,
				Neighbor.UpEast   , Neighbor.UpWest   ,
				Neighbor.UpSouth  , Neighbor.UpNorth  ,
				Neighbor.DownEast , Neighbor.DownWest ,
				Neighbor.DownSouth, Neighbor.DownNorth });

		public static readonly ImmutableHashSet<Neighbor> ALL
			= ALL_AXIS_PLANES.Union(new []{
				Neighbor.UpSouthEast, Neighbor.UpSouthWest,
				Neighbor.UpNorthEast, Neighbor.UpNorthWest,
				Neighbor.DownSouthEast, Neighbor.DownSouthWest,
				Neighbor.DownNorthEast, Neighbor.DownNorthWest });
	}

	public static class NeighborExtensions
	{
		private const int X_SET_BIT = 0b000010, X_VALUE_BIT = 0b000001;
		private const int Y_SET_BIT = 0b001000, Y_VALUE_BIT = 0b000100;
		private const int Z_SET_BIT = 0b100000, Z_VALUE_BIT = 0b010000;
		public static void Deconstruct(this Neighbor self,
		                               out int x, out int y, out int z)
		{
			x = (((int)self & X_SET_BIT) != 0) ? ((((int)self & X_VALUE_BIT) != 0) ? 1 : -1) : 0;
			y = (((int)self & Y_SET_BIT) != 0) ? ((((int)self & Y_VALUE_BIT) != 0) ? 1 : -1) : 0;
			z = (((int)self & Z_SET_BIT) != 0) ? ((((int)self & Z_VALUE_BIT) != 0) ? 1 : -1) : 0;
		}


		// public static Neighbor ToNeighbor(this Axis self, int v)
		// {
		// 	if ((v < -1) || (v > 1)) throw new ArgumentOutOfRangeException(
		// 		nameof(v), v, $"{nameof(v)} (={v}) must be within (-1, 1)");
		// 	return self switch {
		// 		Axis.X => (v > 0) ? Neighbor.East  : Neighbor.West ,
		// 		Axis.Y => (v > 0) ? Neighbor.Up    : Neighbor.Down ,
		// 		Axis.Z => (v > 0) ? Neighbor.South : Neighbor.North,
		// 		_ => Neighbor.None
		// 	};
		// }

		// public static Axis GetAxis(this Neighbor self)
		// 	=> self switch {
		// 		Neighbor.East  => Axis.X,
		// 		Neighbor.West  => Axis.X,
		// 		Neighbor.Up    => Axis.Y,
		// 		Neighbor.Down  => Axis.Y,
		// 		Neighbor.South => Axis.Z,
		// 		Neighbor.North => Axis.Z,
		// 		_ => throw new ArgumentException(nameof(self), $"{self} is not one of FACINGS")
		// 	};


		public static Neighbor ToNeighbor(this BlockFacing self)
			=> self switch {
				BlockFacing.East  => Neighbor.East ,
				BlockFacing.West  => Neighbor.West ,
				BlockFacing.Up    => Neighbor.Up   ,
				BlockFacing.Down  => Neighbor.Down ,
				BlockFacing.South => Neighbor.South,
				BlockFacing.North => Neighbor.North,
				_ => throw new ArgumentException(
					$"'{self}' is not a valid BlockFacing", nameof(self))
			};

		public static BlockFacing ToBlockFacing(this Neighbor self)
			=> self switch {
				Neighbor.East  => BlockFacing.East ,
				Neighbor.West  => BlockFacing.West ,
				Neighbor.Up    => BlockFacing.Up   ,
				Neighbor.Down  => BlockFacing.Down ,
				Neighbor.South => BlockFacing.South,
				Neighbor.North => BlockFacing.North,
				_ => throw new ArgumentException(
					$"'{self}' can#t be converted to a valid BlockFacing", nameof(self))
			};


		public static Neighbor ToNeighbor(this (int x, int y, int z) p)
		{
			var neighbor = Neighbor.None;
			if (p.x != 0) {
				if      (p.x ==  1) neighbor |= Neighbor.East;
				else if (p.x == -1) neighbor |= Neighbor.West;
				else throw new ArgumentOutOfRangeException(
					nameof(p), p.x, $"{nameof(p)}.x (={p.x}) must be within (-1, 1)");
			}
			if (p.y != 0) {
				if      (p.y ==  1) neighbor |= Neighbor.Up;
				else if (p.y == -1) neighbor |= Neighbor.Down;
				else throw new ArgumentOutOfRangeException(
					nameof(p), p.y, $"{nameof(p)}.y (={p.y}) must be within (-1, 1)");
			}
			if (p.z != 0) {
				if      (p.z ==  1) neighbor |= Neighbor.South;
				else if (p.z == -1) neighbor |= Neighbor.North;
				else throw new ArgumentOutOfRangeException(
					nameof(p), p.z, $"{nameof(p)}.z (={p.z}) must be within (-1, 1)");
			}
			return neighbor;
		}

		public static Neighbor GetOpposite(this Neighbor self)
			{ var (x, y, z) = self; return (-x, -y, -z).ToNeighbor(); }


		public static BlockPos ToProperPos(this Neighbor self)
			{ var (x, y, z) = self; return new BlockPos(x, y, z); }

		public static Vector3 ToVector3(this Neighbor self)
			{ var (x, y, z) = self; return new Vector3(x, y, z); }


		public static bool IsNone(this Neighbor self)
			=> (self == Neighbor.None);

		public static bool IsHorizontal(this Neighbor self)
			=> Neighbors.HORIZONTALS.Contains(self);
		public static bool IsVertical(this Neighbor self)
			=> Neighbors.VERTICALS.Contains(self);
		public static bool IsCardinal(this Neighbor self)
			=> Neighbors.CARDINALS.Contains(self);
		public static bool IsFacing(this Neighbor self)
			=> Neighbors.FACINGS.Contains(self);
		public static bool IsValid(this Neighbor self)
			=> Neighbors.ALL.Contains(self);


		public static string ToShortString(this Neighbor self)
		{
			if (!self.IsValid()) return "-";
			var sb = new StringBuilder(3);
			foreach (var chr in self.ToString())
				if ((chr >= 'A') && (chr <= 'Z')) // ASCII IsUpper
					sb.Append(chr + 0x20);        // ASCII ToLower
			return sb.ToString();
		}
	}
}
