extends Control

func _ready() -> void:
	_on_generate_cancel_button_pressed()
	
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
	loading_screen.Characters = [%Pawn1, %Pawn2, %Pawn3]

	%Pawn1.get_parent().remove_child(%Pawn1)
	%Pawn2.get_parent().remove_child(%Pawn2)
	%Pawn3.get_parent().remove_child(%Pawn3)

	queue_free()
	get_tree().root.add_child(loading_screen)
	
func _generate_character(pawn: Pawn, name_label: Label) -> void:
	GameStateServer.character_generation_server.GenerateRandomPawn(pawn)
	
	var nickname_portion: String = "\"" + pawn.PawnNickName + "\"\n" if pawn.PawnNickName else ""
	name_label.text = pawn.PawnName + "\n" + nickname_portion + pawn.PawnSurname

func _on_other_button_1_pressed() -> void:
	_generate_character(%Pawn1, %NameLabel1)

func _on_other_button_2_pressed() -> void:
	_generate_character(%Pawn2, %NameLabel2)

func _on_other_button_3_pressed() -> void:
	_generate_character(%Pawn3, %NameLabel3)
