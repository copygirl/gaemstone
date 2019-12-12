#version 330 core
layout(location = 0) in vec3 vertPosition;
layout(location = 1) in vec3 vertNormal;
layout(location = 2) in vec2 vertUV;

uniform mat4 modelViewProjection;
uniform bool enableTexture;

out vec4 fragColor;
out vec2 fragUV;

void main()
{
  gl_Position = modelViewProjection * vec4(vertPosition, 1.0);
  if (enableTexture) {
    float l = 0.5 + (vertNormal.y + 1) / 4.0 - (vertNormal.z + 1) / 8.0;
    fragColor = vec4(l, l, l, 1.0);
  } else {
    float r = vertNormal.x; if (r < 0) r *= -0.5;
    float g = vertNormal.y; if (g < 0) g *= -0.5;
    float b = vertNormal.z; if (b < 0) b *= -0.5;
    fragColor = vec4(r, g, b, 1.0);
  }
  fragUV = vertUV;
}
