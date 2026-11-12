"""Bakes the lab (procedural materials -> textures) and exports FBX files for Unity.
Works on a copy (Lab_Export.blend) so Lab.blend stays editable.
Per group: BaseColor (with AO), Normal, Emission textures. Ink outlines and
glass are split into their own meshes because Unity needs special materials
for them (back-face unlit outline, transparent glass)."""
import os
import math
import time
import bpy

PROJECT = r"C:\Users\ADMIN\OneDrive\Desktop\GameSystem2"
OUT = os.path.join(PROJECT, "Assets", "VirtualPet", "Art", "Lab")
TEX = os.path.join(OUT, "Textures")
LOG = os.path.join(PROJECT, "01_VirtualPet", "2_Design", "Blender", "export_log.txt")
os.makedirs(TEX, exist_ok=True)


def log(msg):
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(time.strftime("%H:%M:%S ") + msg + "\n")


def top_group(o):
    while o.parent is not None:
        o = o.parent
    return o


def coll_meshes(name):
    c = bpy.data.collections.get(name)
    return [o for o in c.all_objects if o.type == 'MESH'] if c else []


room = coll_meshes("LabV2_Room")
inter = coll_meshes("LabV2_Interactables")
groups = [
    ("Lab_Desk", [o for o in room if top_group(o).name == "Desk"], 2048, (0, 0, 0)),
    ("Lab_Wall", [o for o in room if top_group(o).name != "Desk"], 2048, (0, 0, 0)),
    ("Lab_Shelf", coll_meshes("LabV2_Shelves"), 2048, (0, 0, 0)),
    ("Lab_Window", coll_meshes("LabV2_Window"), 1024, (0, 0, 0)),
    ("Lab_Decor", coll_meshes("LabV2_Decor"), 2048, (0, 0, 0)),
    ("Lab_Pachinko", [o for o in inter if top_group(o).name == "PachinkoMachine"], 1024, None),
    ("Prop_QuatMo", [o for o in inter if o.name.startswith("MoCau")], 512, "QuatMo_Grip"),
    ("Prop_QuatMoHolder", [o for o in inter if o.name.startswith("QuatMo_Holder")], 256, None),
    ("Prop_HairDryer", [o for o in inter if top_group(o).name == "Prop_HairDryer"], 512, None),
]


def select_only(objs, active=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]


def build_group(name, objs, res, pivot):
    tmp = bpy.data.collections.get("EXPORT_TMP") or bpy.data.collections.new("EXPORT_TMP")
    if tmp.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(tmp)
    dups = []
    for o in objs:
        d = o.copy()
        d.data = o.data.copy()
        d.parent = None
        d.matrix_world = o.matrix_world.copy()
        tmp.objects.link(d)
        dups.append(d)
    select_only(dups)
    bpy.ops.object.convert(target='MESH')          # applies bevel/subdiv/outline modifiers
    bpy.ops.object.join()
    J = bpy.context.active_object
    J.name = name
    # pivot
    if isinstance(pivot, str):
        loc = bpy.data.objects[pivot].matrix_world.translation.copy()
    elif pivot is None:
        loc = top_group(objs[0]).matrix_world.translation.copy()
    else:
        loc = pivot
    bpy.context.scene.cursor.location = loc
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return J


def split_by_material(J, test, suffix):
    idx = [i for i, m in enumerate(J.data.materials) if m and test(m)]
    if not idx:
        return None
    select_only([J])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    for i in idx:
        J.active_material_index = i
        bpy.ops.object.material_slot_select()
    bpy.ops.mesh.separate(type='SELECTED')
    bpy.ops.object.mode_set(mode='OBJECT')
    parts = [o for o in bpy.context.selected_objects if o != J]
    if not parts:
        return None
    p = parts[0]
    p.name = J.name + suffix
    for o in (J, p):
        select_only([o])
        bpy.ops.object.material_slot_remove_unused()
    return p


def has_emission(J):
    for m in J.data.materials:
        b = m.node_tree.nodes.get("Principled BSDF") if m and m.node_tree else None
        if b and b.inputs["Emission Strength"].default_value > 0:
            return True
    return False


def bake(J, kind, img_name, res, non_color=False):
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, res, res, alpha=False, float_buffer=False)
    if non_color:
        img.colorspace_settings.name = 'Non-Color'
    for m in J.data.materials:
        nt = m.node_tree
        n = nt.nodes.get("BAKE_TARGET") or nt.nodes.new("ShaderNodeTexImage")
        n.name = "BAKE_TARGET"
        n.image = img
        nt.nodes.active = n
    select_only([J])
    if kind == 'DIFFUSE':
        bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, margin=8, use_clear=True)
    else:
        bpy.ops.object.bake(type=kind, margin=8, use_clear=True)
    img.filepath_raw = os.path.join(TEX, img_name + ".png")
    img.file_format = 'PNG'
    img.save()
    return img


def baked_material(name, base, normal, emit):
    m = bpy.data.materials.new("M_" + name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = base
    nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
    tn = nt.nodes.new("ShaderNodeTexImage")
    tn.image = normal
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(tn.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    if emit is not None:
        te = nt.nodes.new("ShaderNodeTexImage")
        te.image = emit
        nt.links.new(te.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = 1.0
    b.inputs["Roughness"].default_value = 0.6
    return m


def run():
    if os.path.exists(LOG):
        os.remove(LOG)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(PROJECT, "01_VirtualPet", "2_Design", "Blender", "Lab_Export.blend"))
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 24
    sc.cycles.device = 'CPU'
    sc.render.bake.margin = 8
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for name, objs, res, pivot in groups:
        if not objs:
            log(f"skip {name}: no objects")
            continue
        log(f"{name}: {len(objs)} objects")
        J = build_group(name, objs, res, pivot)
        outline = split_by_material(J, lambda m: m.name == "L3_Outline", "_Outline")
        glass = split_by_material(J, lambda m: "Glass" in m.name or m.name.startswith("REF_"), "_Glass")
        select_only([J])
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003)
        bpy.ops.object.mode_set(mode='OBJECT')
        base = bake(J, 'DIFFUSE', name + "_BaseColor", res)
        log(f"  baked color")
        normal = bake(J, 'NORMAL', name + "_Normal", res, True)
        log(f"  baked normal")
        emit = bake(J, 'EMIT', name + "_Emission", res) if has_emission(J) else None
        if emit:
            log(f"  baked emission")
        J.data.materials.clear()
        J.data.materials.append(baked_material(name, base, normal, emit))
        parts = [J] + [p for p in (outline, glass) if p]
        for p in parts[1:]:
            select_only([p, J], J)
            bpy.ops.object.parent_set(type='OBJECT', keep_transform=True)
        select_only(parts, J)
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True,
                                 apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                                 bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE',
                                 use_mesh_modifiers=True, path_mode='STRIP')
        log(f"  exported {name}.fbx  pivot={tuple(round(v, 3) for v in J.location)}")
        bpy.ops.wm.save_mainfile()
    log("ALL DONE")


run()
