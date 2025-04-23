extends Node2D

func _ready() -> void:
	GameStateServer.map_generation_server.InitializeTileSet($TileMapGroundLayer)
	var map_size := Vector2i(200, 200)
	var starting_location := GameStateServer.map_generation_server.GenerateMap(
		$TileMapGroundLayer, map_size.x, map_size.y,
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
	print("Map generation complete")
