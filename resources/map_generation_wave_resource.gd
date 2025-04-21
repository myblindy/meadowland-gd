class_name MapGenerationWaveResource extends Resource

@export var wave_seed: int
@export var frequency: float
@export var amplitude: float

func _init(_wave_seed: int, _frequency: float, _amplitude: float) -> void:
	wave_seed = _wave_seed
	frequency = _frequency
	amplitude = _amplitude
