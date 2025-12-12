#version 330

// Precision qualifiers for better Mac compatibility
precision mediump float;

// Input vertex attributes (from vertex shader)
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Output fragment color
out vec4 finalColor;

// NOTE: Add here your custom variables

#define     MAX_LIGHTS              32
#define     LIGHT_DIRECTIONAL       0
#define     LIGHT_POINT             1

struct Light {
    int enabled;
    int type;
    vec3 position;
    vec3 target;
    vec4 color;
};

// Input lighting values
uniform Light lights[MAX_LIGHTS];
uniform vec3 viewPos;
uniform float shininess;

// Simple pseudo-random function for dirty shadows
float rand(vec2 co)
{
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

// Dithering for pixelated look
float dither2x2(vec2 position, float brightness)
{
    int x = int(mod(position.x, 2.0));
    int y = int(mod(position.y, 2.0));
    int index = x + y * 2;
    float limit = 0.0;

    if (index == 0) limit = 0.25;
    else if (index == 1) limit = 0.75;
    else if (index == 2) limit = 1.0;
    else limit = 0.5;

    return brightness < limit ? 0.0 : 1.0;
}

void main()
{
    // Texel color fetching from texture sampler
    vec4 texelColor = texture(texture0, fragTexCoord);
    vec3 lightDot = vec3(0.0);
    vec3 normal = normalize(fragNormal);
    vec3 viewD = normalize(viewPos - fragPosition);
    vec3 specular = vec3(0.0);

    // Use shininess with fallback
    float shininessValue = max(shininess, 16.0);

    // Calculate lighting
    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (lights[i].enabled == 1)
        {
            vec3 light = vec3(0.0);

            if (lights[i].type == LIGHT_DIRECTIONAL)
            {
                light = -normalize(lights[i].target - lights[i].position);
            }

            if (lights[i].type == LIGHT_POINT)
            {
                light = normalize(lights[i].position - fragPosition);
            }

            float NdotL = max(dot(normal, light), 0.0);
            lightDot += lights[i].color.rgb * NdotL;

            float specCo = 0.0;
            if (NdotL > 0.0) specCo = pow(max(0.0, dot(viewD, reflect(-light, normal))), shininessValue);
            specular += specCo;
        }
    }

    // Base color calculation
    finalColor = (texelColor * ((colDiffuse + vec4(specular, 1.0)) * vec4(lightDot, 1.0)));
    finalColor += texelColor * (0.2); // Ambient light
    finalColor *= fragColor;

    // Add dirty shadow effect
    vec2 shadowCoord = fragPosition.xz * 0.5; // Use world position for consistent shadows
    float noise = rand(shadowCoord);

    // Vertical gradient shadow (darker below, lighter above)
    float heightFactor = smoothstep(-10.0, 10.0, fragPosition.y);
    float dirtyShadow = mix(0.7, 1.0, heightFactor);

    // Add noise variation to shadows
    dirtyShadow = mix(dirtyShadow, dirtyShadow * 0.85, noise * 0.4);

    // Apply dirty shadows
    finalColor.rgb *= dirtyShadow;

    // Subtle dithering for pixelated retro look
    float brightness = dot(finalColor.rgb, vec3(0.299, 0.587, 0.114));
    vec2 pixelPos = fragTexCoord * 512.0; // Adjust scale for dither pattern
    float ditherVal = dither2x2(pixelPos, brightness);

    // Very subtle dither mix (5% blend)
    finalColor.rgb = mix(finalColor.rgb, vec3(brightness * ditherVal), 0.05);

    // Slightly reduce color saturation for grittier look
    float gray = dot(finalColor.rgb, vec3(0.299, 0.587, 0.114));
    finalColor.rgb = mix(finalColor.rgb, vec3(gray), 0.15);
}