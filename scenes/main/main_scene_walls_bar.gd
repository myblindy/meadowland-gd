extends CommandBar

func _ready() -> void:

	# create the wall buttons
	var walls: GameWallsResource = load("res://walls/walls_resource.tres")
	
	for wall_type in walls.wall_types:
		var button := CommandBarButton.new()
		button.icon = wall_type.cost.resource.icon_texture
		button.text = wall_type.cost.resource.name
		add_child(button)
