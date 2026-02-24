shader_type spatial;

// COMPASS PROGRESS INDICATOR
// A golden compass rose that fills clockwise as resources are collected.
// Floats above collection points and always faces the camera (billboard).
//
// Visual layers (back to front):
//   1. Dark translucent ring (track)
//   2. Golden progress arc that fills clockwise from the top
//   3. Compass tick marks at 8 compass directions
//   4. Diamond markers at the 4 cardinal points
//   5. Thin decorative inner ring
//   6. 8-pointed compass rose in the center
//   7. Animated shimmer, leading-edge glow, and outer aura

render_mode unshaded, cull_disabled, blend_mix;

// ===== UNIFORMS (adjustable from Inspector or C#) =====

// How full the ring is: 0.0 = empty, 1.0 = fully collected
uniform float progress : hint_range(0.0, 1.0) = 0.0;

// Pirate gold palette — matches the game's UI colors
uniform vec4 gold : source_color = vec4(0.83, 0.66, 0.26, 1.0);
uniform vec4 gold_light : source_color = vec4(0.91, 0.84, 0.64, 1.0);
uniform vec4 bg_tint : source_color = vec4(0.04, 0.09, 0.17, 0.55);

// Ring geometry (in UV space where the full quad = -1 to 1)
uniform float outer_radius : hint_range(0.2, 0.5) = 0.44;
uniform float ring_width : hint_range(0.02, 0.15) = 0.065;


void vertex() {
	// === BILLBOARD EFFECT ===
	// Extract uniform scale from the model's X basis so the compass stays
	// perfectly circular even if the parent node has non-uniform scale
	// (collection points in the world are scaled like 0.5, 1, 0.5).
	float s = length(MODEL_MATRIX[0].xyz);

	// Build a model-view matrix that replaces the model's rotation with
	// the camera's orientation. INV_VIEW_MATRIX columns are the camera's
	// right / up / forward axes in world space. MODEL_MATRIX[3] keeps
	// the original world position so the compass stays in place.
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		vec4(INV_VIEW_MATRIX[0].xyz * s, 0.0),
		vec4(INV_VIEW_MATRIX[1].xyz * s, 0.0),
		vec4(INV_VIEW_MATRIX[2].xyz * s, 0.0),
		MODEL_MATRIX[3]
	);
}


void fragment() {
	// === UV SETUP ===
	// Remap from [0,1] to [-1,1] so the center of the quad is (0,0).
	vec2 uv = UV * 2.0 - 1.0;
	float dist = length(uv);

	// === ANGLE (clockwise from top, 0–1) ===
	// atan(x, -y) puts 0 at the top of the circle.
	// We normalize to [0,1] and shift so the start aligns with 12-o'clock.
	float raw_angle = atan(uv.x, -uv.y);
	float norm_a = fract((raw_angle + PI) / (2.0 * PI) + 0.5);

	float inner_r = outer_radius - ring_width;
	float aa = 0.008; // anti-aliasing softness


	// ========== 1. MAIN RING (progress track) ==========
	// A slight sine wave on the edges mimics hand-twisted rope.
	float rope = sin(norm_a * 48.0) * 0.002;
	float ring = smoothstep(inner_r - aa + rope, inner_r + aa + rope, dist)
	           * (1.0 - smoothstep(outer_radius - aa - rope, outer_radius + aa - rope, dist));


	// ========== 2. PROGRESS ARC ==========
	// Everything from the top (0) to the current progress value is "filled."
	float fill = smoothstep(progress + aa, progress - aa, norm_a);
	fill *= step(0.003, progress); // force zero when progress is basically 0


	// ========== 3. COMPASS TICK MARKS ==========
	// 4 big ticks at N/E/S/W + 4 smaller ticks at NE/SE/SW/NW,
	// extending outward from the ring like a real compass bezel.
	float big_len = 0.032;
	float small_len = 0.016;
	float tick_w = 0.009;

	float ticks = 0.0;
	for (int i = 0; i < 8; i++) {
		float tick_pos = float(i) * 0.125;
		float d = abs(norm_a - tick_pos);
		d = min(d, 1.0 - d); // wrap around the circle seam

		bool is_big = (i % 2 == 0);
		float len = is_big ? big_len : small_len;
		float w = is_big ? tick_w : tick_w * 0.6;

		float t = smoothstep(outer_radius - 0.004, outer_radius + 0.002, dist)
		        * (1.0 - smoothstep(outer_radius + len - 0.002, outer_radius + len + 0.002, dist));
		ticks += smoothstep(w, w * 0.3, d) * t;
	}
	ticks = clamp(ticks, 0.0, 1.0);


	// ========== 4. DIAMOND MARKERS (cardinal points) ==========
	// Small diamond shapes sitting inside the ring at N/E/S/W.
	float diamonds = 0.0;
	float mid_r = (inner_r + outer_radius) * 0.5;
	for (int i = 0; i < 4; i++) {
		float d_pos = float(i) * 0.25;
		float d = abs(norm_a - d_pos);
		d = min(d, 1.0 - d);
		float r_d = abs(dist - mid_r);
		diamonds += smoothstep(0.016, 0.006, d)
		          * smoothstep(ring_width * 0.35, ring_width * 0.12, r_d);
	}
	diamonds = clamp(diamonds, 0.0, 1.0);


	// ========== 5. COMPASS ROSE (8-pointed star) ==========
	// The iconic nautical star. It slowly rotates for a magical feel
	// and brightens as progress increases.
	float rose_r = inner_r - 0.05;
	float rose_spin = TIME * 0.25;
	float star_a = atan(uv.x, uv.y) + rose_spin;

	// Primary 4-pointed star (tall N/S and E/W arms)
	float s1 = abs(cos(star_a * 2.0));
	float s1_r = rose_r * (0.2 + s1 * 0.8);
	float star1 = 1.0 - smoothstep(s1_r - aa, s1_r + aa, dist);

	// Secondary 4-pointed star rotated 45° (shorter diagonal arms)
	float s2 = abs(sin(star_a * 2.0));
	float s2_r = rose_r * 0.55 * (0.2 + s2 * 0.8);
	float star2 = 1.0 - smoothstep(s2_r - aa, s2_r + aa, dist);

	// Center jewel with a gentle pulse
	float jewel_pulse = sin(TIME * 1.5) * 0.25 + 0.75;
	float jewel = (1.0 - smoothstep(0.02, 0.03, dist)) * jewel_pulse;

	float compass = max(max(star1, star2 * 0.7), jewel);


	// ========== 6. THIN DECORATIVE INNER RING ==========
	// A hairline ring separating the compass rose from the progress track.
	float deco_r = inner_r - 0.015;
	float deco_ring = smoothstep(deco_r - 0.003, deco_r, dist)
	                * (1.0 - smoothstep(deco_r + 0.002, deco_r + 0.005, dist));


	// ========== 7. ANIMATED EFFECTS ==========

	// Leading-edge glow — a bright flare at the progress boundary
	float edge_d = abs(norm_a - progress);
	edge_d = min(edge_d, 1.0 - edge_d);
	float edge_glow = exp(-edge_d * 55.0) * step(0.003, progress) * 1.8;

	// Shimmer — gold sparkles rippling along the filled arc
	float t = TIME * 3.0;
	float shimmer = sin(norm_a * 32.0 + t) * sin(norm_a * 53.0 - t * 0.7);
	shimmer = max(0.0, shimmer) * fill * ring * 0.3;

	// Outer aura — soft warm glow that grows with progress
	float breath = sin(TIME * 2.0) * 0.5 + 0.5;
	float aura = (1.0 - smoothstep(outer_radius + 0.01, outer_radius + 0.06, dist))
	           * smoothstep(outer_radius - ring_width, outer_radius, dist)
	           * progress * 0.2
	           * (0.7 + breath * 0.3);


	// ========== COMPOSITE ALL LAYERS ==========
	vec3 final_col = vec3(0.0);
	float final_a = 0.0;
	vec3 emission = vec3(0.0);

	// 1 — Background ring (dark translucent track)
	final_col = bg_tint.rgb;
	final_a = ring * bg_tint.a;

	// 2 — Progress fill (golden arc)
	vec3 fill_col = mix(gold.rgb, gold_light.rgb, shimmer + edge_glow * 0.3);
	float fill_a = ring * fill;
	final_col = mix(final_col, fill_col, fill_a);
	final_a = max(final_a, fill_a);
	emission += fill_col * (edge_glow * 2.0 + shimmer * 0.5) * ring;

	// 3 — Diamond markers (glow gold when the fill reaches them)
	vec3 dia_col = mix(bg_tint.rgb * 2.0, gold_light.rgb, fill);
	final_col = mix(final_col, dia_col, diamonds * 0.8);
	final_a = max(final_a, diamonds * 0.6);
	emission += dia_col * diamonds * fill * 0.3;

	// 4 — Tick marks (transition from dark to gold as the fill passes)
	vec3 tick_col = mix(bg_tint.rgb * 1.6, gold.rgb, fill);
	final_col = mix(final_col, tick_col, ticks * 0.85);
	final_a = max(final_a, ticks * 0.6);

	// 5 — Decorative inner ring
	vec3 deco_col = mix(bg_tint.rgb * 1.3, gold.rgb * 0.7, progress);
	final_col = mix(final_col, deco_col, deco_ring * 0.5);
	final_a = max(final_a, deco_ring * 0.35);

	// 6 — Compass rose (brightens as collection nears completion)
	float rose_glow = 0.2 + progress * 0.8;
	vec3 rose_col = mix(bg_tint.rgb * 1.5, gold_light.rgb, rose_glow);
	final_col = mix(final_col, rose_col, compass * 0.65);
	final_a = max(final_a, compass * 0.45);
	emission += rose_col * compass * progress * 0.4;

	// 7 — Outer aura
	emission += gold.rgb * aura;
	final_a = max(final_a, aura * 0.4);

	// Discard fully transparent pixels for GPU performance
	if (final_a < 0.01) discard;

	ALBEDO = final_col;
	EMISSION = emission;
	ALPHA = final_a;
}
