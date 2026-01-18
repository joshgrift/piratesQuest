shader_type spatial;

// INTERACTION ZONE RING SHADER
// Creates a cartoony pulsing ring effect for interaction areas.
// Apply this to a flat disc/cylinder mesh for best results.

// Render settings: disable backface culling so ring is visible from below too
render_mode unshaded, cull_disabled;

// === CONFIGURABLE PARAMETERS ===
// Change these in the Inspector to customize per interaction type!

// Main ring color - change this for different interaction types
// Examples: Gold for treasure, Blue for ports, Green for resources
uniform vec4 ring_color : source_color = vec4(1.0, 0.8, 0.0, 0.6);

// How thick the ring band is (0.0 to 0.5, where 0.5 = solid circle)
uniform float ring_thickness : hint_range(0.01, 0.5) = 0.08;

// Where the ring sits (0.0 = center, 1.0 = edge)
uniform float ring_position : hint_range(0.3, 1.0) = 0.85;

// How fast the pulse animation plays
uniform float pulse_speed : hint_range(0.5, 5.0) = 2.0;

// How much the ring "breathes" in size (0 = no pulse)
uniform float pulse_strength : hint_range(0.0, 0.3) = 0.1;

// Glow intensity for that cartoony pop
uniform float glow_strength : hint_range(0.0, 2.0) = 1.0;

// Pass UV coordinates to fragment shader
// UV = texture coordinates, ranging from (0,0) to (1,1) across the mesh
varying vec2 uv_coords;

void vertex() {
	// Store UV for use in fragment shader
	uv_coords = UV;
}

void fragment() {
	// === STEP 1: Calculate distance from center ===
	// UV coordinates go from 0 to 1, so center is at (0.5, 0.5)
	// We remap to -1 to 1 range so center is at (0, 0)
	vec2 centered_uv = (uv_coords - 0.5) * 2.0;

	// length() gives us distance from origin (center)
	// This will be 0 at center, 1 at the edge of a unit circle
	// NOTE: We're on a square plane, so corners are at distance √2 ≈ 1.414
	// We keep our ring within the inscribed circle (radius 1.0) to avoid clipping
	float dist_from_center = length(centered_uv);

	// Discard pixels outside the unit circle to avoid square edges
	// This makes our square plane appear circular!
	if (dist_from_center > 1.0) {
		discard;
	}

	// === STEP 2: Create pulsing animation ===
	// sin(TIME * speed) oscillates between -1 and 1
	// We remap to 0 to 1 range, then scale by pulse_strength
	float pulse = sin(TIME * pulse_speed) * 0.5 + 0.5;  // 0 to 1
	float pulse_offset = pulse * pulse_strength;

	// === STEP 3: Define the ring shape ===
	// The ring exists between an inner and outer radius
	float inner_edge = ring_position - ring_thickness - pulse_offset;
	float outer_edge = ring_position + ring_thickness + pulse_offset;

	// smoothstep creates soft edges (anti-aliasing)
	// First smoothstep: fade in from inner edge
	// Second smoothstep: fade out at outer edge
	float ring_alpha = smoothstep(inner_edge - 0.02, inner_edge + 0.02, dist_from_center);
	ring_alpha *= 1.0 - smoothstep(outer_edge - 0.02, outer_edge + 0.02, dist_from_center);

	// === STEP 4: Add a secondary "glow" ring for cartoony effect ===
	// This is a softer, wider ring behind the main one
	float glow_inner = inner_edge - 0.1;
	float glow_outer = outer_edge + 0.1;
	float glow_alpha = smoothstep(glow_inner - 0.05, glow_inner + 0.1, dist_from_center);
	glow_alpha *= 1.0 - smoothstep(glow_outer - 0.1, glow_outer + 0.05, dist_from_center);
	glow_alpha *= 0.3;  // Make glow subtle

	// === STEP 5: Combine main ring and glow ===
	float final_alpha = max(ring_alpha, glow_alpha);

	// Discard fully transparent pixels for performance
	if (final_alpha < 0.01) {
		discard;
	}

	// === STEP 6: Add brightness pulse for extra "pop" ===
	// The ring gets brighter at the peak of the pulse
	float brightness_pulse = 1.0 + pulse * 0.3;

	// === STEP 7: Set final color ===
	// ALBEDO = base color (affected by lighting, but we're unshaded)
	ALBEDO = ring_color.rgb * brightness_pulse;

	// EMISSION = self-illumination, makes it glow and ignores scene lighting
	// This is key for the cartoony "always visible" look!
	EMISSION = ring_color.rgb * glow_strength * brightness_pulse;

	// Final transparency
	ALPHA = final_alpha * ring_color.a;
}

