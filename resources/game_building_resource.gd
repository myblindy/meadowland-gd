class_name GameBuildingResource extends Resource

enum GameBuildingType { BED }

@export var name: String
@export var building_type: GameBuildingType
@export var cost_types: Array[GameBuildingTypeResource]
