"""Export v2: every movable prop is its own object (own pivot) so Unity can give
it a Rigidbody. Props that belong together share one texture atlas.
Static parts (desk, wall, shelf frame, window, wall decor, item machine) are
merged per group. Metallic is set to 0 while baking, so metal keeps its colour
in the baked base-colour texture (Unity gets metal back through smoothness)."""
import os
import math
import time
import bpy
from mathutils import Vector

PROJECT = r"C:\Users\ADMIN\OneDrive\Desktop\GameSystem2"
OUT = os.path.join(PROJECT, "Assets", "VirtualPet", "Art", "Lab")
TEX = os.path.join(OUT, "Textures")
LOG = os.path.join(PROJECT, "01_VirtualPet", "2_Design", "Blender", "export_log.txt")
os.makedirs(TEX, exist_ok=True)
# Re-export only these atlases (empty = all). Saves a long bake when one group changed.
ONLY = set(globals().get("EXPORT_ONLY", ()))


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


# ---------------- sort every mesh into an atlas and an item ----------------
atlases = {}   # atlas -> {"res": int, "static": bool, "items": {key: [objs]}}


def add(atlas, key, o, res, static):
    a = atlases.setdefault(atlas, {"res": res, "static": static, "items": {}})
    a["items"].setdefault(key, []).append(o)


for o in coll_meshes("LabV2_Room"):
    add("Lab_Desk" if top_group(o).name == "Desk" else "Lab_Wall", "main", o, 2048, True)
for o in coll_meshes("LabV2_Window"):
    add("Lab_Window", "main", o, 1024, True)
for o in coll_meshes("LabV2_Shelves"):
    t = top_group(o).name
    if t == "ShelfUnit" or o.name.startswith(("Shelf_", "Note_")):
        add("Lab_ShelfFrame", "main", o, 2048, True)
    else:
        add("Lab_ShelfItems", "PaperStack" if t.startswith("PaperStack") else t, o, 2048, False)
for o in coll_meshes("LabV2_Decor"):
    t = top_group(o).name
    if t in ("Pipes", "LabSign", "WindSketch") or o.name.startswith("Note_"):
        add("Lab_WallDecor", "main", o, 1024, True)
    elif t == "FlaskGroup":
        add("Lab_DeskItems", o.name.split("_")[0], o, 2048, False)
    else:
        add("Lab_DeskItems", t, o, 2048, False)
for o in coll_meshes("LabV2_Interactables"):
    t = top_group(o).name
    if t == "PachinkoMachine":
        add("Lab_Pachinko", "main", o, 1024, True)
    elif o.name.startswith("MoCau"):
        add("Lab_Props", "QuatMo", o, 1024, False)
    elif o.name.startswith("QuatMo_Holder"):
        add("Lab_Props", "QuatMoHolder", o, 1024, False)
    elif t == "Prop_HairDryer":
        add("Lab_Props", "HairDryer", o, 1024, False)
# work desk props: each top-level group becomes one item (the rotor is separate so it can spin)
for o in coll_meshes("LabV2_Work"):
    add("Lab_Work", top_group(o).name, o, 1024, False)


def select_only(objs, active=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]


def world_bounds_center(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return (lo + hi) / 2


def make_item(name, objs, pivot):
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
    bpy.ops.object.convert(target='MESH')
    if len(dups) > 1:
        bpy.ops.object.join()
    J = bpy.context.active_object
    J.name = name
    bpy.context.scene.cursor.location = pivot
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return J


def split_by_material(J, test, suffix):
    idx = [i for i, m in enumerate(J.data.materials) if m and test(m)]
    if not idx:
        return None
    if len(idx) == len(J.data.materials):
        return None            # the whole item is this material; keep it as the main mesh
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


def bake(mains, kind, img_name, res, non_color=False):
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, res, res, alpha=False)
    if non_color:
        img.colorspace_settings.name = 'Non-Color'
    for J in mains:
        for m in J.data.materials:
            nt = m.node_tree
            n = nt.nodes.get("BAKE_TARGET") or nt.nodes.new("ShaderNodeTexImage")
            n.name = "BAKE_TARGET"
            n.image = img
            nt.nodes.active = n
    select_only(mains)
    if kind == 'DIFFUSE':
        bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, margin=8, use_clear=True)
    else:
        bpy.ops.object.bake(type=kind, margin=8, use_clear=True)
    img.filepath_raw = os.path.join(TEX, img_name + ".png")
    img.file_format = 'PNG'
    img.save()
    return img


def has_emission(mains):
    for J in mains:
        for m in J.data.materials:
            b = m.node_tree.nodes.get("Principled BSDF") if m and m.node_tree else None
            if b and b.inputs["Emission Strength"].default_value > 0:
                return True
    return False


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
    return m


def item_pivot(key, objs):
    if key == "Lantern":
        lan = bpy.data.objects["Lantern"].matrix_world.translation
        top = max((o.matrix_world @ Vector(c)).z for o in objs for c in o.bound_box)
        return Vector((lan.x, lan.y, top))           # hang point at the top of the chain
    if key == "QuatMo":
        return bpy.data.objects["QuatMo_Grip"].matrix_world.translation.copy()
    return world_bounds_center(objs)


def run():
    if os.path.exists(LOG):
        os.remove(LOG)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(PROJECT, "01_VirtualPet", "2_Design", "Blender", "Lab_Export.blend"))
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 16
    sc.cycles.device = 'CPU'
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    # metal keeps its colour in the diffuse bake
    for m in bpy.data.materials:
        b = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if b:
            b.inputs["Metallic"].default_value = 0.0
    for atlas, info in atlases.items():
        if ONLY and atlas not in ONLY:
            continue
        log(f"{atlas}: {len(info['items'])} item(s), static={info['static']}")
        mains, roots = [], []
        for key, objs in info["items"].items():
            name = atlas if info["static"] else "Item_" + key
            pivot = Vector((0, 0, 0)) if info["static"] else item_pivot(key, objs)
            J = make_item(name, objs, pivot)
            outline = split_by_material(J, lambda m: m.name == "L3_Outline", "_Outline")
            glass = split_by_material(J, lambda m: "Glass" in m.name or m.name.startswith("REF_"), "_Glass")
            mains.append(J)
            for p in (outline, glass):
                if p:
                    select_only([p, J], J)
                    bpy.ops.object.parent_set(type='OBJECT', keep_transform=True)
            roots.append(J)
        select_only(mains)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.002)
        bpy.ops.object.mode_set(mode='OBJECT')
        res = info["res"]
        base = bake(mains, 'DIFFUSE', atlas + "_BaseColor", res)
        log("  colour")
        normal = bake(mains, 'NORMAL', atlas + "_Normal", res, True)
        log("  normal")
        emit = bake(mains, 'EMIT', atlas + "_Emission", res) if has_emission(mains) else None
        if emit:
            log("  emission")
        mat = baked_material(atlas, base, normal, emit)
        for J in mains:
            J.data.materials.clear()
            J.data.materials.append(mat)
        export_objs = [o for J in roots for o in [J] + list(J.children)]
        select_only(export_objs, roots[0])
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, atlas + ".fbx"), use_selection=True,
                                 apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                                 bake_space_transform=False, object_types={'MESH'}, mesh_smooth_type='FACE',
                                 use_mesh_modifiers=True, path_mode='STRIP')
        log(f"  exported {atlas}.fbx with {len(roots)} object(s)")
        bpy.ops.wm.save_mainfile()
    log("ALL DONE")


run()
