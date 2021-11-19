using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text.Json;

namespace gaemstone.Client.Graphics
{
	public class SpriteSheet
		: IReadOnlyList<SpriteSheet.Sprite>
	{
		readonly List<Sprite> _byIndex = new();
		readonly Dictionary<string, Sprite> _byName = new();

		public int Count => _byIndex.Count;
		public Sprite this[int index] => _byIndex[index];
		public Sprite this[string name] => _byName[name];


		public static SpriteSheet Load(Game game, string name)
		{
			var json   = game.GetResourceAsBytes(name);
			var reader = new Utf8JsonReader(json, true, default);
			var sheet  = new SpriteSheet();

			reader.Read(); Expect(ref reader, JsonTokenType.StartObject);
			while (reader.Read()) {
				if (reader.TokenType == JsonTokenType.EndObject) break;
				Expect(ref reader, JsonTokenType.PropertyName);
				var spriteName = reader.GetString()!;

				Point? location = null;
				Size?  size     = null;
				Point? center   = null;
				reader.Read(); Expect(ref reader, JsonTokenType.StartObject);
				while (reader.Read()) {
					if (reader.TokenType == JsonTokenType.EndObject) break;
					Expect(ref reader, JsonTokenType.PropertyName);
					var propertyName = reader.GetString();
					switch (propertyName) {
						case nameof(location):
							var (x, y) = GetTwoInts(ref reader);
							location = new(x, y);
							break;
						case nameof(size):
							var (width, height) = GetTwoInts(ref reader);
							size = new(width, height);
							break;
						case nameof(center):
							var (cx, cy) = GetTwoInts(ref reader);
							center = new(cx, cy);
							break;
					}
				}
				if (location == null) throw new JsonException(
					$"Required property '{nameof(location)}' is missing");
				if (size == null) throw new JsonException(
					$"Required property '{nameof(size)}' is missing");
				// If center is not defined, default to center of sprite.
				if (center == null) center = (Point)(size / 2);

				var sprite = new Sprite(sheet, sheet._byIndex.Count, spriteName,
					new(location.Value, size.Value), center.Value);
				sheet._byIndex.Add(sprite);
				sheet._byName.Add(spriteName, sprite);
			}

			void Expect(ref Utf8JsonReader reader, JsonTokenType tokenType)
			{
				if (reader.TokenType != tokenType) throw new JsonException(
					$"Expected token of type '{tokenType}', but got '{reader.TokenType}'");
			}

			(int, int) GetTwoInts(ref Utf8JsonReader reader)
			{
				reader.Read(); Expect(ref reader, JsonTokenType.StartArray);
				reader.Read(); var item1 = reader.GetInt32();
				reader.Read(); var item2 = reader.GetInt32();
				reader.Read(); Expect(ref reader, JsonTokenType.EndArray);
				return (item1, item2);
			}

			return sheet;
		}


		public Mesh Build(Game game, Size textureSize)
		{
			var positions = new Vector3[Count * 6];
			var uvs = new Vector2[Count * 6];

			var count = 0;
			foreach (var sprite in this) {
				var ox = -sprite.Center.X;
				var oy = -sprite.Center.Y;
				var width  = sprite.Bounds.Width;
				var height = sprite.Bounds.Height;
				var topLeftPos     = new Vector3(ox        , oy         , 0);
				var bottomLeftPos  = new Vector3(ox        , oy + height, 0);
				var bottomRightPos = new Vector3(ox + width, oy + height, 0);
				var topRightPos    = new Vector3(ox + width, oy         , 0);

				var tw = (float)textureSize.Width;
				var th = (float)textureSize.Height;
				var left   = sprite.Bounds.Left   / tw;
				var right  = sprite.Bounds.Right  / tw;
				var top    = sprite.Bounds.Top    / th;
				var bottom = sprite.Bounds.Bottom / th;
				var topLeftUV     = new Vector2(left , top   );
				var bottomLeftUV  = new Vector2(left , bottom);
				var bottomRightUV = new Vector2(right, bottom);
				var topRightUV    = new Vector2(right, top   );

				positions[count] = topLeftPos;     uvs[count++] = topLeftUV;
				positions[count] = bottomLeftPos;  uvs[count++] = bottomLeftUV;
				positions[count] = topRightPos;    uvs[count++] = topRightUV;
				positions[count] = bottomLeftPos;  uvs[count++] = bottomLeftUV;
				positions[count] = bottomRightPos; uvs[count++] = bottomRightUV;
				positions[count] = topRightPos;    uvs[count++] = topRightUV;
			}

			var meshManager = game.Processors.GetOrThrow<MeshManager>();
			return meshManager.Create(positions, default, uvs);
		}

		public class Sprite
		{
			public SpriteSheet TextureSheet { get; }
			public SpriteIndex Index { get; }
			public string Name { get; }
			public Rectangle Bounds { get; }
			public Point Center { get; }

			public Sprite(SpriteSheet textureSheet, SpriteIndex index, string name, Rectangle bounds, Point center)
				=> (TextureSheet, Index, Name, Bounds, Center) = (textureSheet, index, name, bounds, center);
		}


		public IEnumerator<Sprite> GetEnumerator()
			=> _byIndex.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> _byIndex.GetEnumerator();
	}
}
