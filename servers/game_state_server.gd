class_name GameStateServer extends Object

static var map_generation_server: MapGenerationServer = load("res://servers/MapGenerationServer.cs").new()
static var terrain_server: TerrainServer = load("res://servers/TerrainServer.cs").new()
static var game_resources_server: GameResourcesServer = load("res://servers/GameResourcesServer.cs").new()

static func initialize():
	pass
