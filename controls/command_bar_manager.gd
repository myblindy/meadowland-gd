class_name CommandBarManager extends Control

@onready var _selected_buttons_container := %SelectedButtonsContainer
@onready var _current_bar_container := %CurrentBarContainer

var _bars: Array[CommandBar]
var _selected_buttons: Array[CommandBarButton]

func _ready() -> void:
	for bar in get_children():
		if bar is CommandBar:
			_bars.append(bar)
			remove_child(bar)
			_hook(bar)
			
	if _bars.size():
		_current_bar_container.add_child(_bars[0])

func _hook(bar: CommandBar) -> void:
	for button in bar.get_children():
		if button is CommandBarButton:
			button.pressed.connect(_button_pressed.bind(bar, button))

func _button_pressed(bar: CommandBar, button: CommandBarButton):
	# clone the bar buttons
	for bar_button in bar.get_children():
		if bar_button is CommandBarButton:
			var cloned_button: CommandBarButton = bar_button.duplicate()
			get_parent().get_parent().add_child(cloned_button)
			cloned_button.position = get_parent().position + bar_button.position + bar.position + position + _current_bar_container.position
			cloned_button.process_mode = Node.PROCESS_MODE_DISABLED
	
	# remove the current bar
	_current_bar_container.remove_child(bar)
