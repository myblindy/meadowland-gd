class_name CommandBarManager extends Control

@onready var _selected_buttons_container := %SelectedButtonsContainer
@onready var _current_bar_container := %CurrentBarContainer

var _bars: Array[CommandBar]
var _selected_buttons: Array[CommandBarButton]

func _ready() -> void:
	var _new_bars: Array[CommandBar]
	for bar in get_children():
		if bar is CommandBar:
			_new_bars.append(bar)
			remove_child(bar)
			_hook(bar)
			
	if _new_bars.size():
		_current_bar_container.add_child(_new_bars[0])
	
	while _new_bars.size():
		_bars.insert(0, _new_bars.pop_back())

func add_new_bar(bar: CommandBar) -> void:
	_bars.append(bar)
	_hook(bar)

signal _await_all_done
func _await_all(signals: Array[Signal]):
	if signals.size():
		var done: Array[bool]
		done.resize(signals.size())

		for _signal in signals:
			_signal.connect(func(): 
				done[signals.find(_signal)] = true
				if done.all(func(b: bool) -> bool: return b):
					_await_all_done.emit())

		await _await_all_done

func _hook(bar: CommandBar) -> void:
	for button in bar.get_children():
		if button is CommandBarButton:
			button.pressed.connect(_button_pressed.bind(bar, button))

func _get_canvas_relative_position(ctl: Control) -> Vector2:
	var pos: Vector2 = ctl.position
	while ctl.get_parent() != null and ctl.get_parent() is not CanvasLayer:
		ctl = ctl.get_parent()
		pos += ctl.position
	return pos

func _button_pressed(bar: CommandBar, button: CommandBarButton) -> void:
	# clone the bar buttons
	var cloned_buttons: Array[CommandBarButton]
	var cloned_clicked_button: CommandBarButton
	
	var left_slide_destination := _get_canvas_relative_position(_current_bar_container)

	for bar_button in bar.get_children():
		if bar_button is CommandBarButton:
			var cloned_button: CommandBarButton = bar_button.duplicate()
			get_parent().get_parent().add_child(cloned_button)
			cloned_button.position = _get_canvas_relative_position(bar_button)
			cloned_button.mouse_filter = Control.MOUSE_FILTER_IGNORE
			
			cloned_buttons.append(cloned_button)
			if bar_button == button:
				cloned_clicked_button = cloned_button
		
	# remove the current bar
	_current_bar_container.remove_child(bar)

	# animate the buttons
	var signals: Array[Signal]

	# step 1: all buttons except the clicked button drop down off the screen
	for cloned_button in cloned_buttons:
		if cloned_button != cloned_clicked_button:
			var tween := create_tween().set_trans(Tween.TRANS_EXPO)
			tween.tween_property(cloned_button, "position", Vector2(cloned_button.position.x, get_viewport().size.y), 0.3)
			signals.append(tween.finished)
	await _await_all(signals)

	# step 2: slide the clicked button to the left
	var tween_left := create_tween().set_trans(Tween.TRANS_EXPO)
	tween_left.tween_property(cloned_clicked_button, "position", left_slide_destination, 0.3)
	await tween_left.finished

	# step 2.1: free all cloned buttons
	for cloned_button in cloned_buttons:
		if cloned_button != cloned_clicked_button:
			cloned_button.queue_free()

	# step 2.2: add the cloned clicked button to the selected buttons container
	cloned_clicked_button.get_parent().remove_child(cloned_clicked_button)
	_selected_buttons_container.add_child(cloned_clicked_button)
	cloned_clicked_button.mouse_filter = Control.MOUSE_FILTER_STOP
	cloned_clicked_button.pressed.connect(_navigate_back.bind(cloned_clicked_button, bar))
	_selected_buttons.append(button)

	# step x: show the new bar, if any
	if button.command_bar:
		while _current_bar_container.get_child_count():
			_current_bar_container.remove_child(_current_bar_container.get_child(0))
		_current_bar_container.add_child(button.command_bar)
		#button.command_bar.show()

func _navigate_back(cloned_button: CommandBarButton, original_bar: CommandBar) -> void:
	# find the navigation buttons to undo
	var undo_navigation_buttons: Array[CommandBarButton]
	var undo_navigation_buttons_dissolve_destination: Array[Vector2]
	for button in _selected_buttons:
		if button.command_bar == original_bar or undo_navigation_buttons.size() > 0:
			undo_navigation_buttons.append(button)
			undo_navigation_buttons_dissolve_destination.append(_get_canvas_relative_position(button) - Vector2(0, button.size.y * 2))

	# replace them with a spacer control to keep the layout the same
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, undo_navigation_buttons.reduce(func(accum: float, ctl: Control): return accum + ctl.size.y, 0))

	for navigation_button in undo_navigation_buttons:
		_selected_buttons_container.remove_child(navigation_button)
	_selected_buttons_container.add_child(spacer)

	# slide the navigation buttons up and make them transparent
	var signals: Array[Signal]
	for navigation_button_idx in range(undo_navigation_buttons.size()):
		var navigation_button := undo_navigation_buttons[navigation_button_idx]
		var tween := create_tween().set_trans(Tween.TRANS_EXPO)
		get_parent().get_parent().add_child(navigation_button)
		tween.tween_property(navigation_button, "position", undo_navigation_buttons_dissolve_destination[navigation_button_idx], 0.3)
		tween.tween_property(navigation_button, "modulate", Color(1, 1, 1, 0), 0.3)
		signals.append(tween.finished)

	await _await_all(signals)

	# show the original bar
	while _current_bar_container.get_child_count():
		_current_bar_container.remove_child(_current_bar_container.get_child(0))
	_current_bar_container.add_child(original_bar)
