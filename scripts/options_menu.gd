extends Control

@onready var master_volume: HSlider = $"MarginContainer/VBoxContainer/VBoxContainer/Master_volume"
@onready var music_volume: HSlider = $"MarginContainer/VBoxContainer/VBoxContainer/Music_volume"
@onready var sfx_volume: HSlider = $"MarginContainer/VBoxContainer/VBoxContainer/SFX_volume"

func _ready() -> void:
	master_volume.value = db_to_linear(AudioServer.get_bus_volume_db(AudioServer.get_bus_index("Master")))
	music_volume.value = db_to_linear(AudioServer.get_bus_volume_db(AudioServer.get_bus_index("Music")))
	sfx_volume.value = db_to_linear(AudioServer.get_bus_volume_db(AudioServer.get_bus_index("SFX")))

func _on_master_volume_value_changed(value: float) -> void:
	AudioServer.set_bus_volume_db(
		AudioServer.get_bus_index("Master"),
		linear_to_db(value)
	)


func _on_music_volume_value_changed(value: float) -> void:
	AudioServer.set_bus_volume_db(
		AudioServer.get_bus_index("Music"),
		linear_to_db(value)
	)

func _on_sfx_volume_value_changed(value: float) -> void:
	AudioServer.set_bus_volume_db(
		AudioServer.get_bus_index("SFX"),
		linear_to_db(value)
	)
	
func _on_back_pressed():
	get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")
