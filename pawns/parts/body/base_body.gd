class_name BaseBody extends Node2D

@onready var _sprite: Sprite2D = $Sprite
@onready var _face_root := $FaceRoot
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

var _eyes: BaseEyes
@export var eyes: BaseEyes:
	get:
		return _eyes
	set(value):
		if value != _eyes:
			if _eyes:
				_eyes.queue_free()
			_eyes = value
			if _eyes and _face_root:
				_face_root.add_child(_eyes)
				
func _ready() -> void:
	_sprite.self_modulate = skin_color
	
	if _eyes:
		_face_root.add_child(_eyes)

	if _coat:
		_coat_root.add_child(_coat)

	if _hat:
		_hat_root.add_child(_hat)
