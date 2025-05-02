extends Camera2D

var _zoom_tween: Tween

const _min_zoom := 0.25
const _max_zoom := 2.0
const _zoom_duration := 0.2
const _zoom_factor := 0.1

var _zoom_level := 1.0
var zoom_level: float:
	get: return _zoom_level
	set(value):
		if value != _zoom_level:
			_zoom_level = clampf(value, _min_zoom, _max_zoom)
			
			if _zoom_tween:
				_zoom_tween.kill()
			_zoom_tween = create_tween()
			
			_zoom_tween.tween_property(self, "zoom", Vector2(zoom_level, zoom_level), _zoom_duration) \
				.set_trans(Tween.TransitionType.TRANS_SINE) \
				.set_ease(Tween.EaseType.EASE_OUT)

func _process(delta: float) -> void:
	var delta_movement := Input.get_vector("camera_left", "camera_right", "camera_up", "camera_down")
	position += delta_movement * 1000 * delta
	
	var delta_zoom := Input.get_axis("camera_zoom_out", "camera_zoom_in")
	if delta_zoom != 0:
		zoom += Vector2(delta_zoom, delta_zoom) * 0.1 * delta
		zoom = clamp(zoom, Vector2(0.25, 0.25), Vector2(2, 2))

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("camera_zoom_in"):
		zoom_level += _zoom_factor
	elif event.is_action_pressed("camera_zoom_out"):
		zoom_level -= _zoom_factor
