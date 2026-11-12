"""Step 4: pet size reference, preview camera (matches the Unity camera), lights.
Raytracing stays OFF: it crashed Blender on this machine."""
mat = bpy.data.materials
root = coll("LabV2")
ref = coll("LabV2_Reference", root)
clear_collection(ref)
cyl("REF_PetSize", 0.6, 2.2, (0, 0, 1.1), mat['REF_Pet'], ref, (0, 0, 0), 0.5)

cam = bpy.data.objects.new("LabV2Cam", bpy.data.cameras.new("LabV2Cam"))
ref.objects.link(cam)
cam.location = (0, -7, 2.2)          # Unity: (0, 2.2, -7), rotation X = 14
cam.rotation_euler = (R(76), 0, 0)
cam.data.sensor_fit = 'VERTICAL'
cam.data.angle_y = R(40)
sc = bpy.context.scene
sc.camera = cam

sun = bpy.data.objects.new("LabV2Sun", bpy.data.lights.new("LabV2Sun", 'SUN'))
ref.objects.link(sun)
sun.data.energy = 2.6
sun.data.color = (1.0, 0.86, 0.66)        # warm afternoon light
sun.data.angle = R(10)
sun.rotation_euler = (R(55), R(-30), R(40))
fill = bpy.data.objects.new("LabV2Fill", bpy.data.lights.new("LabV2Fill", 'AREA'))
ref.objects.link(fill)
fill.data.energy = 1200
fill.data.size = 8
fill.data.color = (1.0, 0.9, 0.78)
fill.location = (-4, -8, 6)
fill.rotation_euler = (R(60), 0, R(-25))

w = sc.world or bpy.data.worlds.new("World")
sc.world = w
try:
    w.use_nodes = True
except Exception:
    pass
bg = w.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.95, 0.82, 0.64, 1)
    bg.inputs[1].default_value = 0.5
for n in ("Light", "Camera"):
    o = bpy.data.objects.get(n)
    if o:
        o.hide_render = True
sb = bpy.data.collections.get("WindPet_SmokeBlobs")
if sb:
    sb.hide_render = True
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
try:
    sc.render.engine = 'BLENDER_EEVEE'
except Exception:
    pass
try:
    sc.eevee.use_raytracing = False
    sc.eevee.taa_render_samples = 32
except Exception:
    pass
try:
    sc.view_settings.view_transform = 'AgX'
    sc.view_settings.look = 'AgX - Punchy'
except Exception:
    pass
