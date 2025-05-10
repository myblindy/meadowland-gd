extends Control

func _on_exit_button_pressed() -> void:
	get_tree().quit()

func _on_new_game_button_pressed() -> void:
	%MainMenuContainer.visible = false
	%NewGameContainer.visible = true

func _on_generate_cancel_button_pressed() -> void:
	%MainMenuContainer.visible = true
	%NewGameContainer.visible = false

func _on_start_map_generation_button_pressed() -> void:
	var res := RegEx.create_from_string("(\\d+)x(\\d+)").search(%SizeOptionButton.get_item_text(%SizeOptionButton.selected))
	var width := int(res.get_string(1))
	var height := int(res.get_string(2))
	
	var loading_screen = load("res://scenes/generating_map/generating_map_scene.tscn").instantiate()
	loading_screen.MapSize = Vector2i(width, height)

	queue_free()
	get_tree().root.add_child(loading_screen)
