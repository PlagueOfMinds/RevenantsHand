@tool
extends Sprite2D

@export var editor_color: Color = Color.WHITE:
	set(new_color):
		editor_color = new_color
		self_modulate = new_color

# 1. Create a dummy boolean variable
@export var click_to_refresh: bool = false:
	set(value):
		# 2. When clicked, force the color to re-apply and immediately turn the button off
		self_modulate = editor_color
		notify_property_list_changed() # Refreshes the Inspector UI

func _ready() -> void:
	self_modulate = editor_color
