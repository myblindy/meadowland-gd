extends Node2D

func _ready() -> void:
	await MapGenerationServer.GenerateMapAsync(200, 200)
