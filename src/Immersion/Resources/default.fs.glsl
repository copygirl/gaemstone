#version 330 core
in vec4 fragColor;
in vec2 fragUV;

uniform sampler2D textureSampler;

out vec4 color;

void main()
{
  color = fragColor * texture(textureSampler, fragUV);
}
