shader_type canvas_item;

uniform sampler2D noise_sampler;
uniform float median_direction;
uniform float sway_strength = 0.1;
uniform float speed = 1.0;

varying flat vec2 vertexPos;

void vertex() {
	vertexPos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	float motion = texture(noise_sampler, vec2(sin(TIME * speed) / 2.0 + 0.5, 0.0)).x * 2.0 - 1.0;
	vec2 uv = UV + vec2((motion * sway_strength - median_direction) * pow(1.0 - UV.y, 4.0), 0);
	COLOR = texture(TEXTURE, uv); //vec4(uv, 0.0, 1.0);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
