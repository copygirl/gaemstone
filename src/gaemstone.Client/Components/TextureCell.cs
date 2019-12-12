using System.Drawing;
using System.Numerics;

namespace gaemstone.Client.Graphics
{
	public readonly struct TextureCell
	{
		public Vector2 TopLeft { get; }
		public Vector2 TopRight { get; }
		public Vector2 BottomLeft { get; }
		public Vector2 BottomRight { get; }

		public TextureCell(float x1, float y1, float x2, float y2)
		{
			TopLeft     = new Vector2(x1, y1);
			TopRight    = new Vector2(x2, y1);
			BottomLeft  = new Vector2(x1, y2);
			BottomRight = new Vector2(x2, y2);
		}

		public TextureCell(int cellX, int cellY, int cellSize, Size textureSize)
			: this((float)( cellX      * cellSize) / textureSize.Width  + 0.001F,
			       (float)( cellY      * cellSize) / textureSize.Height + 0.001F,
			       (float)((cellX + 1) * cellSize) / textureSize.Width  - 0.001F,
			       (float)((cellY + 1) * cellSize) / textureSize.Height - 0.001F) {  }
	}
}
