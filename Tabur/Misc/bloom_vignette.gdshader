shader_type canvas_item;

// Godot 4.x. Put this on a ColorRect that covers the screen / viewport.
uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear_mipmap;

group_uniforms bloom;
uniform float bloom_threshold : hint_range(0.0, 2.0) = 0.7;
uniform float bloom_soft_knee : hint_range(0.0, 1.0) = 0.5;
uniform float bloom_intensity : hint_range(0.0, 3.0) = 0.8;
uniform float bloom_radius : hint_range(1.0, 60.0) = 18.0; // in pixels
uniform float bloom_lod : hint_range(0.0, 6.0) = 2.0;      // higher = softer
uniform vec3 bloom_tint : source_color = vec3(1.0);

group_uniforms vignette;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.5;
uniform float vignette_radius : hint_range(0.0, 1.5) = 0.75;
uniform float vignette_softness : hint_range(0.01, 1.5) = 0.5;
uniform float vignette_roundness : hint_range(0.0, 1.0) = 1.0; // 1 = circular, 0 = follows screen shape
uniform vec3 vignette_color : source_color = vec3(0.0);

const int SAMPLES = 24;
const float GOLDEN_ANGLE = 2.39996323;

// Keep only the bright parts, with a soft transition.
vec3 prefilter(vec3 c) {
	float br = max(c.r, max(c.g, c.b));
	float knee = bloom_threshold * bloom_soft_knee + 1e-4;
	float soft = clamp(br - bloom_threshold + knee, 0.0, 2.0 * knee);
	soft = soft * soft / (4.0 * knee);
	float contrib = max(soft, br - bloom_threshold) / max(br, 1e-4);
	return c * contrib;
}

vec3 bloom(vec2 uv, vec2 pixel_size, float radius_px, float lod) {
	vec3 sum = vec3(0.0);
	for (int i = 0; i < SAMPLES; i++) {
		float a = float(i) * GOLDEN_ANGLE;
		float r = sqrt((float(i) + 0.5) / float(SAMPLES)) * radius_px;
		vec2 offset = vec2(cos(a), sin(a)) * r * pixel_size;
		sum += prefilter(textureLod(screen_tex, uv + offset, lod).rgb);
	}
	return sum / float(SAMPLES);
}

void fragment() {
	vec3 base = textureLod(screen_tex, SCREEN_UV, 0.0).rgb;

	// Two scales blended together gives a nicer glow than a single blur.
	vec3 glow = bloom(SCREEN_UV, SCREEN_PIXEL_SIZE, bloom_radius, bloom_lod) * 0.6
			  + bloom(SCREEN_UV, SCREEN_PIXEL_SIZE, bloom_radius * 2.5, bloom_lod + 1.5) * 0.4;
	vec3 col = base + glow * bloom_intensity * bloom_tint;

	// Vignette
	vec2 p = SCREEN_UV - 0.5;
	float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x; // width / height
	p.x *= mix(1.0, aspect, vignette_roundness);
	float d = length(p) * 2.0;
	float v = smoothstep(vignette_radius, vignette_radius + vignette_softness, d);
	col = mix(col, vignette_color, v * vignette_intensity);

	COLOR = vec4(col, 1.0);
}
