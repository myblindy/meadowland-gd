extends ColorRect

var _lighting_audio := load("res://sounds/lightning.mp3")
func _create_lightning_audio() -> AudioStreamPlayer:
	var audio_stream_player := AudioStreamPlayer.new()
	audio_stream_player.stream = _lighting_audio
	audio_stream_player.autoplay = true
	audio_stream_player.finished.connect(func(): audio_stream_player.queue_free())
	
	return audio_stream_player

func _ready() -> void:
	while true:
		await get_tree().create_timer(randf_range(5, 30)).timeout
		
		var lightning_count := randi_range(3, 6)
		for lightning_index in range(lightning_count):
			color = Color(1, 1, 1, 0.8)
			if lightning_index == 0 and visible:
				add_child(_create_lightning_audio())
			await get_tree().create_timer(randf_range(0.05, 0.2)).timeout
			color = Color(1, 1, 1, 0)
			await get_tree().create_timer(randf_range(0.05, 0.2)).timeout
