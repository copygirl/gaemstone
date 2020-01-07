#version 330 core
layout(location = 0) in vec3 vertPosition;
layout(location = 1) in vec3 vertNormal;
layout(location = 2) in vec2 vertUV;

uniform mat4 cameraMatrix;
uniform mat4 modelMatrix;

out vec4 fragColor;
out vec2 fragUV;

void main()
{
  gl_Position = cameraMatrix * modelMatrix * vec4(vertPosition, 1.0);
  // Apply a pseudo-lighting effect based on the object's normals and rotation.
  vec3 normal = mat3(modelMatrix) * vertNormal;
  float l = 0.5 + (normal.y + 1) / 4.0 - (normal.z + 1) / 8.0;
  fragColor = vec4(l, l, l, 1.0);
  fragUV    = vertUV;
}
