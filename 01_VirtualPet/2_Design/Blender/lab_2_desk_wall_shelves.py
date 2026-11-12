"""Step 2: plaster wall with brick patches, the big old desk and the rustic
shelf unit on the left (layout kept from v3)."""
mat = bpy.data.materials
root = coll("LabV2")
room = coll("LabV2_Room", root)
furn = coll("LabV2_Shelves", root)
clear_collection(room)
clear_collection(furn)
random.seed(4)
DESK = -1.0
WALL_Y = 5.6

# ---------- Big old wooden desk ----------
desk = empty("Desk", (0, 1.2, DESK), room)
plank_d = 1.36
for i in range(6):
    y = -3.0 + plank_d * (i + 0.5)
    z = DESK - 0.12 + random.uniform(-0.02, 0.02)
    rbox(f"Desk_Plank{i}", (20.5, plank_d - 0.07, 0.24), (random.uniform(-0.15, 0.15), y, z), mat['L2_DeskWood'], room, 0.08,
         desk, (random.uniform(-0.006, 0.006), 0, random.uniform(-0.004, 0.004)))
    for sx in (-9.2, 9.2):
        for sy in (-0.42, 0.42):
            screw(f"Desk_Nail{i}_{sx}_{sy}", (sx + random.uniform(-0.2, 0.2), y + sy, z + 0.125), room, desk,
                  mat['L2_Metal'], (0, 0, 0), 0.05)
rbox("Desk_FrontEdge", (20.8, 0.5, 0.6), (0, -3.15, DESK - 0.4), mat['L2_DarkWood'], room, 0.12, desk)
# stains, spilled potion and scratches
ring = tor("Desk_CoffeeRing", 0.32, 0.035, (-6.2, -1.6, DESK + 0.006), mat['L2_Stain'], room, (0, 0, 0), desk)
ring.scale = (1, 1, 0.08)
for i, (x, y, s) in enumerate([(-1.6, 3.3, 0.55), (6.6, -0.6, 0.4)]):
    sp = cyl(f"Desk_Spill{i}", s, 0.01, (x, y, DESK + 0.004), mat['L2_Liquid0'] if i == 0 else mat['L2_Stain'], room,
             (0, 0, 0), 0.0, desk, 24, 0)
    sp.scale = (1.4, 0.8, 1)
for i in range(6):
    rbox(f"Desk_Scratch{i}", (random.uniform(0.5, 1.3), 0.03, 0.02), (random.uniform(-8, 8), random.uniform(-2.2, 3.5), DESK + 0.003),
         mat['L2_Stain'], room, 0.008, desk, (0, 0, R(random.uniform(-25, 25))), 0)

# ---------- Wall: warm plaster, brick patches, cracks ----------
rbox("Wall_Back", (22, 0.5, 9), (0, WALL_Y + 0.25, DESK + 3.5), mat['L2_Wallpaper'], room, 0.05, None, (0, 0, 0), 0)
rbox("Wall_DeskTrim", (22, 0.3, 0.3), (0, WALL_Y - 0.05, DESK + 0.15), mat['L2_WallTrim'], room, 0.1)
bricks = empty("Wall_Bricks", (0, WALL_Y, 0), room)


def brick_patch(cx, cz, cols, rows):
    for r in range(rows):
        for c in range(cols - (r % 2)):
            if random.random() < 0.18:
                continue          # missing bricks look more natural
            x = cx + (c - cols / 2) * 0.62 + (0.31 if r % 2 else 0)
            z = cz + r * 0.3
            rbox(f"Brick_{cx:.1f}_{r}_{c}", (0.56 + random.uniform(-0.04, 0.04), 0.14, 0.24),
                 (x, WALL_Y - 0.03 - random.uniform(0, 0.04), z), mat['L3_Brick'], room, 0.05, bricks,
                 (0, R(random.uniform(-2, 2)), 0))


brick_patch(-9.4, DESK + 3.45, 2, 3)   # above the shelf side
brick_patch(-1.8, DESK + 0.35, 4, 2)   # low, under the shelf, clear of the wall notes
brick_patch(0.4, DESK + 3.6, 3, 2)
brick_patch(8.9, DESK + 1.0, 2, 4)    # right of the pipe brackets
for i, (x, z, rot) in enumerate([(-6.0, DESK + 4.1, 35), (2.9, DESK + 2.6, -50), (-1.2, DESK + 0.9, 20)]):
    rbox(f"Wall_Crack{i}", (0.7, 0.02, 0.025), (x, WALL_Y - 0.01, z), mat['L3_Crack'], room, 0.006, None, (0, R(rot), 0), 0)
    rbox(f"Wall_Crack{i}b", (0.35, 0.02, 0.022), (x + 0.35, WALL_Y - 0.01, z - 0.15), mat['L3_Crack'], room, 0.006, None,
         (0, R(rot - 40), 0), 0)

# ---------- Rustic shelf unit on the left ----------
SX0, SX1 = -8.6, 1.6
W = SX1 - SX0
scx = (SX0 + SX1) / 2
SY = WALL_Y - 0.55
Z0, Z1 = DESK + 1.0, DESK + 3.15         # bottom and top of the unit
unit = empty("ShelfUnit", (scx, SY, Z0), furn)
for k, x in enumerate((SX0, SX1)):
    rbox(f"Shelf_Side{k}", (0.28, 1.1, Z1 - Z0 + 0.3), (x, SY, (Z0 + Z1) / 2 - 0.1), mat['L2_ShelfWood'], furn, 0.1, unit)
    cyl(f"Shelf_Corbel{k}", 0.35, 0.28, (x, SY + 0.2, Z0 - 0.25), mat['L2_ShelfWood'], furn, (0, R(90), 0), 0.08, unit)
for k, z in enumerate((Z0, (Z0 + Z1) / 2, Z1)):
    rbox(f"Shelf_Board{k}", (W + 0.35, 1.15, 0.24), (scx, SY, z), mat['L2_ShelfWood'], furn, 0.09, unit)
for k in range(5):  # little hooks under the unit
    x = SX0 + 1.0 + k * (W - 2.0) / 4
    cyl(f"Shelf_HookBase{k}", 0.07, 0.08, (x, SY - 0.4, Z0 - 0.2), mat['L2_Brass'], furn, (R(90), 0, 0), 0.02, unit, 16)
    tor(f"Shelf_Hook{k}", 0.08, 0.025, (x, SY - 0.48, Z0 - 0.3), mat['L2_Brass'], furn, (0, R(90), 0), unit)

LOWZ = Z0 + 0.12
MIDZ = (Z0 + Z1) / 2 + 0.12


def bottle(name, x, z, h, r, liquid, label=True):
    b = empty(name, (x, SY - 0.1, z), furn)
    cyl(name + "_Liquid", r * 0.86, h * 0.6, (x, SY - 0.1, z + h * 0.32), liquid, furn, (0, 0, 0), 0.05, b)
    cyl(name + "_Glass", r, h * 0.8, (x, SY - 0.1, z + h * 0.4), mat['L2_Glass'], furn, (0, 0, 0), r * 0.4, b)
    cyl(name + "_Neck", r * 0.4, h * 0.25, (x, SY - 0.1, z + h * 0.88), mat['L2_Glass'], furn, (0, 0, 0), 0.02, b)
    cyl(name + "_Cork", r * 0.45, h * 0.12, (x, SY - 0.1, z + h * 1.02), mat['L2_ShelfWood'], furn, (0, 0, 0), 0.03, b)
    if label:
        rbox(name + "_Label", (r * 1.1, 0.02, h * 0.28), (x, SY - 0.1 - r, z + h * 0.4), mat['L3_Paper'], furn, 0.01, b,
             (0, 0, 0), 0)
    return b


# middle shelf: potion bottles, a brass gadget, an alarm clock, papers, books
bottle("Potion0", -8.0, MIDZ, 1.0, 0.24, mat['L2_Liquid0'])
bottle("Potion1", -7.35, MIDZ, 0.85, 0.27, mat['L2_Liquid1'])
bottle("Potion2", -6.7, MIDZ, 0.95, 0.22, mat['L3_LiquidPink'])
gad = empty("BrassGadget", (-4.8, SY - 0.1, MIDZ), furn)
rbox("Gadget_Base", (1.0, 0.7, 0.2), (-4.8, SY - 0.1, MIDZ + 0.1), mat['L2_DarkWood'], furn, 0.06, gad)
cyl("Gadget_Boiler", 0.25, 0.55, (-5.0, SY - 0.1, MIDZ + 0.5), mat['L2_Brass'], furn, (0, 0, 0), 0.08, gad)
cyl("Gadget_Gear", 0.24, 0.07, (-4.55, SY - 0.42, MIDZ + 0.5), mat['L3_Copper'], furn, (R(90), 0, 0), 0.02, gad, 12)
for t in range(12):
    a = R(30 * t)
    rbox(f"Gadget_Tooth{t}", (0.07, 0.07, 0.08), (-4.55 + 0.27 * math.cos(a), SY - 0.42, MIDZ + 0.5 + 0.27 * math.sin(a)),
         mat['L3_Copper'], furn, 0.015, gad, (0, -a, 0), 0)
tor("Gadget_Pipe", 0.22, 0.04, (-4.78, SY - 0.1, MIDZ + 0.82), mat['L2_Brass'], furn, (R(90), 0, 0), gad)
clock = empty("AlarmClock", (-3.3, SY - 0.1, MIDZ), furn)
cyl("Clock_Body", 0.36, 0.25, (-3.3, SY - 0.15, MIDZ + 0.45), mat['L3_RedValve'], furn, (R(90), 0, 0), 0.08, clock)
cyl("Clock_Face", 0.29, 0.05, (-3.3, SY - 0.29, MIDZ + 0.45), mat['L2_Cream'], furn, (R(90), 0, 0), 0.02, clock)
rbox("Clock_HandH", (0.03, 0.02, 0.16), (-3.3, SY - 0.32, MIDZ + 0.52), mat['L3_Ink'], furn, 0.008, clock, (0, 0, 0), 0)
rbox("Clock_HandM", (0.03, 0.02, 0.22), (-3.23, SY - 0.32, MIDZ + 0.47), mat['L3_Ink'], furn, 0.008, clock, (0, R(60), 0), 0)
for k in (-1, 1):
    sph(f"Clock_Bell{k}", 0.13, (-3.3 + k * 0.24, SY - 0.15, MIDZ + 0.83), mat['L2_Brass'], furn, (1, 1, 0.75), clock)
    cyl(f"Clock_Leg{k}", 0.04, 0.16, (-3.3 + k * 0.22, SY - 0.15, MIDZ + 0.1), mat['L2_Brass'], furn, (0, R(k * 20), 0), 0.01,
        clock, 12)
for i in range(5):
    rbox(f"PaperStack{i}", (0.9, 0.65, 0.05), (-1.9 + random.uniform(-0.05, 0.05), SY - 0.1, MIDZ + 0.03 + i * 0.055),
         mat['L3_Paper'], furn, 0.01, None, (0, 0, R(random.uniform(-6, 6))), 0)
for i, h in enumerate((0.95, 0.85, 1.0)):
    rbox(f"BookMid{i}", (0.2, 0.75, h), (-0.4 + i * 0.23, SY - 0.1, MIDZ + h / 2), mat[f'L2_Book{i}'], furn, 0.05)
rbox("BookMid3", (0.22, 0.75, 0.9), (0.38, SY - 0.1, MIDZ + 0.44), mat['L2_Book3'], furn, 0.05, None, (0, R(-14), 0))

# top shelf: specimen jar, bottles, a crate, books lying down
jar = empty("SpecimenJar", (-7.6, SY - 0.1, Z1 + 0.12), furn)
cyl("Jar_Glass", 0.4, 1.0, (-7.6, SY - 0.1, Z1 + 0.62), mat['L2_Glass'], furn, (0, 0, 0), 0.12, jar)
cyl("Jar_Lid", 0.44, 0.16, (-7.6, SY - 0.1, Z1 + 1.15), mat['L2_Brass'], furn, (0, 0, 0), 0.06, jar)
sph("Jar_Specimen", 0.24, (-7.6, SY - 0.1, Z1 + 0.58), mat['L2_SpecimenGlow'], furn, (1, 1, 1.3), jar)
bottle("Potion3", -6.6, Z1 + 0.12, 0.8, 0.2, mat['L2_Liquid2'])
bottle("Potion4", -6.0, Z1 + 0.12, 0.65, 0.24, mat['L3_LiquidBlue'])
for i in range(3):
    rbox(f"BookTop{i}", (0.95, 0.7, 0.18), (-2.8, SY - 0.1, Z1 + 0.21 + i * 0.19), mat[f'L2_Book{(i + 1) % 4}'], furn, 0.05,
         None, (0, 0, R(random.uniform(-8, 8))))
crate = empty("Crate_A", (-4.4, SY - 0.1, Z1 + 0.12), furn)
rbox("Crate_A_Box", (1.3, 0.8, 0.8), (-4.4, SY - 0.1, Z1 + 0.52), mat['L2_ShelfWood'], furn, 0.07, crate)
for k in (-1, 1):
    rbox(f"Crate_A_Slat{k}", (1.34, 0.84, 0.1), (-4.4, SY - 0.1, Z1 + 0.52 + k * 0.22), mat['L2_DarkWood'], furn, 0.04, crate)
rbox("Crate_A_Label", (0.6, 0.03, 0.25), (-4.4, SY - 0.52, Z1 + 0.52), mat['Crate_A_LabelMat'], furn, 0.02, crate, (0, 0, 0), 0)
bottle("Potion5", -0.6, Z1 + 0.12, 0.9, 0.22, mat['L2_Liquid1'])
bottle("Potion6", 0.4, Z1 + 0.12, 0.7, 0.26, mat['L2_Liquid0'])

# pinned notes on the shelf side and under the unit
paper_note("Note_Shelf0", (SX0 - 0.15, SY - 0.56, Z0 + 0.9), 0.55, 0.7, furn, None, 4, mat['L3_RedValve'])
paper_note("Note_Shelf1", (-6.3, WALL_Y - 0.04, Z0 - 0.75), 0.7, 0.85, furn, None, -5, mat['L2_Brass'])
paper_note("Note_Shelf2", (-5.3, WALL_Y - 0.04, Z0 - 0.7), 0.6, 0.7, furn, None, 6, mat['L3_RedValve'])
