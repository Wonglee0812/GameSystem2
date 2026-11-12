"""Helper functions for building the Wind Pet lab in Blender.
Cartoon (Mario-like) style: chunky bevelled shapes with subdivision,
procedural materials with ambient-occlusion darkening in the creases.
Run through lab_build_all.py.
"""
import bpy, bmesh, math, random
from mathutils import Vector

R = math.radians


def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexlin(h):
    h = h.lstrip('#')
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4)) + (1.0,)


def new_mat(name):
    m = bpy.data.materials.get(name)
    if m:
        bpy.data.materials.remove(m)
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    return m, m.node_tree.nodes, m.node_tree.links


def ao_mix(nodes, links, color_socket, bsdf, strength=0.55):
    """Darken creases and contact areas: soft cartoon depth."""
    ao = nodes.new("ShaderNodeAmbientOcclusion")
    ao.inputs["Distance"].default_value = 0.6
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = 'RGBA'
    mix.blend_type = 'MULTIPLY'
    mix.inputs[0].default_value = strength
    links.new(color_socket, mix.inputs[6])
    links.new(ao.outputs["AO"], mix.inputs[7])
    links.new(mix.outputs[2], bsdf.inputs["Base Color"])


def ramp(nodes, stops):
    r = nodes.new("ShaderNodeValToRGB")
    el = r.color_ramp.elements
    el[0].position, el[0].color = stops[0][0], hexlin(stops[0][1])
    el[1].position, el[1].color = stops[-1][0], hexlin(stops[-1][1])
    for pos, col in stops[1:-1]:
        e = el.new(pos)
        e.color = hexlin(col)
    return r


def mat_wood(name, light, mid, dark, scale=(1, 6, 1), rough=0.55, worn=0.0):
    m, nodes, links = new_mat(name)
    b = nodes.get("Principled BSDF")
    tc = nodes.new("ShaderNodeTexCoord")
    mp = nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = scale
    links.new(tc.outputs["Object"], mp.inputs["Vector"])
    wave = nodes.new("ShaderNodeTexWave")
    wave.wave_type = 'BANDS'
    wave.bands_direction = 'Y'
    wave.inputs["Scale"].default_value = 1.2
    wave.inputs["Distortion"].default_value = 2.5
    wave.inputs["Detail"].default_value = 1.5
    wave.inputs["Detail Scale"].default_value = 0.6
    links.new(mp.outputs["Vector"], wave.inputs["Vector"])
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 1.5
    noise.inputs["Detail"].default_value = 2
    links.new(mp.outputs["Vector"], noise.inputs["Vector"])
    mixf = nodes.new("ShaderNodeMath")
    mixf.operation = 'MULTIPLY_ADD'
    mixf.inputs[1].default_value = 0.4
    links.new(wave.outputs["Fac"], mixf.inputs[0])
    links.new(noise.outputs["Fac"], mixf.inputs[2])
    rp = ramp(nodes, [(0.2, dark), (0.55, mid), (0.95, light)])
    links.new(mixf.outputs[0], rp.inputs["Fac"])
    # Per-object brightness change, so planks are not identical.
    oi = nodes.new("ShaderNodeObjectInfo")
    hue = nodes.new("ShaderNodeHueSaturation")
    mr = nodes.new("ShaderNodeMapRange")
    mr.inputs["To Min"].default_value = 0.88
    mr.inputs["To Max"].default_value = 1.1
    links.new(oi.outputs["Random"], mr.inputs["Value"])
    links.new(mr.outputs["Result"], hue.inputs["Value"])
    links.new(rp.outputs["Color"], hue.inputs["Color"])
    color = hue.outputs["Color"]
    if worn > 0:
        n2 = nodes.new("ShaderNodeTexNoise")
        n2.inputs["Scale"].default_value = 1.2
        n2.inputs["Detail"].default_value = 6
        links.new(mp.outputs["Vector"], n2.inputs["Vector"])
        r2 = ramp(nodes, [(0.45, '#FFFFFF'), (0.7, '#B8A08A')])
        links.new(n2.outputs["Fac"], r2.inputs["Fac"])
        mul = nodes.new("ShaderNodeMix")
        mul.data_type = 'RGBA'
        mul.blend_type = 'MULTIPLY'
        mul.inputs[0].default_value = worn
        links.new(color, mul.inputs[6])
        links.new(r2.outputs["Color"], mul.inputs[7])
        color = mul.outputs[2]
    # knots: a few dark oval rings
    vor = nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 0.9
    links.new(mp.outputs["Vector"], vor.inputs["Vector"])
    kr = ramp(nodes, [(0.0, dark), (0.06, mid), (0.1, '#FFFFFF')])
    links.new(vor.outputs["Distance"], kr.inputs["Fac"])
    mk = nodes.new("ShaderNodeMix")
    mk.data_type = 'RGBA'
    mk.blend_type = 'MULTIPLY'
    mk.inputs[0].default_value = 0.6
    links.new(color, mk.inputs[6])
    links.new(kr.outputs["Color"], mk.inputs[7])
    color = add_surface_detail(nodes, links, tc, mk.outputs[2], b, rough, 0.0, 0.25)
    ao_mix(nodes, links, color, b)
    # grain bump + fine scratches bump, chained
    fine = nodes.new("ShaderNodeTexNoise")
    fine.inputs["Scale"].default_value = 60
    fine.inputs["Detail"].default_value = 10
    links.new(mp.outputs["Vector"], fine.inputs["Vector"])
    bump1 = nodes.new("ShaderNodeBump")
    bump1.inputs["Strength"].default_value = 0.12
    links.new(wave.outputs["Fac"], bump1.inputs["Height"])
    bump2 = nodes.new("ShaderNodeBump")
    bump2.inputs["Strength"].default_value = 0.06
    links.new(fine.outputs["Fac"], bump2.inputs["Height"])
    links.new(bump1.outputs["Normal"], bump2.inputs["Normal"])
    links.new(bump2.outputs["Normal"], b.inputs["Normal"])
    m.diffuse_color = hexlin(mid)
    return m


def mat_paint(name, base, var, rough=0.6, stripes=None, metal=0.0, noise_scale=6, ao=0.55, bump=1.0, grime=0.3):
    m, nodes, links = new_mat(name)
    b = nodes.get("Principled BSDF")
    tc = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = 4
    links.new(tc.outputs["Object"], noise.inputs["Vector"])
    rp = ramp(nodes, [(0.35, base), (0.75, var)])
    links.new(noise.outputs["Fac"], rp.inputs["Fac"])
    color = rp.outputs["Color"]
    if stripes:
        wave = nodes.new("ShaderNodeTexWave")
        wave.bands_direction = 'X'
        wave.inputs["Scale"].default_value = stripes[0]
        links.new(tc.outputs["Generated"], wave.inputs["Vector"])
        gt = nodes.new("ShaderNodeMath")
        gt.operation = 'GREATER_THAN'
        gt.inputs[1].default_value = 0.5
        links.new(wave.outputs["Fac"], gt.inputs[0])
        st = nodes.new("ShaderNodeMix")
        st.data_type = 'RGBA'
        links.new(gt.outputs[0], st.inputs[0])
        links.new(color, st.inputs[6])
        st.inputs[7].default_value = hexlin(stripes[1])
        mixs = nodes.new("ShaderNodeMix")
        mixs.data_type = 'RGBA'
        mixs.inputs[0].default_value = 0.35
        links.new(color, mixs.inputs[6])
        links.new(st.outputs[2], mixs.inputs[7])
        color = mixs.outputs[2]
    color = add_surface_detail(nodes, links, tc, color, b, rough, bump, grime)
    ao_mix(nodes, links, color, b, ao)
    b.inputs["Metallic"].default_value = metal
    m.diffuse_color = hexlin(base)
    return m


def add_surface_detail(nodes, links, tc, color, b, rough, bump=1.0, grime=0.3):
    """Grime patches, a slightly different shade per object, rough/glossy
    variation and a fine bumpy surface, so nothing looks plastic-clean."""
    # grime: big soft darker patches
    g = nodes.new("ShaderNodeTexNoise")
    g.inputs["Scale"].default_value = 1.3
    g.inputs["Detail"].default_value = 6
    links.new(tc.outputs["Object"], g.inputs["Vector"])
    gr = ramp(nodes, [(0.42, '#FFFFFF'), (0.72, '#9C8466')])
    links.new(g.outputs["Fac"], gr.inputs["Fac"])
    mg = nodes.new("ShaderNodeMix")
    mg.data_type = 'RGBA'
    mg.blend_type = 'MULTIPLY'
    mg.inputs[0].default_value = grime
    links.new(color, mg.inputs[6])
    links.new(gr.outputs["Color"], mg.inputs[7])
    # per-object brightness
    oi = nodes.new("ShaderNodeObjectInfo")
    mr = nodes.new("ShaderNodeMapRange")
    mr.inputs["To Min"].default_value = 0.93
    mr.inputs["To Max"].default_value = 1.06
    links.new(oi.outputs["Random"], mr.inputs["Value"])
    hs = nodes.new("ShaderNodeHueSaturation")
    links.new(mr.outputs["Result"], hs.inputs["Value"])
    links.new(mg.outputs[2], hs.inputs["Color"])
    # roughness variation
    rn = nodes.new("ShaderNodeMapRange")
    rn.inputs["To Min"].default_value = max(0.0, rough - 0.12)
    rn.inputs["To Max"].default_value = min(1.0, rough + 0.12)
    links.new(g.outputs["Fac"], rn.inputs["Value"])
    links.new(rn.outputs["Result"], b.inputs["Roughness"])
    # fine bumpy surface
    if bump > 0:
        fn = nodes.new("ShaderNodeTexNoise")
        fn.inputs["Scale"].default_value = 35
        fn.inputs["Detail"].default_value = 8
        links.new(tc.outputs["Object"], fn.inputs["Vector"])
        bp = nodes.new("ShaderNodeBump")
        bp.inputs["Strength"].default_value = 0.08 * bump
        bp.inputs["Distance"].default_value = 0.02
        links.new(fn.outputs["Fac"], bp.inputs["Height"])
        links.new(bp.outputs["Normal"], b.inputs["Normal"])
    return hs.outputs["Color"]


def mat_glass(name, tint='#CDEBFF', alpha=0.3):
    m, nodes, links = new_mat(name)
    b = nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = hexlin(tint)
    b.inputs["Roughness"].default_value = 0.03
    b.inputs["Alpha"].default_value = alpha
    try:
        m.surface_render_method = 'BLENDED'
    except Exception:
        pass
    m.diffuse_color = hexlin(tint)[:3] + (alpha,)
    return m


def mat_emit(name, col, strength=2.0):
    m, nodes, links = new_mat(name)
    b = nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = hexlin(col)
    b.inputs["Emission Color"].default_value = hexlin(col)
    b.inputs["Emission Strength"].default_value = strength
    m.diffuse_color = hexlin(col)
    return m


def coll(name, parent=None):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(c)
    return c


def clear_collection(c):
    for o in list(c.all_objects):
        bpy.data.objects.remove(o, do_unlink=True)


def place(o, collection, parent=None, material=None):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    collection.objects.link(o)
    if material is not None and o.type == 'MESH':
        o.data.materials.clear()
        o.data.materials.append(material)
    if o.type == 'MESH':
        for p in o.data.polygons:
            p.use_smooth = True
    if parent is not None:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    return o


def soften(o, width, segments=3, subd=1):
    if width > 0:
        b = o.modifiers.new("Bevel", 'BEVEL')
        b.width = width
        b.segments = segments
        b.limit_method = 'NONE'
    if subd > 0:
        s = o.modifiers.new("Subdivision", 'SUBSURF')
        s.levels = subd
        s.render_levels = subd
    o.modifiers.new("WeightedNormal", 'WEIGHTED_NORMAL')


def rbox(name, size, loc, material, collection, bevel=0.12, parent=None, rot=(0, 0, 0), subd=1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    soften(o, min(bevel, min(size) / 2 - 0.002), 3, subd)
    return place(o, collection, parent, material)


def cyl(name, r, depth, loc, material, collection, rot=(0, 0, 0), bevel=0.04, parent=None, verts=48, subd=1):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    soften(o, min(bevel, r / 2, depth / 2 - 0.002), 3, subd)
    return place(o, collection, parent, material)


def sph(name, r, loc, material, collection, scale=(1, 1, 1), parent=None, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=r, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return place(o, collection, parent, material)


def tor(name, Rr, r, loc, material, collection, rot=(0, 0, 0), parent=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=Rr, minor_radius=r, major_segments=64, minor_segments=20,
                                     location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    return place(o, collection, parent, material)


def empty(name, loc, collection, parent=None):
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, None)
        o.empty_display_size = 0.5
    for c in list(o.users_collection):
        c.objects.unlink(o)
    collection.objects.link(o)
    if parent is not None:
        # keep 'loc' in world space under the parent (fixes doubled positions)
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    o.location = loc
    bpy.context.view_layer.update()   # make matrix_world valid before children use it
    return o


def screw(name, loc, collection, parent, mat, rot=(R(90), 0, 0), r=0.05):
    return cyl(name, r, 0.03, loc, mat, collection, rot, 0.012, parent, 16, 1)


# ---------------- v4 additions: arches, outline, text, paper notes ----------------

def _mesh_object(name, verts, faces, collection, material, parent=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return place(o, collection, parent, material)


def arch_ring(name, center, r_out, r_in, depth, material, collection, parent=None, segs=40):
    """Half ring (top half of an annulus) facing -Y, for an arched window frame."""
    cx, cy, cz = center
    verts, faces = [], []
    for i in range(segs + 1):
        a = math.pi * i / segs
        for (r, y) in ((r_out, cy - depth / 2), (r_in, cy - depth / 2), (r_out, cy + depth / 2), (r_in, cy + depth / 2)):
            verts.append((cx + r * math.cos(a), y, cz + r * math.sin(a)))
    for i in range(segs):
        a, b = i * 4, (i + 1) * 4
        faces += [(a, b, b + 1, a + 1),          # front
                  (a + 2, a + 3, b + 3, b + 2),  # back
                  (a, a + 2, b + 2, b),          # outer
                  (a + 1, b + 1, b + 3, a + 3)]  # inner
    n = segs * 4
    faces += [(0, 1, 3, 2), (n, n + 2, n + 3, n + 1)]
    o = _mesh_object(name, verts, faces, collection, material, parent)
    soften(o, min(0.06, (r_out - r_in) / 2 - 0.002), 2, 1)
    return o


def arch_panel(name, center, width, rect_h, y, material, collection, parent=None, segs=40):
    """Flat shape: rectangle with a half circle on top (for sky / glass)."""
    cx, _, cz = center
    r = width / 2
    pts = [(cx - r, cz - rect_h), (cx + r, cz - rect_h)]
    for i in range(segs + 1):
        a = math.pi * i / segs
        pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    verts = [(x, y, z) for x, z in pts]
    o = _mesh_object(name, verts, [tuple(range(len(verts)))], collection, material, parent)
    return o


OUTLINE_MAT = None


def outline_material():
    global OUTLINE_MAT
    m = bpy.data.materials.get("L3_Outline")
    if m is None:
        m, nodes, links = new_mat("L3_Outline")
        b = nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = hexlin('#2B1A10')
        b.inputs["Roughness"].default_value = 1.0
        m.diffuse_color = hexlin('#2B1A10')
    m.use_backface_culling = True
    OUTLINE_MAT = m
    return m


def add_outline(o, thickness):
    """Ink outline like the reference drawing: inverted hull with a dark,
    back-face-culled material."""
    if o.type != 'MESH' or o.modifiers.get("Outline"):
        return
    m = outline_material()
    o.data.materials.append(m)
    s = o.modifiers.new("Outline", 'SOLIDIFY')
    s.thickness = -abs(thickness)
    s.offset = 1.0
    s.use_flip_normals = True
    s.use_rim = False
    s.material_offset = len(o.data.materials) - 1


def text_mesh(name, body, loc, size, depth, material, collection, parent=None, rot=(R(90), 0, 0)):
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = body
    cu.size = size
    cu.extrude = depth
    cu.bevel_depth = depth * 0.4
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    o = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = rot
    bpy.context.view_layer.objects.active = o
    for ob in bpy.context.selected_objects:
        ob.select_set(False)
    o.select_set(True)
    bpy.ops.object.convert(target='MESH')
    o = bpy.context.active_object
    o.name = name
    return place(o, collection, parent, material)


def paper_note(name, loc, w, h, collection, parent=None, tilt=0.0, pin_mat=None):
    p = rbox(name, (w, 0.02, h), loc, bpy.data.materials['L3_Paper'], collection, 0.005, parent, (0, R(tilt), 0), 0)
    # scribble lines
    for i in range(3):
        rbox(name + f"_Line{i}", (w * 0.65, 0.01, 0.025), (loc[0], loc[1] - 0.015, loc[2] + h * 0.25 - i * h * 0.2),
             bpy.data.materials['L3_Ink'], collection, 0.004, parent, (0, R(tilt), 0), 0)
    if pin_mat is not None:
        sph(name + "_Pin", 0.05, (loc[0], loc[1] - 0.04, loc[2] + h * 0.42), pin_mat, collection, (1, 0.6, 1), parent)
    return p


def flat_shape(name, pts, thickness, material, collection, parent=None, rim_mat=None, rim=0.0):
    """Filled 2D outline (x, y) extruded to a thin plate, optional rounded rim."""
    def make(nm, fill, depth, bevel):
        cu = bpy.data.curves.new(nm, 'CURVE')
        cu.dimensions = '2D'
        cu.fill_mode = 'BOTH' if fill else 'NONE'
        cu.extrude = depth
        cu.bevel_depth = bevel
        cu.bevel_resolution = 3
        sp = cu.splines.new('POLY')
        sp.points.add(len(pts) - 1)
        for i, (x, y) in enumerate(pts):
            sp.points[i].co = (x, y, 0, 1)
        sp.use_cyclic_u = True
        ob = bpy.data.objects.new(nm, cu)
        bpy.context.scene.collection.objects.link(ob)
        for o2 in bpy.context.selected_objects:
            o2.select_set(False)
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        bpy.ops.object.convert(target='MESH')
        ob = bpy.context.active_object
        ob.name = nm
        return ob
    plate = make(name, True, thickness, 0.0)
    place(plate, collection, None, material)
    rim_o = None
    if rim > 0 and rim_mat is not None:
        rim_o = make(name + "_Rim", False, 0.0, rim)
        place(rim_o, collection, None, rim_mat)
    return plate, rim_o
