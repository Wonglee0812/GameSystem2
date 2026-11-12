"""Step 5: decor that gives the lab its storybook feel: hanging lantern,
pipes with a red valve, a LAB sign, pinned notes and the clutter on the desk.
All original items, no characters or items from other games."""
import bmesh
mat = bpy.data.materials
root = coll("LabV2")
deco = coll("LabV2_Decor", root)
clear_collection(deco)
random.seed(11)
DESK = -1.0
WALL_Y = 5.6


def cone(name, r1, r2, depth, loc, material, parent=None, rot=(0, 0, 0), bevel=0.03):
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    soften(o, bevel, 3, 1)
    return place(o, deco, parent, material)


# ---------- Hanging lantern ----------
LX, LY = 1.9, 4.3
lan = empty("Lantern", (LX, LY, 2.55), deco)
for i in range(8):
    tor(f"Lantern_Chain{i}", 0.06, 0.018, (LX, LY, 2.95 + i * 0.13), mat['L2_Metal'], deco, (0, R(90 * (i % 2)), 0), lan)
cone("Lantern_Roof", 0.38, 0.08, 0.3, (LX, LY, 2.85), mat['L2_Metal'], lan)
cyl("Lantern_Glass", 0.26, 0.5, (LX, LY, 2.5), mat['L3_LanternGlow'], deco, (0, 0, 0), 0.05, lan)
for k in range(4):
    a = R(45 + 90 * k)
    cyl(f"Lantern_Bar{k}", 0.025, 0.55, (LX + 0.27 * math.cos(a), LY + 0.27 * math.sin(a), 2.5), mat['L2_Metal'], deco, (0, 0, 0), 0.01, lan, 12)
cyl("Lantern_Base", 0.32, 0.1, (LX, LY, 2.22), mat['L2_Metal'], deco, (0, 0, 0), 0.04, lan)
light = bpy.data.objects.new("Lantern_Light", bpy.data.lights.new("Lantern_Light", 'POINT'))
deco.objects.link(light)
light.location = (LX, LY - 0.4, 2.45)
light.data.energy = 250
light.data.color = (1.0, 0.75, 0.4)
light.data.shadow_soft_size = 0.4

# ---------- Pipes with a red valve on the right ----------
PXp = 7.75
PY = WALL_Y - 0.3
pip = empty("Pipes", (PXp, PY, 0), deco)
cyl("Pipe_Vertical", 0.17, 4.2, (PXp, PY, DESK + 2.3), mat['L2_Metal'], deco, (0, 0, 0), 0.03, pip)
tor("Pipe_Elbow", 0.35, 0.17, (PXp + 0.35, PY, DESK + 4.4), mat['L2_Metal'], deco, (R(90), 0, 0), pip)
for k, z in enumerate((DESK + 0.6, DESK + 2.0, DESK + 3.5)):
    cyl(f"Pipe_Joint{k}", 0.21, 0.18, (PXp, PY, z), mat['L2_Metal'], deco, (0, 0, 0), 0.05, pip)
    rbox(f"Pipe_Bracket{k}", (0.5, 0.35, 0.1), (PXp, PY + 0.15, z + 0.2), mat['L2_DarkPlastic'], deco, 0.03, pip)
cyl("Valve_Stem", 0.06, 0.4, (PXp, PY - 0.3, DESK + 2.6), mat['L2_Brass'], deco, (R(90), 0, 0), 0.02, pip, 16)
tor("Valve_Wheel", 0.32, 0.05, (PXp, PY - 0.5, DESK + 2.6), mat['L3_RedValve'], deco, (R(90), 0, 0), pip)
for k in range(3):
    rbox(f"Valve_Spoke{k}", (0.6, 0.06, 0.06), (PXp, PY - 0.5, DESK + 2.6), mat['L3_RedValve'], deco, 0.02, pip, (0, R(60 * k), 0), 0)
cyl("Valve_Cap", 0.08, 0.08, (PXp, PY - 0.55, DESK + 2.6), mat['L2_Brass'], deco, (R(90), 0, 0), 0.02, pip, 16)

# ---------- LAB sign (between the shelf and the window) ----------
sg = empty("LabSign", (2.75, WALL_Y - 0.12, DESK + 3.75), deco)
rbox("Sign_Board", (1.0, 0.1, 0.48), (2.75, WALL_Y - 0.12, DESK + 3.75), mat['L2_DeskWood'], deco, 0.05, sg, (0, R(-4), 0))
text_mesh("Sign_Text", "LAB", (2.75, WALL_Y - 0.2, DESK + 3.74), 0.32, 0.02, mat['L3_LeatherGreen'], deco, sg, (R(90), R(-4), 0))
screw("Sign_Nail", (2.75, WALL_Y - 0.15, DESK + 4.12), deco, sg, mat['L2_Brass'])

# ---------- Notes and a wind sketch pinned on the wall ----------
paper_note("Note_Wall0", (2.55, WALL_Y - 0.04, DESK + 2.45), 0.6, 0.75, deco, None, -6, mat['L3_RedValve'])
sk = empty("WindSketch", (2.75, WALL_Y - 0.04, DESK + 1.3), deco)
rbox("Sketch_Paper", (0.85, 0.02, 1.05), (2.75, WALL_Y - 0.04, DESK + 1.3), mat['L3_Paper'], deco, 0.005, sk, (0, R(3), 0), 0)
for k, (r, dz) in enumerate(((0.22, 0.2), (0.14, -0.1), (0.08, -0.32))):
    tor(f"Sketch_Swirl{k}", r, 0.012, (2.75, WALL_Y - 0.06, DESK + 1.3 + dz), mat['L3_Ink'], deco, (R(90), 0, 0), sk)
sph("Sketch_Pin", 0.05, (2.75, WALL_Y - 0.08, DESK + 1.78), mat['L2_Brass'], deco, (1, 0.6, 1), sk)

# ---------- Desk clutter (kept away from the centre, where the pet flies) ----------
# cauldron with a bubbling brew
cx, cy = -2.6, 3.4
cau = empty("Cauldron", (cx, cy, DESK), deco)
pot = sph("Cauldron_Pot", 0.7, (cx, cy, DESK + 0.62), mat['L3_CauldronIron'], deco, (1, 1, 0.8), cau)
# open the pot: cut off the top cap, otherwise it pokes up through the brew
bm = bmesh.new()
bm.from_mesh(pot.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z > 0.46], context='VERTS')
bm.to_mesh(pot.data)
bm.free()
tor("Cauldron_Rim", 0.58, 0.09, (cx, cy, DESK + 1.12), mat['L3_CauldronIron'], deco, (0, 0, 0), cau)
cyl("Cauldron_Brew", 0.56, 0.06, (cx, cy, DESK + 1.1), mat['L3_CauldronBrew'], deco, (0, 0, 0), 0.02, cau)
for k in range(3):
    a = R(120 * k + 30)
    cyl(f"Cauldron_Leg{k}", 0.08, 0.3, (cx + 0.45 * math.cos(a), cy + 0.45 * math.sin(a), DESK + 0.12), mat['L3_CauldronIron'], deco,
        (0, 0, 0), 0.03, cau, 16)
for k in range(2):
    tor(f"Cauldron_Handle{k}", 0.15, 0.035, (cx + (k * 2 - 1) * 0.72, cy, DESK + 0.85), mat['L3_CauldronIron'], deco, (R(90), 0, 0), cau)
# the rising bubbles are a particle system in Unity, not meshes

# flasks with glowing liquid
fl = empty("FlaskGroup", (-4.4, 2.6, DESK), deco)
sph("RoundFlask_Liquid", 0.42, (-4.8, 2.6, DESK + 0.42), mat['L2_Liquid0'], deco, (1, 1, 0.9), fl)
sph("RoundFlask_Glass", 0.47, (-4.8, 2.6, DESK + 0.47), mat['L2_Glass'], deco, (1, 1, 1), fl)
cyl("RoundFlask_Neck", 0.12, 0.5, (-4.8, 2.6, DESK + 1.1), mat['L2_Glass'], deco, (0, 0, 0), 0.03, fl)
cyl("RoundFlask_Cork", 0.13, 0.16, (-4.8, 2.6, DESK + 1.4), mat['L2_ShelfWood'], deco, (0, 0, 0), 0.04, fl)
cone("Erlen_Liquid", 0.36, 0.14, 0.45, (-3.7, 2.2, DESK + 0.23), mat['L2_Liquid1'], fl)
cone("Erlen_Glass", 0.4, 0.13, 0.8, (-3.7, 2.2, DESK + 0.4), mat['L2_Glass'], fl)
cyl("Tall_Liquid", 0.14, 0.45, (-5.6, 3.2, DESK + 0.25), mat['L2_Liquid2'], deco, (0, 0, 0), 0.04, fl)
cyl("Tall_Glass", 0.17, 1.0, (-5.6, 3.2, DESK + 0.5), mat['L2_Glass'], deco, (0, 0, 0), 0.05, fl)

# test-tube rack (behind the pet, a little to the right)
tr = empty("TestTubeRack", (1.3, 3.7, DESK), deco)
rbox("Rack_Base", (1.4, 0.5, 0.12), (1.3, 3.7, DESK + 0.06), mat['L2_DeskWood'], deco, 0.04, tr)
rbox("Rack_Top", (1.4, 0.5, 0.1), (1.3, 3.7, DESK + 0.6), mat['L2_DeskWood'], deco, 0.04, tr)
for k in (-1, 1):
    rbox(f"Rack_Side{k}", (0.1, 0.5, 0.6), (1.3 + k * 0.65, 3.7, DESK + 0.33), mat['L2_DeskWood'], deco, 0.03, tr)
for k, liq in enumerate(('L3_LiquidPink', 'L3_LiquidBlue', 'L2_Liquid0', 'L2_Liquid2')):
    x = 1.3 - 0.42 + k * 0.28
    cyl(f"Tube{k}_Liquid", 0.07, 0.35, (x, 3.7, DESK + 0.3), mat[liq], deco, (0, 0, 0), 0.03, tr, 16)
    cyl(f"Tube{k}_Glass", 0.085, 0.8, (x, 3.7, DESK + 0.45), mat['L2_Glass'], deco, (0, 0, 0), 0.04, tr, 16)

# brass apparatus next to the item machine
ap = empty("BrassApparatus", (3.1, 3.2, DESK), deco)
rbox("App_Base", (1.2, 0.8, 0.25), (3.1, 3.2, DESK + 0.12), mat['L2_DarkWood'], deco, 0.06, ap)
cyl("App_Boiler", 0.32, 0.8, (2.85, 3.2, DESK + 0.65), mat['L2_Brass'], deco, (0, 0, 0), 0.1, ap)
sph("App_Dome", 0.32, (2.85, 3.2, DESK + 1.05), mat['L2_Brass'], deco, (1, 1, 0.6), ap)
tor("App_Coil", 0.25, 0.04, (3.35, 3.2, DESK + 1.0), mat['L3_Copper'], deco, (R(90), 0, 0), ap)
cyl("App_Pipe", 0.05, 0.7, (3.55, 3.2, DESK + 0.6), mat['L3_Copper'], deco, (0, 0, 0), 0.02, ap, 16)
cyl("App_Gauge", 0.17, 0.06, (2.85, 2.86, DESK + 0.75), mat['L2_Cream'], deco, (R(90), 0, 0), 0.02, ap)
tor("App_GaugeRim", 0.17, 0.025, (2.85, 2.84, DESK + 0.75), mat['L2_Brass'], deco, (R(90), 0, 0), ap)
rbox("App_GaugeNeedle", (0.02, 0.01, 0.12), (2.88, 2.82, DESK + 0.78), mat['L3_RedValve'], deco, 0.005, ap, (0, R(-35), 0), 0)

# open book, scroll, ink bottle, magnifying glass, books
BX, BY = -2.0, -1.75
bk = empty("OpenBook", (BX, BY, DESK), deco)
rbox("Book_Cover", (2.0, 1.4, 0.06), (BX, BY, DESK + 0.03), mat['L3_Leather'], deco, 0.03, bk, (0, 0, R(8)))
for k in (-1, 1):
    rbox(f"Book_Pages{k}", (0.92, 1.28, 0.12), (BX + k * 0.48, BY, DESK + 0.12), mat['L3_Paper'], deco, 0.04, bk, (0, R(-k * 5), R(8)))
for k in range(4):
    rbox(f"Book_Text{k}", (0.6, 0.03, 0.02), (BX - 0.45, BY + 0.4 - k * 0.22, DESK + 0.2), mat['L3_Ink'], deco, 0.005, bk, (0, 0, R(8)), 0)
tor("Book_Sketch", 0.18, 0.015, (BX + 0.45, BY, DESK + 0.2), mat['L3_Ink'], deco, (0, 0, 0), bk)
sc_ = empty("Scroll", (-5.6, 1.2, DESK), deco)
cyl("Scroll_Roll", 0.12, 1.3, (-5.6, 1.2, DESK + 0.12), mat['L3_Paper'], deco, (0, R(90), R(20)), 0.04, sc_)
rbox("Scroll_Flat", (1.2, 0.7, 0.02), (-5.5, 1.6, DESK + 0.01), mat['L3_Paper'], deco, 0.01, sc_, (0, 0, R(20)), 0)
ink = empty("InkBottle", (4.3, 1.4, DESK), deco)
cyl("Ink_Bottle", 0.2, 0.32, (4.3, 1.4, DESK + 0.16), mat_glass('L3_InkGlass', '#5A3B8C', 0.85), deco, (0, 0, 0), 0.07, ink)
cyl("Ink_Cork", 0.09, 0.12, (4.3, 1.4, DESK + 0.38), mat['L2_ShelfWood'], deco, (0, 0, 0), 0.03, ink)
cyl("Quill", 0.03, 1.1, (4.45, 1.45, DESK + 0.6), mat['L2_Cream'], deco, (R(-20), R(15), 0), 0.01, ink, 12)
mg = empty("Magnifier", (1.7, -2.0, DESK), deco)
tor("Magnifier_Ring", 0.3, 0.045, (1.7, -2.0, DESK + 0.05), mat['L2_Brass'], deco, (0, 0, 0), mg)
cyl("Magnifier_Lens", 0.28, 0.02, (1.7, -2.0, DESK + 0.05), mat['L2_Glass'], deco, (0, 0, 0), 0.005, mg)
cyl("Magnifier_Handle", 0.06, 0.55, (2.25, -2.3, DESK + 0.06), mat['L2_DarkWood'], deco, (0, R(90), R(-30)), 0.02, mg, 16)
for k in range(2):
    rbox(f"DeskBook{k}", (1.1, 0.8, 0.22), (-4.85, -0.45, DESK + 0.11 + k * 0.23), mat['L3_Leather'] if k == 0 else mat['L3_LeatherGreen'],
         deco, 0.06, None, (0, 0, R(-10 + k * 14)))

# potted plant on the right
pp = empty("PottedPlant", (4.3, 2.5, DESK), deco)
cone("Plant_Pot", 0.38, 0.5, 0.65, (4.3, 2.5, DESK + 0.33), mat['L3_Terracotta'], pp)
tor("Plant_PotRim", 0.5, 0.07, (4.3, 2.5, DESK + 0.65), mat['L3_Terracotta'], deco, (0, 0, 0), pp)
for k in range(7):
    a = R(360 * k / 7)
    leaf = sph(f"Plant_Leaf{k}", 0.3, (4.3 + 0.3 * math.cos(a), 2.5 + 0.3 * math.sin(a), DESK + 1.05 + 0.1 * (k % 2)), mat['L3_Leaf'],
               deco, (0.45, 1, 0.18), pp)
    leaf.rotation_euler = (R(35), 0, a + R(90))
