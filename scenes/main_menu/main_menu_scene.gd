extends Control

func _ready() -> void:
	%MainMenuContainer.visible = true
	%NewGameContainer.visible = false
	
	_on_other_button_1_pressed()
	_on_other_button_2_pressed()
	_on_other_button_3_pressed()

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
	loading_screen.Characters = [%SpriteMarker1.get_child(0), %SpriteMarker2.get_child(0), %SpriteMarker3.get_child(0)]

	%SpriteMarker1.remove_child(%SpriteMarker1.get_child(0))
	%SpriteMarker2.remove_child(%SpriteMarker2.get_child(0))
	%SpriteMarker3.remove_child(%SpriteMarker3.get_child(0))

	queue_free()
	get_tree().root.add_child(loading_screen)
	
func _generate_character(marker: Marker2D, name_label: Label) -> void:
	var pawn: Pawn = GameStateServer.character_generation_server.GenerateRandomPawn()
	while marker.get_child_count() > 0:
		marker.remove_child(marker.get_child(0))
	marker.add_child(pawn)
	
	var pawn_name := GameStateServer.character_generation_server.GenerateRandomName(randi_range(0, 1))
	name_label.text = pawn_name.replace(' ', '\n')

func _on_other_button_1_pressed() -> void:
	_generate_character(%SpriteMarker1, %NameLabel1)

func _on_other_button_2_pressed() -> void:
	_generate_character(%SpriteMarker2, %NameLabel2)

func _on_other_button_3_pressed() -> void:
	_generate_character(%SpriteMarker3, %NameLabel3)
