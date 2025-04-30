class_name BaseHat extends Node2D

@onready var _sprite: Sprite2D = $Sprite

var _color := Color.WHITE

@export var color: Color:
	get:
		return _color
	set(value):
		if value != _color:
			_color = value
			if _sprite:
				_sprite.self_modulate = value

func _ready() -> void:
	_sprite.self_modulate = color
