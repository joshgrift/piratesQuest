shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled;

// Synty-style water shader adapted from:
// https://godotshaders.com/shader/synty-core-drop-in-water-shader/
//
// This keeps the nicer stylized look from the original shader, but avoids the
// project-wide global uniforms that were making the setup fragile in the editor.

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

group_uniforms General;
uniform float gale_strength = 0.8;
uniform vec3 wind_direction = vec3(1.0, 0.0, 0.25);
uniform sampler2D ocean_waves_gradient : source_color, filter_linear_mipmap;

group_uniforms Base;
uniform float smoothness : hint_range(0.0, 1.0) = 0.82;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float base_opacity : hint_range(0.0, 1.0) = 0.92;
uniform float shallows_opacity : hint_range(0.0, 1.0) = 0.65;
uniform vec4 shallow_color : source_color = vec4(0.25, 0.64, 0.68, 1.0);
uniform vec4 deep_color : source_color = vec4(0.06, 0.34, 0.58, 1.0);
uniform vec4 very_deep_color : source_color = vec4(0.03, 0.12, 0.24, 1.0);
uniform float deep_height = 1.25;
uniform float very_deep_height = 4.5;
uniform float maximum_depth = 15.0;

group_uniforms Base.Normals;
uniform bool enable_normals = false;
uniform sampler2D normal_texture : source_color, filter_linear_mipmap;
uniform vec2 normal_offset = vec2(0.0);
uniform float normal_tiling = 4.0;
uniform float normal_intensity : hint_range(0.0, 10.0) = 1.0;
uniform float normal_pan_speed = 1.0;
uniform float normal_noise_tiling = 0.2;
uniform float normal_noise_intensity = 0.23;

group_uniforms Base.FadeDistantNormals;
uniform bool enable_fresnel_fade = false;
uniform float fade_distance = 1.0;
uniform float fade_power = 0.0;

group_uniforms ShoreFoam;
uniform bool enable_shore_wave_foam = true;

group_uniforms ShoreFoam.Wave;
uniform bool enable_shore_animation = true;
uniform int animation_offset = 0;
uniform float shore_wave_speed : hint_range(0.0, 1.0) = 0.18;
uniform float shore_wave_return_amount : hint_range(0.0, 1.0) = 0.7;
uniform float shore_wave_tickness : hint_range(0.0, 1.0) = 0.12;
uniform float shore_edge_opacity : hint_range(0.0, 1.0) = 0.18;
uniform vec4 shore_wave_color_tint : source_color = vec4(0.85, 0.95, 1.0, 1.0);
uniform float shore_edge_tickness : hint_range(0.0, 1.0) = 0.2;
uniform float shore_edge_noise_scale = 1.5;
uniform float shore_lap_fade_out_speed : hint_range(0.0, 10.0) = 1.0;
uniform float shore_lap_fade_in_speed : hint_range(0.0, 10.0) = 3.0;
uniform int shore_foam_noise_scale : hint_range(0, 100, 1) = 12;
uniform sampler2D shore_foam_noise_texture : source_color, filter_linear_mipmap;

group_uniforms ShoreFoam.Foam;
uniform bool enable_shore_foam = true;
uniform float shore_small_foam_opacity : hint_range(0.0, 1.0) = 0.72;
uniform float shore_small_foam_tiling = 2.5;
uniform vec4 shore_foam_color_tint : source_color = vec4(0.9, 0.97, 1.0, 1.0);

group_uniforms GlobalFoam;
uniform bool enable_global_foam = true;
uniform sampler2D noise_texture : source_color, filter_linear_mipmap;

group_uniforms GlobalFoam.TopScrollingTexture;
uniform bool enable_top_scrolling_texture = true;
uniform vec2 top_scrolling_direction = vec2(0.01, -0.0075);
uniform sampler2D scrolling_texture : source_color, filter_linear_mipmap;
uniform vec2 scrolling_texture_tiling = vec2(1.8, 1.8);
uniform vec4 scrolling_texture_tint : source_color = vec4(0.2, 0.78, 0.92, 1.0);
uniform float scrolling_texture_opacity : hint_range(0.0, 1.0) = 0.18;

group_uniforms Waves;
uniform bool enable_ocean_waves = true;
uniform float ocean_wave_height : hint_range(0.0, 10.0) = 3.0;
uniform float ocean_wave_speed : hint_range(0.0, 1.0) = 0.12;
uniform float ocean_foam_amount : hint_range(0.0, 1.0) = 0.55;
uniform float ocean_foam_opacity : hint_range(0.0, 1.0) = 0.32;
uniform float ocean_wave_frequency : hint_range(1.0, 20.0) = 1.25;
uniform int ocean_breakup_tiling = 10;

group_uniforms Advanced;
group_uniforms Advanced.Caustics;
uniform bool enable_caustics = false;
uniform bool caustics_use_voronoi_noise = false;
uniform float caustics_intensity : hint_range(0.0, 1.0) = 0.5;
uniform vec4 caustics_color : source_color = vec4(1.0);
uniform sampler2D caustics_flipbook : source_color, filter_linear_mipmap;
uniform float caustics_speed : hint_range(0.0, 10.0) = 0.5;
uniform int caustics_scale = 1;

group_uniforms Advanced.Distorsion;
uniform bool enable_distortion = false;
uniform float distortion_speed : hint_range(0.0, 1.0) = 0.2;
uniform vec2 distortion_direction = vec2(1.0, 1.0);
uniform float distortion_size : hint_range(0.0, 1.0) = 0.1;
uniform float distortion_strength : hint_range(0.0, 1.0) = 0.5;

float saturate_value(float value) {
	return clamp(value, 0.0, 1.0);
}

vec2 unity_voronoi_noise_randomVector(vec2 uv, float offset) {
	mat2 m = mat2(vec2(15.27, 99.41), vec2(47.63, 89.98));
	vec2 temp = transpose(m) * uv;
	uv = fract(sin(temp) * 46839.32);
	return vec2(
		sin(uv.y * offset) * 0.5 + 0.5,
		cos(uv.x * offset) * 0.5 + 0.5
	);
}

vec2 Unity_Voronoi(vec2 uv, float angle_offset, float cell_density) {
	vec2 g = floor(uv * cell_density);
	vec2 f = fract(uv * cell_density);
	float best_distance = 8.0;
	vec3 best_result = vec3(best_distance, 0.0, 0.0);

	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 lattice = vec2(float(x), float(y));
			vec2 offset = unity_voronoi_noise_randomVector(lattice + g, angle_offset);
			vec2 point = lattice + offset - f;
			float distance_to_point = length(point);

			if (distance_to_point < best_result.x) {
				best_result = vec3(distance_to_point, offset.x, offset.y);
			}
		}
	}

	return vec2(best_result.x, best_result.y);
}

vec2 get_flipbook_uv(vec2 uv, int tile_width, int tile_height, int tile_index, bool invert_x, bool invert_y) {
	vec2 tile_size = vec2(1.0 / float(tile_width), 1.0 / float(tile_height));
	int h_tile = tile_index % tile_width;
	int v_tile = (tile_index / tile_width) % tile_height;

	if (invert_x) {
		h_tile = tile_width - h_tile - 1;
	}

	if (invert_y) {
		v_tile = tile_height - v_tile - 1;
	}

	vec2 tile_offset = vec2(
		float(h_tile) * tile_size.x,
		1.0 - (1.0 + float(v_tile)) * tile_size.y
	);

	return uv * tile_size + tile_offset;
}

float remap01(float input_value, float out_min, float out_max) {
	return out_min + input_value * (out_max - out_min);
}

vec4 top_down_projection(vec3 world_pos, vec3 world_normal, sampler2D tex, vec2 tile, vec2 off) {
	vec2 top_pos = (world_pos * vec3(1.0, 1.0, -1.0)).xz;
	vec2 normal_sign = vec2((sign(world_normal) * vec3(-1.0, 1.0, 1.0)).y, 1.0);
	vec2 projected_uv = (top_pos * normal_sign) * tile + off;
	return texture(tex, projected_uv);
}

vec2 unity_gradientNoise_dir(vec2 p) {
	p = mod(p, 289.0);
	float x = (34.0 * p.x + 1.0) * mod(p.x, 289.0) + p.y;
	x = (34.0 * x + 1.0) * mod(x, 289.0);
	x = fract(x / 41.0) * 2.0 - 1.0;
	return normalize(vec2(x - floor(x + 0.5), abs(x) - 0.5));
}

float unity_gradientNoise(vec2 p) {
	vec2 ip = floor(p);
	vec2 fp = fract(p);

	float d00 = dot(unity_gradientNoise_dir(ip), fp);
	float d01 = dot(unity_gradientNoise_dir(ip + vec2(0.0, 1.0)), fp - vec2(0.0, 1.0));
	float d10 = dot(unity_gradientNoise_dir(ip + vec2(1.0, 0.0)), fp - vec2(1.0, 0.0));
	float d11 = dot(unity_gradientNoise_dir(ip + vec2(1.0, 1.0)), fp - vec2(1.0, 1.0));

	fp = fp * fp * fp * (fp * (fp * 6.0 - 15.0) + 10.0);
	return mix(mix(d00, d01, fp.y), mix(d10, d11, fp.y), fp.x);
}

float unity_gradientNoise_scaled(vec2 uv, float scale) {
	return unity_gradientNoise(uv * scale) + 0.5;
}

vec2 panner(vec2 uv, float speed) {
	return uv + vec2(TIME * speed);
}

float scene_depth(vec2 screen_uv, mat4 inv_prj) {
	float raw_depth = texture(DEPTH_TEXTURE, screen_uv).r;
	vec3 ndc;

	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	ndc = vec3(screen_uv * 2.0 - 1.0, raw_depth * 2.0 - 1.0);
	#else
	ndc = vec3(screen_uv * 2.0 - 1.0, raw_depth);
	#endif

	vec4 view_pos = inv_prj * vec4(ndc, 1.0);
	view_pos.xyz /= view_pos.w;
	return -view_pos.z;
}

vec2 get_caustic_uv(vec3 camera_pos, vec3 camera_dir, vec3 view_dir, float depth) {
	vec3 view_module = view_dir / dot(view_dir, camera_dir);
	vec3 depth_vector = depth * view_module;
	return (depth_vector + camera_pos).xz;
}

vec4 water_caustics(vec3 camera_pos, vec3 camera_dir, vec3 view_dir, float eye_depth, float depth_fade) {
	vec2 caustic_color_uv = get_caustic_uv(camera_pos, camera_dir, view_dir, eye_depth);
	float caustic_value;

	if (caustics_use_voronoi_noise) {
		float value = Unity_Voronoi(
			caustic_color_uv / max(0.01, float(caustics_scale) / 2.0),
			(TIME * caustics_speed + 100.0) * 2.0,
			3.0
		).x;
		caustic_value = saturate_value(pow(value, 4.0));
	} else {
		vec2 scaled_in_uv = caustic_color_uv / max(0.01, float(caustics_scale));
		vec2 fract_part = vec2(fract(scaled_in_uv.x), fract(scaled_in_uv.y));
		int flipbook_idx = int(TIME * caustics_speed * 10.0);
		vec2 sample_uv = get_flipbook_uv(fract_part, 8, 8, flipbook_idx, false, true);
		caustic_value = texture(caustics_flipbook, sample_uv).r;
	}

	float saturated_depth = saturate_value(depth_fade * 0.1);
	float ocean_floor_fader = (1.0 - saturated_depth) * saturated_depth;
	vec4 faded_caustics = caustics_color * ocean_floor_fader * caustic_value;
	faded_caustics *= caustics_intensity * 10.0;
	float intensity = caustics_intensity + 1.0;

	return vec4(
		saturate_value(pow(faded_caustics.r, intensity)),
		saturate_value(pow(faded_caustics.g, intensity)),
		saturate_value(pow(faded_caustics.b, intensity)),
		saturate_value(pow(faded_caustics.a, intensity))
	);
}

vec4 water_shore_waves(vec3 position, vec3 normal, float depth) {
	vec4 foam_noise_raw = top_down_projection(
		position,
		normal,
		shore_foam_noise_texture,
		vec2(float(shore_foam_noise_scale) * 0.005),
		vec2(0.0)
	);
	float foam_noise = saturate_value(pow(foam_noise_raw.x, 0.2));

	vec4 shore_wave_raw = top_down_projection(
		position,
		normal,
		noise_texture,
		vec2(shore_edge_noise_scale * 0.005),
		vec2(0.0)
	);
	float shore_wave_oscillation = saturate_value(shore_wave_raw.r);

	float animation = enable_shore_animation ? TIME : float(animation_offset);
	float norm_shore_lap_time = fract(shore_wave_oscillation + shore_wave_speed * animation);
	float return_factor = PI / (2.0 - shore_wave_return_amount);
	float sin_lap = sin(norm_shore_lap_time * return_factor);
	float wave_lap = depth + sin_lap;
	float wave_motion = saturate_value(wave_lap * foam_noise);

	float fade_in = 1.0 - saturate_value(remap01(norm_shore_lap_time, -shore_lap_fade_in_speed, 1.0));
	float fade_out = 1.0 - saturate_value(remap01(norm_shore_lap_time, 1.0, -shore_lap_fade_out_speed));
	float lap_fade_speed = fade_in * fade_out;

	float rectified_thickness = shore_wave_tickness * 0.9999 * lap_fade_speed;
	float wave_dir = saturate_value(floor(wave_motion + rectified_thickness) - floor(wave_motion));
	float wave_factor = mix(0.0, wave_dir, shore_edge_opacity / 1.5);

	return shore_wave_color_tint * wave_factor;
}

vec4 water_shore_foam(vec3 position, vec3 normal, float depth) {
	float depth_factor = (1.0 - saturate_value(depth / 2.0)) * 0.8;
	float time_offset = TIME * 0.003;
	vec4 foam = top_down_projection(
		position,
		normal,
		noise_texture,
		vec2(shore_small_foam_tiling * 0.01),
		vec2(time_offset)
	);

	float effect_factor = saturate_value(pow((depth_factor * foam).r * 5.0, 20.0));
	float effect_mask = mix(0.0, effect_factor, shore_small_foam_opacity / 3.0);
	return shore_foam_color_tint * effect_mask;
}

vec3 water_distortion(vec2 uv, vec2 screen_pos) {
	vec2 distortion_velocity = distortion_direction * distortion_speed * 0.001;
	vec2 noise_source = uv + distortion_velocity * TIME;
	float noise = unity_gradientNoise_scaled(noise_source, distortion_size * 200.0);
	vec2 sampler_uv = noise * distortion_strength * 0.05 + screen_pos;
	sampler_uv += sin(TIME / 2.0 * (1.0 + distortion_speed * 2.0)) * distortion_strength / 100.0;
	return texture(SCREEN_TEXTURE, sampler_uv).xyz * 0.65;
}

vec4 water_top_scrolling(vec2 uv) {
	vec2 time_offset = 1.0 - top_scrolling_direction * TIME;
	vec4 sample_color = texture(scrolling_texture, uv * scrolling_texture_tiling + time_offset);
	return sample_color * scrolling_texture_tint * scrolling_texture_opacity;
}

vec3 water_normal(vec3 world_pos, vec3 world_normal) {
	vec2 tiling = vec2(normal_noise_tiling * 0.01);
	vec4 prj1 = top_down_projection(world_pos, world_normal, noise_texture, tiling, vec2(0.0));
	vec4 prj2 = top_down_projection(world_pos, world_normal, noise_texture, tiling * 0.5, vec2(0.0));
	vec2 normal_uv = vec2(prj1.r * prj2.r * normal_noise_intensity) + normal_offset;
	vec2 panned_uv = normal_uv + panner(vec2(0.0), normal_pan_speed * 0.02);
	float tiling_factor = normal_tiling * 0.04;

	vec4 prj_n1 = top_down_projection(world_pos, world_normal, normal_texture, vec2(tiling_factor), panned_uv);
	vec4 prj_n2 = top_down_projection(world_pos, world_normal, normal_texture, vec2(tiling_factor * 0.2), panned_uv * vec2(1.0, -1.0));

	return normalize(vec3(prj_n1.xy + prj_n2.xy, prj_n1.z * prj_n2.z));
}

float water_transparency(float shallows_depth, float depth_fade) {
	return 1.0 - remap01(saturate_value(depth_fade / maximum_depth), shallows_depth, 1.0);
}

float fresnel_mask(vec3 normal_ws, vec3 view_dir_ws) {
	float fresnel_effect = pow(1.0 - clamp(dot(normalize(normal_ws), normalize(view_dir_ws)), 0.0, 1.0), fade_distance);
	return smoothstep(0.0, 1.0, 1.0 - fresnel_effect * fade_power) * normal_intensity;
}

vec4 ocean_waves(vec2 uv, float speed, float height, vec3 direction, float gale) {
	vec2 pan_uv = uv + normalize(direction).xz * (TIME * speed);
	vec4 sample_color = texture(ocean_waves_gradient, pan_uv * ocean_wave_frequency);
	float wave_height_value = height * sample_color.z * (gale * 1.5);
	return vec4(0.0, wave_height_value, 0.0, sample_color.r);
}

varying float wave_z_offset;
varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

	if (enable_ocean_waves) {
		vec2 uv_world = (MODEL_MATRIX * vec4(UV.x, 0.0, UV.y, 1.0)).xz;
		vec4 wave_vertex = ocean_waves(
			uv_world,
			ocean_wave_speed,
			ocean_wave_height,
			wind_direction,
			gale_strength
		);

		wave_z_offset = wave_vertex.w;
		VERTEX += wave_vertex.xyz - vec3(0.0, ocean_wave_height / 2.0, 0.0);
	}
}

void fragment() {
	float effect_alpha_cover = 0.0;
	float fresnel = normal_intensity;

	vec3 view_dir_world = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
	vec3 normal_world = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);

	float frag_depth = 1.0 / FRAGCOORD.w;
	float bottom_depth = scene_depth(SCREEN_UV, INV_PROJECTION_MATRIX);
	float depth_fade = bottom_depth - frag_depth;

	float corrected_deep = pow(1.0 - saturate_value(depth_fade / abs(deep_height)), 1.8);
	float corrected_very_deep = pow(saturate_value(depth_fade / abs(very_deep_height)), 1.8);
	ALBEDO = mix(mix(deep_color, shallow_color, corrected_deep), very_deep_color, corrected_very_deep).xyz;

	if (enable_top_scrolling_texture) {
		ALBEDO += water_top_scrolling(UV).xyz;
	}

	if (enable_caustics) {
		ALBEDO += water_caustics(
			CAMERA_POSITION_WORLD,
			-CAMERA_DIRECTION_WORLD,
			view_dir_world,
			bottom_depth,
			depth_fade
		).xyz;
	}

	if (enable_shore_wave_foam) {
		float shore_edge = (1.0 - saturate_value(pow(depth_fade / shore_edge_tickness, 2.0))) * shore_edge_opacity;
		vec4 shore_edge_tint = shore_wave_color_tint * shore_edge;
		ALBEDO += water_shore_waves(world_pos, normal_world, depth_fade).xyz + shore_edge_tint.xyz;
		effect_alpha_cover += shore_edge_tint.r;
	}

	if (enable_shore_foam) {
		vec4 shore_foam = water_shore_foam(world_pos, normal_world, depth_fade);
		ALBEDO += shore_foam.xyz;
		effect_alpha_cover += shore_foam.r;
	}

	if (enable_global_foam || enable_normals) {
		fresnel = fresnel_mask(normal_world, view_dir_world);
	}

	if (enable_normals) {
		NORMAL = mix(vec3(0.0, 0.0, 1.0), water_normal(world_pos, normal_world), fresnel);
	} else {
		NORMAL = vec3(0.5, 0.5, 1.0);
	}

	if (enable_global_foam) {
		float foam = ocean_foam_amount;
		float reverse_foam = 5.0 - 4.0 * foam;
		float foam_amount = saturate_value(pow(wave_z_offset, reverse_foam)) * foam;
		float noise_sample = top_down_projection(world_pos, normal_world, noise_texture, vec2(0.02), vec2(0.0)).r;
		float noise_amount = saturate_value(noise_sample * foam_amount * 2.0);
		float time_factor = TIME * -0.002;
		float retiling = float(ocean_breakup_tiling) * 0.01;
		float tile_sample = top_down_projection(world_pos, normal_world, noise_texture, vec2(retiling), vec2(time_factor, 2.0)).r;

		float depth_factor = saturate_value(depth_fade * 0.05) * 0.18 * tile_sample * noise_amount;
		float depth_mult = saturate_value(pow(depth_factor * 20.0, 20.0));
		ALBEDO += depth_mult * ocean_foam_opacity * 3.0;
	}

	float alpha = saturate_value(base_opacity - water_transparency(shallows_opacity, depth_fade) + effect_alpha_cover);

	if (enable_distortion) {
		ALBEDO += mix(water_distortion(UV, SCREEN_UV), vec3(0.0), alpha);
	} else {
		ALPHA = alpha;
	}

	METALLIC = metallic;
	ROUGHNESS = 1.0 - smoothness;
	AO = 1.0;
}
