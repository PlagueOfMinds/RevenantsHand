class_name ZDepthTint
extends Node
## Feeds a sprite's depth into the z_depth_tint shader.
## Add as a child of any CanvasItem (Sprite2D, AnimatedSprite2D, ...).
## Each sprite gets its own copy of the material.

enum DepthSource { Z_INDEX, GLOBAL_Y, CUSTOM }
enum RangeMode { AUTO, MANUAL }

const CACHE_KEY := &"_z_depth_tint_cache"

## The node to affect. Leave empty to use the parent.
@export var target: CanvasItem
## Optional ShaderMaterial to copy onto the target. If empty, the target's
## existing ShaderMaterial is duplicated instead.
@export var template_material: ShaderMaterial
@export var depth_source: DepthSource = DepthSource.Z_INDEX
## Used when depth_source is CUSTOM.
@export var custom_depth: float = 0.0:
	set(value):
		custom_depth = value
		if is_node_ready():
			_refresh(false)

@export_group("Depth Range")
## AUTO: the min/max depth is taken from every ZDepthTint in the same range_group.
## MANUAL: uses depth_min / depth_max below.
@export var range_mode: RangeMode = RangeMode.AUTO
## Sprites sharing a group share one depth range. Use different groups
## for separate layers/scenes that should be tinted independently.
@export var range_group: StringName = &"z_depth_tint"
@export var depth_min: float = -10.0
@export var depth_max: float = 10.0

@export_group("Updating")
## Re-check depth every frame. Shader values are only written when something changes.
## Turn off for static scenes and call refresh() yourself after z changes.
@export var update_every_frame: bool = true
## Prints the computed depth whenever it changes. Useful for checking your z values.
@export var debug_print: bool = false

var _material: ShaderMaterial
var _last_depth: float = 0.0
var _last_min: float = 0.0
var _last_max: float = 0.0
var _has_last: bool = false


func _ready() -> void:
	if target == null:
		target = get_parent() as CanvasItem
	if target == null:
		push_warning("ZDepthTint: needs a CanvasItem parent or an assigned target.")
		set_process(false)
		return

	var source: Material = template_material
	if source == null:
		source = target.material
	if source == null or not (source is ShaderMaterial):
		push_warning("ZDepthTint: no ShaderMaterial found on '%s'." % target.name)
		set_process(false)
		return

	_material = (source as ShaderMaterial).duplicate() as ShaderMaterial
	target.material = _material

	if range_mode == RangeMode.AUTO:
		add_to_group(range_group)

	_refresh(true)
	set_process(update_every_frame)


func _process(_delta: float) -> void:
	_refresh(false)


## Call manually after changing z_index if update_every_frame is off.
func refresh() -> void:
	_refresh(true)


func _refresh(force: bool) -> void:
	if _material == null or target == null:
		return

	var d: float = _get_depth()
	var lo: float = depth_min
	var hi: float = depth_max
	if range_mode == RangeMode.AUTO:
		var r: Vector2 = _get_auto_range()
		lo = r.x
		hi = r.y
	# All sprites at the same depth: widen the range so they land mid-way.
	if hi - lo < 0.001:
		lo -= 0.5
		hi += 0.5

	if not force and _has_last \
			and is_equal_approx(d, _last_depth) \
			and is_equal_approx(lo, _last_min) \
			and is_equal_approx(hi, _last_max):
		return

	_last_depth = d
	_last_min = lo
	_last_max = hi
	_has_last = true

	_material.set_shader_parameter("depth_value", d)
	_material.set_shader_parameter("depth_min", lo)
	_material.set_shader_parameter("depth_max", hi)

	if debug_print:
		print("ZDepthTint '%s': depth=%s range=[%s, %s]" % [target.name, d, lo, hi])


func _get_depth() -> float:
	match depth_source:
		DepthSource.GLOBAL_Y:
			return target.get_global_transform().origin.y
		DepthSource.CUSTOM:
			return custom_depth
		_:
			return float(_effective_z())


## Final z used by the renderer: z_index summed up through ancestors for as long
## as z_as_relative is on. Skips non-CanvasItem nodes, stops at CanvasLayers.
func _effective_z() -> int:
	var z: int = 0
	var ci: CanvasItem = target
	while ci != null:
		z += ci.z_index
		if not ci.z_as_relative or ci.top_level:
			break
		ci = _next_canvas_item(ci)
	return clampi(z, RenderingServer.CANVAS_ITEM_Z_MIN, RenderingServer.CANVAS_ITEM_Z_MAX)


func _next_canvas_item(from: CanvasItem) -> CanvasItem:
	var p: Node = from.get_parent()
	while p != null:
		if p is CanvasItem:
			return p as CanvasItem
		if p is CanvasLayer:
			return null
		p = p.get_parent()
	return null


## Min/max depth of every ZDepthTint in range_group. Computed at most once per
## frame per group; the result is cached on the root node so all instances share it.
func _get_auto_range() -> Vector2:
	var root: Node = get_tree().root
	var frame: int = Engine.get_process_frames()

	var cache: Dictionary = {}
	if root.has_meta(CACHE_KEY):
		cache = root.get_meta(CACHE_KEY)

	var entry: Variant = cache.get(range_group)
	if entry != null and entry["frame"] == frame:
		return entry["range"]

	var lo: float = INF
	var hi: float = -INF
	for n in get_tree().get_nodes_in_group(range_group):
		var other: ZDepthTint = n as ZDepthTint
		if other == null or other.target == null or other._material == null:
			continue
		var od: float = other._get_depth()
		lo = minf(lo, od)
		hi = maxf(hi, od)

	if lo > hi:
		var own: float = _get_depth()
		lo = own
		hi = own

	var result := Vector2(lo, hi)
	cache[range_group] = {"frame": frame, "range": result}
	root.set_meta(CACHE_KEY, cache)
	return result
