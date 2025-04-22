extends Node2D

func _ready() -> void:
	GameStateServer.map_generation_server.InitializeTileSet($TileMapGroundLayer)
	GameStateServer.map_generation_server.GenerateMap($TileMapGroundLayer, 200, 200,
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
	print("Map generation complete")
