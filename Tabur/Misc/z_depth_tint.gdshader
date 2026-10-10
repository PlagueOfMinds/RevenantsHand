shader_type canvas_item;
render_mode blend_mix;

// Depth-based tint + blur for 2D sprites (Godot 4.x).
// Depth t = 0 -> "back" (depth_min / lowest z), t = 1 -> "front" (depth_max / highest z).

const int MAX_BLUR_SAMPLES = 64;
const float GOLDEN_ANGLE = 2.39996323;

varying vec4 v_modulate;

// ---------------------------------------------------------------- Depth
group_uniforms Depth;
// Set by the ZDepthTint script (or manually). Usually the sprite's z_index.
uniform float depth_value = 0.0;
uniform float depth_min = -10.0;   // depth treated as "back"
uniform float depth_max = 10.0;    // depth treated as "front"
uniform bool invert_depth = false; // swap back/front
uniform float depth_curve : hint_range(0.05, 8.0, 0.01) = 1.0; // 1 = linear
uniform bool smooth_depth = false; // smoothstep the blend factor

// ---------------------------------------------------------------- Tint
group_uniforms Tint;
uniform bool tint_enabled = true;
// 0 Mix, 1 Multiply, 2 Add, 3 Screen, 4 Overlay
uniform int tint_mode : hint_range(0, 4, 1) = 0;
// Color alpha scales the strength.
uniform vec4 tint_color_back : source_color = vec4(0.35, 0.45, 0.75, 1.0);
uniform vec4 tint_color_front : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float tint_strength_back : hint_range(0.0, 1.0, 0.01) = 0.6;
uniform float tint_strength_front : hint_range(0.0, 1.0, 0.01) = 0.0;
// Optional: sample tint color from a gradient texture instead (x = depth 0..1).
uniform bool use_tint_gradient = false;
uniform sampler2D tint_gradient : source_color, hint_default_white, filter_linear, repeat_disable;

// ---------------------------------------------------------------- Color adjustment
group_uniforms Color_Adjust;
uniform float brightness_back : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float brightness_front : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float saturation_back : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float saturation_front : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float contrast_back : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float contrast_front : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float opacity_back : hint_range(0.0, 1.0, 0.01) = 1.0;
uniform float opacity_front : hint_range(0.0, 1.0, 0.01) = 1.0;

// ---------------------------------------------------------------- Blur
group_uniforms Blur;
uniform bool blur_enabled = true;
// 0 = Gradient (blur_radius_back -> blur_radius_front by depth)
// 1 = Focus (depth-of-field: sharp at blur_focus_depth, blurrier away from it)
uniform int blur_mode : hint_range(0, 1, 1) = 0;
// Radii are in texture pixels.
uniform float blur_radius_back : hint_range(0.0, 64.0, 0.1) = 2.0;
uniform float blur_radius_front : hint_range(0.0, 64.0, 0.1) = 0.0;
// Focus mode settings (in the same units as depth_value).
uniform float blur_focus_depth = 0.0;
uniform float blur_focus_range : hint_range(0.0, 100.0, 0.01) = 0.0;    // fully sharp zone around focus
uniform float blur_focus_falloff : hint_range(0.01, 100.0, 0.01) = 5.0; // distance to reach max blur
uniform float blur_focus_max_radius : hint_range(0.0, 64.0, 0.1) = 4.0;
// Quality / shape
uniform int blur_sample_count : hint_range(1, 64, 1) = 16;
uniform vec2 blur_scale = vec2(1.0, 1.0);   // anisotropic blur (e.g. (1, 0) = horizontal only)
uniform float blur_rotation : hint_range(-3.14159, 3.14159, 0.01) = 0.0;
uniform float blur_falloff : hint_range(0.0, 1.0, 0.01) = 0.5; // 0 = flat disc, 1 = soft edge
uniform bool blur_affects_alpha = true;     // false keeps the original silhouette

void vertex() {
	// In vertex(), COLOR holds the node's modulate / self_modulate.
	v_modulate = COLOR;
}

void fragment() {
	// ---------- depth factor
	float depth_span = max(depth_max - depth_min, 0.0001);
	float t = clamp((depth_value - depth_min) / depth_span, 0.0, 1.0);
	if (invert_depth) {
		t = 1.0 - t;
	}
	t = pow(t, depth_curve);
	if (smooth_depth) {
		t = smoothstep(0.0, 1.0, t);
	}

	vec4 base = texture(TEXTURE, UV);
	vec4 col = base;

	// ---------- blur radius
	float blur_px = 0.0;
	if (blur_enabled) {
		if (blur_mode == 0) {
			blur_px = mix(blur_radius_back, blur_radius_front, t);
		} else {
			float dist = abs(depth_value - blur_focus_depth);
			float f = clamp((dist - blur_focus_range) / max(blur_focus_falloff, 0.0001), 0.0, 1.0);
			blur_px = blur_focus_max_radius * f;
		}
	}

	// ---------- blur (golden-angle disc sampling, alpha-weighted)
	if (blur_px > 0.001) {
		vec2 radius_uv = TEXTURE_PIXEL_SIZE * blur_scale * blur_px;
		int count = clamp(blur_sample_count, 1, MAX_BLUR_SAMPLES);

		vec3 rgb_sum = base.rgb * base.a;
		float a_sum = base.a;
		float w_sum = 1.0;

		for (int i = 0; i < MAX_BLUR_SAMPLES; i++) {
			if (i >= count) {
				break;
			}
			float r = sqrt((float(i) + 0.5) / float(count));
			float ang = float(i) * GOLDEN_ANGLE + blur_rotation;
			vec2 suv = UV + vec2(cos(ang), sin(ang)) * r * radius_uv;

			vec4 s = textureLod(TEXTURE, suv, 0.0);
			// Treat anything outside the texture as transparent (no edge smearing).
			if (suv.x < 0.0 || suv.y < 0.0 || suv.x > 1.0 || suv.y > 1.0) {
				s = vec4(0.0);
			}

			float w = mix(1.0, 1.0 - r * r, blur_falloff);
			rgb_sum += s.rgb * s.a * w;
			a_sum += s.a * w;
			w_sum += w;
		}

		vec3 rgb_blur = base.rgb;
		if (a_sum > 0.00001) {
			rgb_blur = rgb_sum / a_sum;
		}
		float a_blur = base.a;
		if (blur_affects_alpha) {
			a_blur = a_sum / w_sum;
		}
		col = vec4(rgb_blur, a_blur);
	}

	// ---------- color adjustments
	vec3 rgb = col.rgb * mix(brightness_back, brightness_front, t);
	float luma = dot(rgb, vec3(0.2126, 0.7152, 0.0722));
	rgb = mix(vec3(luma), rgb, mix(saturation_back, saturation_front, t));
	rgb = (rgb - vec3(0.5)) * mix(contrast_back, contrast_front, t) + vec3(0.5);
	rgb = max(rgb, vec3(0.0));

	// ---------- tint
	if (tint_enabled) {
		vec3 tc;
		float ta;
		if (use_tint_gradient) {
			vec4 g = textureLod(tint_gradient, vec2(t, 0.5), 0.0);
			tc = g.rgb;
			ta = g.a;
		} else {
			vec4 c4 = mix(tint_color_back, tint_color_front, t);
			tc = c4.rgb;
			ta = c4.a;
		}
		float strength = clamp(mix(tint_strength_back, tint_strength_front, t) * ta, 0.0, 1.0);

		vec3 tinted;
		if (tint_mode == 0) {
			tinted = tc;                                                    // Mix
		} else if (tint_mode == 1) {
			tinted = rgb * tc;                                              // Multiply
		} else if (tint_mode == 2) {
			tinted = rgb + tc;                                              // Add
		} else if (tint_mode == 3) {
			tinted = vec3(1.0) - (vec3(1.0) - rgb) * (vec3(1.0) - tc);      // Screen
		} else {
			vec3 lo = 2.0 * rgb * tc;                                       // Overlay
			vec3 hi = vec3(1.0) - 2.0 * (vec3(1.0) - rgb) * (vec3(1.0) - tc);
			tinted = mix(lo, hi, step(vec3(0.5), rgb));
		}
		rgb = mix(rgb, tinted, strength);
	}

	float alpha = col.a * mix(opacity_back, opacity_front, t);
	COLOR = vec4(rgb, alpha) * v_modulate;
}