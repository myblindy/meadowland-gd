extends Node2D

func _ready() -> void:
	_on_calm_weather_button_pressed()
	
	%RainAudioStreamPlayer.play()
	%RainAudioStreamPlayer.stream_paused = true
	
	%WindAudioStreamPlayer.play()
	%WindAudioStreamPlayer.stream_paused = true

func _on_calm_weather_button_pressed() -> void:
	$TileMapPlantLayer2.material.set_shader_parameter("median_direction", 0.0)
	$TileMapPlantLayer2.material.set_shader_parameter("sway_strength", 0.005)
	$TileMapPlantLayer2.material.set_shader_parameter("speed", 0.04)
	%RainParticles.process_material.direction = Vector3(0, 1, 0)
	%RainParticles.process_material.angle_min = 0
	%RainParticles.process_material.angle_max = 0
	%WindAudioStreamPlayer.stream_paused = true

func _on_windy_left_button_pressed() -> void:
	$TileMapPlantLayer2.material.set_shader_parameter("median_direction", -0.02)
	$TileMapPlantLayer2.material.set_shader_parameter("sway_strength", 0.016)
	$TileMapPlantLayer2.material.set_shader_parameter("speed", 0.2)
	%RainParticles.process_material.direction = Vector3(0, 1, 0).rotated(Vector3.FORWARD, deg_to_rad(-30))
	%RainParticles.process_material.angle_min = -30
	%RainParticles.process_material.angle_max = -30
	%WindAudioStreamPlayer.stream_paused = false

func _on_windy_right_button_pressed() -> void:
	$TileMapPlantLayer2.material.set_shader_parameter("median_direction", 0.02)
	$TileMapPlantLayer2.material.set_shader_parameter("sway_strength", 0.016)
	$TileMapPlantLayer2.material.set_shader_parameter("speed", 0.2)
	%RainParticles.process_material.direction = Vector3(0, 1, 0).rotated(Vector3.FORWARD, deg_to_rad(30))
	%RainParticles.process_material.angle_min = 30
	%RainParticles.process_material.angle_max = 30
	%WindAudioStreamPlayer.stream_paused = false

func _on_storm_button_pressed() -> void:
	%RainParticles.process_mode = Node.PROCESS_MODE_ALWAYS if %StormButton.button_pressed else Node.PROCESS_MODE_DISABLED
	%RainParticles.visible = %StormButton.button_pressed
	%LightningRect.visible = %StormButton.button_pressed
	%RainAudioStreamPlayer.stream_paused = not %StormButton.button_pressed
