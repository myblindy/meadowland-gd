extends HBoxContainer

func _ready() -> void:

	# create the build buttons
	const buildings_path := "res://buildings/"
	for building_file in DirAccess.open(buildings_path).get_files():
		building_file = building_file.replace('.remap', '')
		if building_file.ends_with(".tres"):
			var building_resource := load(buildings_path + building_file) as GameBuildingResource

			var button := Button.new()
			button.text = building_resource.name
			button.icon = building_resource.cost_types[0].icon_texture
			button.tooltip_text = "Builds a {}".format([building_resource.name], "{}")
			add_child(button)
