#version 330 core

uniform sampler2D screenTexture; // The game screen texture
uniform float time;              // Time uniform for animations

in vec2 texCoord;
layout(location = 0) out vec4 outColor;

// Function to simulate sweat drops
float sweatDrip(vec2 uv, float dripTime, float dripSize, float dripSpeed) {
    float yDrip = mod(time * dripSpeed + uv.y, 1.0); // Simulate dripping motion
    float xSpread = 0.05 * sin(uv.y * 10.0 + time);  // Slight wobble
    float dripMask = smoothstep(dripSize, dripSize + 0.01, abs(uv.x - 0.5 - xSpread));
    dripMask *= smoothstep(0.0, 0.01, yDrip - dripTime);
    return dripMask;
}

void main(void) {
    vec2 uv = texCoord;

    // Define clear center region
    float clearRadius = 0.25;
    float clearCenter = length(uv - vec2(0.5, 0.5));

    // Blurred edges
    float blurFactor = smoothstep(clearRadius, 0.5, clearCenter);
    vec4 originalColor = texture(screenTexture, uv);
    vec4 blurredColor = vec4(0.0);

    // Simple radial blur
    for (float x = -2.0; x <= 2.0; x += 1.0) {
        for (float y = -2.0; y <= 2.0; y += 1.0) {
            vec2 offset = vec2(x, y) * 0.002;
            blurredColor += texture(screenTexture, uv + offset);
        }
    }
    blurredColor /= 25.0;

    // Combine blurred edges and original color
    vec4 screenColor = mix(originalColor, blurredColor, blurFactor);

    // Simulate sweat drops
    float drip1 = sweatDrip(uv, 0.3, 0.02, 0.2);
    float drip2 = sweatDrip(uv, 0.6, 0.015, 0.3);
    float drip3 = sweatDrip(uv, 0.9, 0.025, 0.25);

    float sweatOverlay = max(drip1, max(drip2, drip3));

    // Tint sweat drops
    vec4 sweatColor = vec4(0.8, 0.8, 1.0, 1.0) * sweatOverlay;

    // Combine sweat drops and screen
    outColor = mix(screenColor, sweatColor, sweatOverlay);
}
