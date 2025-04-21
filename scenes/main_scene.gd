extends Node2D

func _ready() -> void:
	GameStateServer.map_generation_server.GenerateMap(200, 200,
		[
			MapGenerationWaveResource.new(randi(), 0.004, 1),
			MapGenerationWaveResource.new(randi(), 0.02, 0.5),
		],
		[
			MapGenerationWaveResource.new(randi(), 0.02, 1)
		],
		[
			MapGenerationWaveResource.new(randi(), 0.02, 1),
			MapGenerationWaveResource.new(randi(), 0.01, 0.5),
		])
	print("Map generation complete")
