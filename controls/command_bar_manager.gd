class_name CommandBarManager extends Control

@export_range(0, 10, 0.01) var fade_duration_sec := 0.25
@export_range(0, 5, 0.01) var fade_delay_sec := 0.2
@export_range(0, 1000, 0.1) var randomness_percentage := 30.0
@export var fade_height_multiplier := 3.0

@onready var _selected_buttons_container := %SelectedButtonsContainer
@onready var _current_bar_container := %CurrentBarContainer
@onready var _separator_image := %SeparatorImage

@onready var _separator_arrow_image := load("res://controls/separator_arrow.png")
@onready var _separator_bar_image := load("res://controls/separator_bar.png")

const _temp_command_bar_button_group := "temp_command_bar_button_group"

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

func _show_separator_left() -> void:
	_separator_image.modulate = Color(1, 1, 1, 1)
	_separator_image.texture = _separator_arrow_image
	_separator_image.flip_h = true
	_separator_image.show()

func _show_separator_right() -> void:
	_separator_image.modulate = Color(1, 1, 1, 1)
	_separator_image.texture = _separator_arrow_image
	_separator_image.flip_h = false
	_separator_image.show()

func _show_separator_bar() -> void:
	_separator_image.modulate = Color(1, 1, 1, 1)
	_separator_image.texture = _separator_bar_image
	_separator_image.flip_h = false
	_separator_image.show()
	
func _hide_separator() -> void:
	_separator_image.hide()

func _transparent_separator() -> void:
	_separator_image.modulate = Color(1, 1, 1, 0)
	_separator_image.show()
	
func _auto_separator():
	# layout isn't done until next frame
	await RenderingServer.frame_pre_draw
	
	var mouse_position := get_global_mouse_position()
	var selected_buttons_container_rect: Rect2 = _selected_buttons_container.get_global_rect()
	var current_bar_container_rect: Rect2 = _current_bar_container.get_global_rect()

	if selected_buttons_container_rect.has_point(mouse_position) and _current_bar_container.get_child_count() > 0:
		_show_separator_right()
	elif current_bar_container_rect.has_point(mouse_position) and _selected_buttons_container.get_child_count() > 0:
		_show_separator_left()
	elif _current_bar_container.get_child_count() == 0 or _selected_buttons_container.get_child_count() == 0:
		_hide_separator()
	else:
		_show_separator_bar()

func _get_random_fade_duration() -> float:
	return randf_range(fade_duration_sec * (1 - randomness_percentage / 100), fade_duration_sec * (1 + randomness_percentage / 100))

func _get_random_fade_delay() -> float:
	return randf_range(fade_delay_sec * (1 - randomness_percentage / 100), fade_delay_sec * (1 + randomness_percentage / 100))

func _fade_in_out_button(real_bar_button: CommandBarButton, fade_in: bool, master_tween: Tween):
	
	# clone the button
	var cloned_button: CommandBarButton = real_bar_button.duplicate()
	cloned_button.mouse_filter = Control.MOUSE_FILTER_IGNORE
	cloned_button.modulate = Color(1, 1, 1, 0) if fade_in else Color(1, 1, 1, 1)
	cloned_button.visible = true
	get_parent().get_parent().add_child(cloned_button)

	var original_position := _get_canvas_relative_position(real_bar_button)

	var this_fade_duration_sec := _get_random_fade_duration()
	var this_delay_sec := _get_random_fade_delay()

	if fade_in:
		cloned_button.position = original_position - Vector2(0, real_bar_button.size.y * fade_height_multiplier)
		master_tween.parallel().tween_property(cloned_button, "position", original_position, this_fade_duration_sec)
		master_tween.parallel().tween_property(cloned_button, "modulate", Color(1, 1, 1, 1), this_fade_duration_sec)
	else:
		cloned_button.position = original_position
		master_tween.parallel().tween_property(cloned_button, "position", original_position + Vector2(0, real_bar_button.size.y * fade_height_multiplier), this_fade_duration_sec)
		master_tween.parallel().tween_property(cloned_button, "modulate", Color(1, 1, 1, 0), this_fade_duration_sec)

	master_tween.finished.connect(cloned_button.queue_free)

func _button_pressed(bar: CommandBar, button: CommandBarButton) -> void:
	# clone the bar buttons
	var cloned_buttons: Array[CommandBarButton]
	var cloned_clicked_button: CommandBarButton
	
	var left_slide_destination := _get_canvas_relative_position(_selected_buttons_container) \
		+ Vector2(_selected_buttons_container.size.x, 0)

	if _selected_buttons_container.get_child_count():
		left_slide_destination += Vector2(5, 0)

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
	# step 1: all buttons except the clicked button drop down off the screen
	if cloned_buttons.size() > 1:
		var tween := create_tween().set_trans(Tween.TRANS_EXPO)
		for cloned_button in cloned_buttons:
			if cloned_button != cloned_clicked_button:
				tween.parallel().tween_property(cloned_button, "position", Vector2(cloned_button.position.x, get_viewport().size.y), _get_random_fade_duration())
		await tween.finished

	# step 2: slide the clicked button to the left
	var tween_left := create_tween().set_trans(Tween.TRANS_EXPO)
	tween_left.tween_property(cloned_clicked_button, "position", left_slide_destination, _get_random_fade_duration())
	await tween_left.finished

	# step 2.1: free all cloned buttons
	for cloned_button in cloned_buttons:
		if cloned_button != cloned_clicked_button:
			cloned_button.queue_free()

	# step 2.2: add the cloned clicked button to the selected buttons container
	cloned_clicked_button.get_parent().remove_child(cloned_clicked_button)
	_selected_buttons_container.add_child(cloned_clicked_button)
	cloned_clicked_button.mouse_filter = Control.MOUSE_FILTER_PASS
	cloned_clicked_button.pressed.connect(_navigate_back.bind(cloned_clicked_button, bar))
	_selected_buttons.append(button)
	
	if button.command_bar:
		# show the new bar, if any
		while _current_bar_container.get_child_count():
			_current_bar_container.remove_child(_current_bar_container.get_child(0))
		_current_bar_container.add_child(button.command_bar)
		
	_auto_separator()

func _navigate_back(cloned_button: CommandBarButton, original_bar: CommandBar) -> void:
	# find the navigation buttons to undo
	var undo_navigation_buttons: Array[CommandBarButton]
	var undo_navigation_buttons_dissolve_source: Array[Vector2]
	var undo_navigation_buttons_dissolve_destination: Array[Vector2]
	for button in _selected_buttons_container.get_children():
		if button == cloned_button or undo_navigation_buttons.size() > 0:
			undo_navigation_buttons.append(button)
			undo_navigation_buttons_dissolve_source.append(_get_canvas_relative_position(button))
			undo_navigation_buttons_dissolve_destination.append(undo_navigation_buttons_dissolve_source[-1] - Vector2(0, button.size.y * 2))
			button.add_to_group(_temp_command_bar_button_group)

	# replace them with a spacer control to keep the layout the same
	var spacer := Control.new()
	spacer.name = "spacer"
	spacer.custom_minimum_size = Vector2(
		undo_navigation_buttons[-1].position.x + undo_navigation_buttons[-1].size.x - undo_navigation_buttons[0].position.x,
		0)

	for navigation_button in undo_navigation_buttons:
		_selected_buttons_container.remove_child(navigation_button)
	_selected_buttons_container.add_child(spacer)

	# slide the navigation buttons up and make them transparent
	var tween = create_tween().set_trans(Tween.TRANS_EXPO)
	for navigation_button_idx in range(undo_navigation_buttons.size()):
		var navigation_button := undo_navigation_buttons[navigation_button_idx]
		get_parent().get_parent().add_child(navigation_button)
		navigation_button.position = undo_navigation_buttons_dissolve_source[navigation_button_idx]
		var fade_duration := _get_random_fade_duration()
		tween.parallel().tween_property(navigation_button, "position", undo_navigation_buttons_dissolve_destination[navigation_button_idx], fade_duration)
		tween.parallel().tween_property(navigation_button, "modulate", Color(1, 1, 1, 0), fade_duration)
	
	# fade out the current bar, if any
	for bar in _current_bar_container.get_children():
		if bar is CommandBar:
			for button in bar.get_children():
				if button is CommandBarButton:
					_fade_in_out_button(button, false, tween)

	while _current_bar_container.get_child_count():
		_current_bar_container.remove_child(_current_bar_container.get_child(0))
	#return
	await tween.finished
	
	# spacer goes bye bye
	spacer.get_parent().remove_child(spacer)
	spacer.queue_free()

	# fade in the original bar
	_current_bar_container.add_child(original_bar)
	original_bar.modulate = Color(1, 1, 1, 0)
	if _selected_buttons_container.get_child_count():
		_transparent_separator()
	else:
		_hide_separator()
	await RenderingServer.frame_pre_draw
	
	tween = create_tween().set_trans(Tween.TRANS_EXPO)
	for original_bar_button in original_bar.get_children():
		if original_bar_button is CommandBarButton:
			_fade_in_out_button(original_bar_button, true, tween)
	await tween.finished
	
	# fading is done, get rid of the cloned buttons and show the real bar
	original_bar.modulate = Color(1, 1, 1, 1)
	get_tree().call_group(_temp_command_bar_button_group, "queue_free")

	await _auto_separator()

func _on_selected_buttons_container_mouse_entered() -> void:
	if _selected_buttons_container.get_child_count():
		_show_separator_right()

func _on_current_bar_container_mouse_entered() -> void:
	if _current_bar_container.get_child_count() and _selected_buttons_container.get_child_count():
		_show_separator_left()

func _on_current_bar_container_mouse_exited() -> void:
	await _auto_separator()

func _on_selected_buttons_container_mouse_exited() -> void:
	await _auto_separator()
