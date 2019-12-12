#version 330 core
in vec4 fragColor;
in vec2 fragUV;

uniform bool enableTexture;
uniform sampler2D textureSampler;

out vec4 color;

void main()
{
  if (enableTexture)
    color = fragColor * texture(textureSampler, fragUV);
  else
    color = fragColor;
}
