#version 460 core

out vec4 out_color;
uniform sampler2D diffuseTexture;
in vec2 TexCoords;

void main()
{
    vec3 color = texture(diffuseTexture, TexCoords).rgb;
    out_color = vec4(color, 1.0);
}