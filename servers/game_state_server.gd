class_name GameStateServer extends Object

static var map_generation_server: MapGenerationServer = load("res://servers/MapGenerationServer.cs").new()
static var terrain_server: TerrainServer = load("res://servers/TerrainServer.cs").new()
static var game_resources_server: GameResourcesServer = load("res://servers/GameResourcesServer.cs").new()
static var character_generation_server: CharacterGenerationServer = load("res://servers/CharacterGenerationServer.cs").new()

class GlobalSignals:
	signal current_selection_changed(new_selection: Node2D)

static var global_signals: GlobalSignals = GlobalSignals.new()

static var _current_selection: Node2D
static var current_selection: Node2D:
	get():
		return _current_selection
	set(value):
		if _current_selection != value:
			global_signals.current_selection_changed.emit(value)
			_current_selection = value
		else:
			_current_selection = null

static func initialize():
	pass
