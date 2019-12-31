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
			TopLeft     = new Vector2(x1, y1);
			TopRight    = new Vector2(x2, y1);
			BottomLeft  = new Vector2(x1, y2);
			BottomRight = new Vector2(x2, y2);
		}

		public static TextureCoords4 FromIntCoords(Size textureSize, Point origin, Size size)
			=> FromIntCoords(textureSize, origin.X, origin.Y, size.Width, size.Height);
		public static TextureCoords4 FromIntCoords(Size textureSize, int x, int y, int width, int height)
			=> new TextureCoords4(
				 x           / (float)textureSize.Width  + 0.001F,
				 y           / (float)textureSize.Height + 0.001F,
				(x + width ) / (float)textureSize.Width  - 0.001F,
				(y + height) / (float)textureSize.Height - 0.001F);

		public static TextureCoords4 FromGrid(Size textureSize, int cellX, int cellY, int cellSize)
			=> new TextureCoords4(
				( cellX      * cellSize) / (float)textureSize.Width  + 0.001F,
				( cellY      * cellSize) / (float)textureSize.Height + 0.001F,
				((cellX + 1) * cellSize) / (float)textureSize.Width  - 0.001F,
				((cellY + 1) * cellSize) / (float)textureSize.Height - 0.001F);
	}
}
