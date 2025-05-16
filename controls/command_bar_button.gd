class_name CommandBarButton extends Button

@export var command_bar: CommandBar

func _init() -> void:
	custom_minimum_size = Vector2(0, 60)
	mouse_filter = Control.MOUSE_FILTER_PASS
