#version 330 core

uniform vec2 invFrameSizeIn;

out vec2 texCoord;

void main(void)
{
    // Generate a full-screen triangle using gl_VertexID
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0.0, 1.0);

    // Map vertex positions to normalized screen coordinates
    texCoord = vec2((x + 1.0) * 0.5, (y + 1.0) * 0.5);
}
