extends CommandBar

func _ready() -> void:

	# create the floor buttons
	const floors_path := "res://floors/"
	for floor_file in DirAccess.open(floors_path).get_files():
		floor_file = floor_file.replace('.remap', '')
		if floor_file.ends_with(".tres"):
			var floor_resource := load(floors_path + floor_file) as GameFloorResource

			var button := CommandBarButton.new()
			#button.text = floor_resource.name
			button.icon = floor_resource.sprite_texture
			add_child(button)
