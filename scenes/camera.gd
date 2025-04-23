extends Camera2D

func _process(delta: float) -> void:
	
	var delta_movement := Input.get_vector("camera_left", "camera_right", "camera_up", "camera_down")
	position += delta_movement * 1000 * delta
