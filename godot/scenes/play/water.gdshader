shader_type spatial;

// This shader creates animated water by displacing vertices using noise
// The noise texture provides the wave pattern
uniform sampler2D noise_img;
uniform float wave_height = 1.0;      // How tall the waves are
uniform float wave_speed = 0.0000002;    // How fast waves move (slower for ocean feel)
uniform vec4 water_color : source_color = vec4(0.0, 0.4, 0.8, 1.0);
uniform vec4 wave_color : source_color = vec4(0.3, 0.6, 1.0, 1.0);  // Lighter blue for wave peaks

varying float height_current;
varying vec2 uv_coords;  // Pass UV to fragment shader for texture detail

void vertex() {
	// Move the noise sampling over time to create wave animation
	// TIME is a built-in Godot variable that counts seconds
	vec2 uv_movement = vec2(TIME * wave_speed, 0.0);
	uv_coords = UV + uv_movement;  // Store for fragment shader

	// Sample the noise texture at current UV + time offset
	// This reads a grayscale value (0.0 to 1.0) from the texture
	float tex_height = texture(noise_img, uv_coords).r;
	height_current = tex_height;

	// Displace the vertex upward based on noise value
	// This creates the wave shapes by moving vertices up and down
	VERTEX.y += tex_height * wave_height;
}

void fragment() {
	// Sample the noise texture at a smaller scale for surface detail
	float detail = texture(noise_img, uv_coords * 3.0).r;

	// Add green tint variation based on detail for texture
	// Creates patches of greenish-blue water
	vec3 green_tint = vec3(0.1, 0.3, 0.2);  // Subtle green
	float green_amount = detail * 0.3;  // 0 to 0.3 green influence

	// Blend water color based on wave height for depth
	// Higher waves are lighter, lower waves are darker with green tint
	vec3 deep_color = mix(water_color.rgb * 0.7, water_color.rgb * 0.7 + green_tint, green_amount);
	vec3 color_variation = mix(deep_color, wave_color.rgb, height_current);

	// Add white foam on the highest wave peaks
	float foam_start = 0.80;
	float foam_end = 0.90;
	float foam_blend = smoothstep(foam_start, foam_end, height_current);
	vec3 foam_color = vec3(1.0, 1.0, 1.0);  // White foam

	vec3 final_color = mix(color_variation, foam_color, foam_blend);

	ALBEDO = final_color;

	// Reduce metallic for less mirror-like reflections
	METALLIC = 0.2;

	// Higher roughness makes it less shiny/reflective
	ROUGHNESS = 0.1;

	// Slight transparency
	ALPHA = 0.9;
}
