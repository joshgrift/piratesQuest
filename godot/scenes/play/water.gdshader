shader_type spatial;

// This shader creates animated water by displacing vertices using noise.
// Two wave layers move in different directions for a more natural ocean look.
uniform sampler2D noise_img;
uniform float wave_height = 1.0;
uniform float wave_speed = 0.0000002;
uniform vec4 water_color : source_color = vec4(0.1, 0.55, 0.95, 1.0);
uniform vec4 wave_color : source_color = vec4(0.45, 0.75, 1.0, 1.0);

// Controls how bumpy the surface looks when lit (higher = more ripple detail)
uniform float normal_strength = 0.8;

varying float height_current;
varying vec2 uv_coords;
varying vec2 uv_coords2;  // Second wave layer for visual complexity

void vertex() {
	// Primary wave layer moves along X
	vec2 uv_movement = vec2(TIME * wave_speed, 0.0);
	uv_coords = UV + uv_movement;

	// Secondary wave layer moves diagonally at a different speed.
	// Two overlapping wave patterns break up the repeating look.
	vec2 uv_movement2 = vec2(TIME * wave_speed * 0.7, TIME * wave_speed * 0.4);
	uv_coords2 = UV * 1.4 + uv_movement2;

	// Blend both noise samples for the vertex displacement
	float tex_height1 = texture(noise_img, uv_coords).r;
	float tex_height2 = texture(noise_img, uv_coords2).r;
	height_current = mix(tex_height1, tex_height2, 0.4);

	VERTEX.y += height_current * wave_height;
}

void fragment() {
	// --- Surface normals from noise ---
	// Sample tiny offsets around the current point to figure out the slope.
	// This makes the surface scatter light like real ripples would.
	float pixel_size = 0.002;  // Distance between samples

	// Four neighboring noise samples (up, down, left, right)
	float h_left  = texture(noise_img, uv_coords + vec2(-pixel_size, 0.0)).r;
	float h_right = texture(noise_img, uv_coords + vec2( pixel_size, 0.0)).r;
	float h_up    = texture(noise_img, uv_coords + vec2(0.0, -pixel_size)).r;
	float h_down  = texture(noise_img, uv_coords + vec2(0.0,  pixel_size)).r;

	// Second layer normals for extra ripple detail
	float h2_left  = texture(noise_img, uv_coords2 + vec2(-pixel_size, 0.0)).r;
	float h2_right = texture(noise_img, uv_coords2 + vec2( pixel_size, 0.0)).r;
	float h2_up    = texture(noise_img, uv_coords2 + vec2(0.0, -pixel_size)).r;
	float h2_down  = texture(noise_img, uv_coords2 + vec2(0.0,  pixel_size)).r;

	// Combine both layers into one slope value per axis
	float dx = (h_right - h_left) + 0.5 * (h2_right - h2_left);
	float dy = (h_down  - h_up)   + 0.5 * (h2_down  - h2_up);

	// Build a normal vector from the slopes. The "1.0 / normal_strength" term
	// controls how much the surface reacts to the slopes — lower values = bumpier.
	vec3 surface_normal = normalize(vec3(-dx * normal_strength, 1.0, -dy * normal_strength));
	NORMAL_MAP = surface_normal * 0.5 + 0.5;  // Encode to 0-1 range for Godot

	// --- Color ---
	// Fine detail at 3x scale for close-up texture
	float detail_fine = texture(noise_img, uv_coords * 3.0).r;
	// Broad detail at 0.8x scale for large color patches
	float detail_broad = texture(noise_img, uv_coords2 * 0.8).r;

	// Greenish-blue patches that shift with the broad detail
	vec3 green_tint = vec3(0.05, 0.2, 0.15);
	float green_amount = detail_broad * 0.35;

	// Brighter base — troughs are lighter, peaks brighter still
	vec3 deep_color = mix(water_color.rgb, water_color.rgb + green_tint, green_amount);
	vec3 color_variation = mix(deep_color, wave_color.rgb * 1.2, height_current);

	// Fine-scale variation to break up flat areas
	color_variation *= 0.95 + detail_fine * 0.08;

	// White foam on wave crests
	float foam_blend = smoothstep(0.78, 0.92, height_current);
	// Foam is patchy, not solid — use fine detail to break it up
	foam_blend *= smoothstep(0.3, 0.6, detail_fine);
	vec3 foam_color = vec3(0.95, 0.97, 1.0);

	vec3 final_color = mix(color_variation, foam_color, foam_blend);

	ALBEDO = final_color;

	// Low metallic — water isn't metal, it just reflects a bit
	METALLIC = 0.05;

	// Higher roughness = less mirror-shiny, more of a matte ocean look.
	// Foam patches are even rougher since they're bubbly.
	ROUGHNESS = mix(0.55, 0.85, foam_blend);

	ALPHA = 0.96;
}
