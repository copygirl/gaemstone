#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec3 color;

uniform mat4 modelViewProjection;

out vec4 fragmentColor;

void main(void)
{
  gl_Position   = modelViewProjection * vec4(position, 1.0);
  fragmentColor = vec4(color, 1.0);
}
