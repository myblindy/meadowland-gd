extends Node2D

func _ready() -> void:
	GameStateServer.map_generation_server.InitializeGroundTileSet($TileMapGroundLayer)
	GameStateServer.map_generation_server.InitializePlantTileSet($TileMapPlantLayer, $TileMapPlantLayer2)
	var map_size := Vector2i(200, 200)
	var starting_location := GameStateServer.map_generation_server.GenerateMap(
		$TileMapGroundLayer, $TileMapPlantLayer, $TileMapPlantLayer2, $TileMapMiningLayer,
		map_size.x, map_size.y,
		[
			MapGenerationWaveResource.new(randi(), 0.004, 1.0),
			MapGenerationWaveResource.new(randi(), 0.002, 0.5),
		],
		[
			MapGenerationWaveResource.new(randi(), 0.002, 1.0)
		],
		[
			MapGenerationWaveResource.new(randi(), 0.002, 1.0),
			MapGenerationWaveResource.new(randi(), 0.001, 0.5),
		])
	$Camera2D.position = (starting_location - map_size / 2) * GameStateServer.map_generation_server.TileSize

	# generate pawns around the starting location
	for idx in range(5):
		var pawn_position := starting_location - map_size / 2 + Vector2i(randi_range(-10, 10), randi_range(-10, 10))
		_create_random_at_position(pawn_position)

	print("Map generation complete")

var _bodies := [
	load("res://pawns/parts/body/slim_body.tscn"),
	load("res://pawns/parts/body/fat_body.tscn"),
]

var _coats := [
	null,
	load("res://pawns/parts/coats/pauldrons_coat.tscn"),
	load("res://pawns/parts/coats/jacket_coat.tscn")
]

var _hats := [
	null,
	load("res://pawns/parts/hat/beanie_hat.tscn"),
	load("res://pawns/parts/hat/warm_hat.tscn"),
]

func _create_random_at_position(pawn_position: Vector2) -> BaseBody:
	var body: BaseBody = _bodies.pick_random().instantiate()
	
	# skin
	body.skin_color = Color(randf(), randf(), randf())
	
	# coat
	var coat_scene: PackedScene = _coats.pick_random()
	if coat_scene:
		var coat := coat_scene.instantiate()
		coat.color = Color(randf(), randf(), randf())
		body.coat = coat
	else:
		body.coat = null
	
	# hat
	var hat_scene: PackedScene = _hats.pick_random()
	if hat_scene:
		var hat := hat_scene.instantiate()
		hat.color = Color(randf(), randf(), randf())
		body.hat = hat
	else:
		body.hat = null

	$PawnRoot.add_child(body)
	body.position = (pawn_position + Vector2(0.5, 0)) * Vector2(
		GameStateServer.map_generation_server.TileSize.x,
		GameStateServer.map_generation_server.TileSize.y)
	body.scale = Vector2(0.25, 0.25)

	return body
