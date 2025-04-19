extends HBoxContainer

func _ready() -> void:

	# create the build buttons
	const items_path := "res://items/"
	for item_file in DirAccess.open(items_path).get_files():
		var item_resource := load(items_path + item_file) as GameItemResource
		if item_resource and item_resource.placeable:
			var button := Button.new()
			button.text = item_resource.name
			button.icon = item_resource.icon_texture
			button.tooltip_text = "Builds a {}".format([item_resource.name], "{}")
			add_child(button)
