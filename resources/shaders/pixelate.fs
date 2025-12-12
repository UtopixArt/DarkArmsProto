#version 330

// Precision qualifiers for better Mac compatibility
precision mediump float;

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Pixelation settings
uniform float pixelSize; // Size of each "pixel" (default: 4.0)
uniform vec2 resolution;  // Screen resolution

// Output fragment color
out vec4 finalColor;

// Simple pseudo-random function
float rand(vec2 co)
{
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

// Dithering pattern (Bayer matrix 4x4)
float dither4x4(vec2 position, float brightness)
{
    int x = int(mod(position.x, 4.0));
    int y = int(mod(position.y, 4.0));

    // Bayer matrix 4x4
    float bayerMatrix[16];
    bayerMatrix[0] = 0.0;    bayerMatrix[1] = 8.0;    bayerMatrix[2] = 2.0;    bayerMatrix[3] = 10.0;
    bayerMatrix[4] = 12.0;   bayerMatrix[5] = 4.0;    bayerMatrix[6] = 14.0;   bayerMatrix[7] = 6.0;
    bayerMatrix[8] = 3.0;    bayerMatrix[9] = 11.0;   bayerMatrix[10] = 1.0;   bayerMatrix[11] = 9.0;
    bayerMatrix[12] = 15.0;  bayerMatrix[13] = 7.0;   bayerMatrix[14] = 13.0;  bayerMatrix[15] = 5.0;

    float threshold = bayerMatrix[x + y * 4] / 16.0;
    return brightness > threshold ? 1.0 : 0.0;
}

void main()
{
    vec2 uv = fragTexCoord;

    // Apply pixelation effect
    if (pixelSize > 1.0 && resolution.x > 0.0)
    {
        // Calculate pixel size in UV space
        vec2 pixelUV = vec2(pixelSize) / resolution;

        // Snap to pixel grid
        uv = floor(fragTexCoord / pixelUV) * pixelUV;
    }

    // Sample texture
    vec4 texelColor = texture(texture0, uv);

    // Apply color and diffuse
    vec4 color = texelColor * colDiffuse * fragColor;

    // Add dirty shadow effect using noise
    vec2 shadowCoord = uv * 100.0; // Scale for noise
    float noise = rand(shadowCoord);

    // Create shadow based on UV position (darker at edges and bottom)
    float vignette = 1.0 - length(fragTexCoord - 0.5) * 0.5;
    float bottomShadow = smoothstep(0.0, 0.4, fragTexCoord.y); // Darker at bottom

    // Combine shadow effects
    float shadowStrength = mix(0.6, 1.0, vignette * bottomShadow);
    shadowStrength = mix(shadowStrength, shadowStrength * 0.9, noise * 0.3); // Add noise variation

    color.rgb *= shadowStrength;

    // Optional: Apply dithering for retro look
    float brightness = (color.r + color.g + color.b) / 5.0;
    vec2 pixelPos = fragTexCoord * resolution;
    float ditherValue = dither4x4(pixelPos, brightness);

    // Mix original color with dithered effect (subtle)
    color.rgb = mix(color.rgb, vec3(ditherValue), 0.1);

    finalColor = color;
}
