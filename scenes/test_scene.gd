extends Node2D

func _ready() -> void:
	_on_randomize_button_pressed()

var bodies := [
	load("res://pawns/parts/body/slim_body.tscn"),
	load("res://pawns/parts/body/fat_body.tscn"),
]

var coats := [
	null,
	load("res://pawns/parts/coats/pauldrons_coat.tscn"),
	load("res://pawns/parts/coats/jacket_coat.tscn")
]

var hats := [
	null,
	load("res://pawns/parts/hat/beanie_hat.tscn"),
	load("res://pawns/parts/hat/warm_hat.tscn"),
]

func _create_at_marker(marker: Marker2D) -> void:
	var body: BaseBody = bodies.pick_random().instantiate()
	
	# skin
	body.skin_color = Color(randf(), randf(), randf())
	
	# coat
	var coat_scene: PackedScene = coats.pick_random()
	if coat_scene:
		var coat := coat_scene.instantiate()
		coat.color = Color(randf(), randf(), randf())
		body.coat = coat
	else:
		body.coat = null
	
	# hat
	var hat_scene: PackedScene = hats.pick_random()
	if hat_scene:
		var hat := hat_scene.instantiate()
		hat.color = Color(randf(), randf(), randf())
		body.hat = hat
	else:
		body.hat = null

	for child in marker.get_children():
		child.queue_free()
	marker.add_child(body)

func _on_randomize_button_pressed() -> void:
	for child in get_children():
		if child is Marker2D:
			_create_at_marker(child)
