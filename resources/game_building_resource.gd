class_name GameBuildingResource extends Resource

enum GameBuildingType { BED }

@export var name: String
@export var building_type: GameBuildingType
@export var icon_texture: Texture2D
@export var cost_types: Array[GameBuildingTypeResource]
