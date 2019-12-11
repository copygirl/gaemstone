#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

uniform mat4 modelViewProjection;

out vec4 fragmentColor;

void main(void)
{
  gl_Position = modelViewProjection * vec4(position, 1.0);
  float r = normal.x; if (r < 0) r *= -0.5;
  float g = normal.y; if (g < 0) g *= -0.5;
  float b = normal.z; if (b < 0) b *= -0.5;
  fragmentColor = vec4(r, g, b, 1.0);
}
