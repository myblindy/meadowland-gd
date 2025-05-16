extends CommandBar

func _ready() -> void:

	# create the build buttons
	const buildings_path := "res://buildings/"
	for building_file in DirAccess.open(buildings_path).get_files():
		building_file = building_file.replace('.remap', '')
		if building_file.ends_with(".tres"):
			var building_resource := load(buildings_path + building_file) as GameBuildingResource

			var button := CommandBarButton.new()
			button.text = building_resource.name
			button.icon = building_resource.icon_texture
			#button.tooltip_text = "Builds a {}".format([building_resource.name], "{}")
			add_child(button)
			
			# material sub-bar
			var material_sub_bar := CommandBar.new()
			for cost_type in building_resource.cost_types:
				var material_button := CommandBarButton.new()
				material_button.text = cost_type.material_cost.resource.name
				material_button.icon = cost_type.material_cost.resource.icon_texture
				#material_button.tooltip_text = "Builds a {} out of {} ({} hp)" \
				#	.format([building_resource.name, cost_type.material_cost.resource.name, cost_type.hp], "{}")
				material_sub_bar.add_child(material_button)
			get_parent().add_new_bar(material_sub_bar)
			button.command_bar = material_sub_bar
