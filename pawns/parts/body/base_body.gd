class_name BaseBody extends Node2D

@onready var _sprite: Sprite2D = $Sprite
@onready var _coat_root := $CoatRoot
@onready var _hat_root := $HatRoot

var _skin_color := Color.WHITE

@export var skin_color: Color:
	get:
		return _skin_color
	set(value):
		if value != _skin_color:
			_skin_color = value
			if _sprite:
				_sprite.self_modulate = value

var _coat: BaseCoat
@export var coat: BaseCoat:
	get:
		return _coat
	set(value):
		if value != _coat:
			if _coat:
				_coat.queue_free()
			_coat = value
			if _coat and _coat_root:
				_coat_root.add_child(_coat)

var _hat: BaseHat
@export var hat: BaseHat:
	get:
		return _hat
	set(value):
		if value != _hat:
			if _hat:
				_hat.queue_free()
			_hat = value
			if _hat and _hat_root:
				_hat_root.add_child(_hat)

func _ready() -> void:
	_sprite.self_modulate = skin_color

	if _coat:
		_coat_root.add_child(_coat)

	if _hat:
		_hat_root.add_child(_hat)
