using System.Drawing;
using System.Numerics;

namespace gaemstone.Client.Graphics
{
	public readonly struct TextureCoords4
	{
		public Vector2 TopLeft { get; }
		public Vector2 TopRight { get; }
		public Vector2 BottomLeft { get; }
		public Vector2 BottomRight { get; }

		public TextureCoords4(float x1, float y1, float x2, float y2)
		{
			TopLeft     = new(x1, y1);
			TopRight    = new(x2, y1);
			BottomLeft  = new(x1, y2);
			BottomRight = new(x2, y2);
		}

		public static TextureCoords4 FromIntCoords(Size textureSize, Point origin, Size size)
			=> FromIntCoords(textureSize, origin.X, origin.Y, size.Width, size.Height);
		public static TextureCoords4 FromIntCoords(Size textureSize, int x, int y, int width, int height) => new(
			 x           / (float)textureSize.Width  + 0.001F,
			 y           / (float)textureSize.Height + 0.001F,
			(x + width ) / (float)textureSize.Width  - 0.001F,
			(y + height) / (float)textureSize.Height - 0.001F);

		public static TextureCoords4 FromGrid(int numCellsX, int numCellsY, int cellX, int cellY) => new(
			 cellX      / (float)numCellsX + 0.001F,
			 cellY      / (float)numCellsY + 0.001F,
			(cellX + 1) / (float)numCellsX - 0.001F,
			(cellY + 1) / (float)numCellsY - 0.001F);
	}
}
