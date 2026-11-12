"""Step 3: arched sealed window, pachinko item machine, hand fan and hair dryer."""
mat = bpy.data.materials
root = coll("LabV2")
win_c = coll("LabV2_Window", root)
inter = coll("LabV2_Interactables", root)
clear_collection(win_c)
clear_collection(inter)
DESK = -1.0
WALL_Y = 5.6

# ---------- Arched window with many small panes (right) ----------
WX = 5.4
RAD = 1.5                     # half width = arch radius
ZB, ZS = DESK + 0.75, DESK + 2.3   # bottom of the window, start of the arch
FY = WALL_Y - 0.3
win = empty("Window", (WX, FY, ZS), win_c)
arch_panel("Window_Sky", (WX, 0, ZS), RAD * 2, ZS - ZB, WALL_Y - 0.03, mat['L2_Sky'], win_c, win)
arch_panel("Window_Glass", (WX, 0, ZS), RAD * 2, ZS - ZB, WALL_Y - 0.22, mat['L2_Glass'], win_c, win)
arch_ring("Window_Arch", (WX, FY, ZS), RAD + 0.3, RAD - 0.02, 0.5, mat['L2_WindowFrame'], win_c, win)
for k in (-1, 1):
    rbox(f"Window_Jamb{k}", (0.32, 0.5, ZS - ZB + 0.05), (WX + k * (RAD + 0.14), FY, (ZB + ZS) / 2), mat['L2_WindowFrame'], win_c, 0.1, win)
rbox("Window_Sill", (RAD * 2 + 1.0, 1.0, 0.3), (WX, FY - 0.25, ZB - 0.1), mat['L2_WindowFrame'], win_c, 0.12, win)
rbox("Window_SillFoot", (RAD * 2 + 0.6, 0.6, 0.25), (WX, FY - 0.1, ZB - 0.35), mat['L2_DarkWood'], win_c, 0.1, win)
# mullions: 3 vertical bars, 3 horizontal bars in the square part, 1 in the arch
for i, x in enumerate((-RAD / 2, 0.0, RAD / 2)):
    top = ZS + math.sqrt(max(RAD * RAD - x * x, 0.0))
    rbox(f"Window_BarV{i}", (0.12 if x else 0.2, 0.28, top - ZB), (WX + x, FY - 0.05, (ZB + top) / 2), mat['L2_WindowFrame'], win_c,
         0.05, win)
for i, z in enumerate((ZB + 0.52, ZB + 1.04, ZS)):
    rbox(f"Window_BarH{i}", (RAD * 2, 0.28, 0.12), (WX, FY - 0.05, z), mat['L2_WindowFrame'], win_c, 0.05, win)
zA = ZS + 0.75
rbox("Window_BarArch", (2 * math.sqrt(RAD * RAD - 0.75 ** 2), 0.28, 0.12), (WX, FY - 0.05, zA), mat['L2_WindowFrame'], win_c, 0.05, win)
# sealed: brass latch handles and a padlock in the middle
for k in (-1, 1):
    rbox(f"Window_Handle{k}", (0.07, 0.12, 0.42), (WX + k * 0.16, FY - 0.3, ZB + 0.95), mat['L2_Brass'], win_c, 0.03, win)
lock = empty("Window_Padlock", (WX, FY - 0.35, ZB + 0.62), win_c, win)
rbox("Padlock_Body", (0.36, 0.16, 0.32), (WX, FY - 0.37, ZB + 0.6), mat['L2_Brass'], win_c, 0.09, lock)
tor("Padlock_Shackle", 0.13, 0.035, (WX, FY - 0.37, ZB + 0.8), mat['L2_Metal'], win_c, (R(90), 0, 0), lock)

# ---------- Pachinko item machine on the desk (covers the lower window) ----------
red, yel, cream = mat['L2_MachineRed'], mat['L2_MachineYellow'], mat['L2_Cream']
chrome, dark = mat['L2_Metal'], mat['L2_DarkPlastic']
PX, PY, PZ = WX, WALL_Y - 1.35, DESK
H = 2.3
FRONT = PY - 0.72
pa = empty("PachinkoMachine", (PX, PY, PZ), inter)
rbox("Pachinko_Body", (2.6, 1.4, H), (PX, PY, PZ + H / 2 + 0.2), red, inter, 0.32, pa)
rbox("Pachinko_Feet", (2.75, 1.55, 0.25), (PX, PY, PZ + 0.12), dark, inter, 0.1, pa)
dome = cyl("Pachinko_Dome", 1.3, 1.45, (PX, PY, PZ + H + 0.2), yel, inter, (R(90), 0, 0), 0.07, pa, 96, 0)
dome.scale = (1, 0.55, 1)
rbox("Pachinko_DomeTrim", (2.75, 1.5, 0.18), (PX, PY, PZ + H + 0.22), mat['L2_Brass'], inter, 0.08, pa)
for i in range(9):
    a = R(10 + 160 * i / 8)
    sph(f"Pachinko_Bulb{i}", 0.09, (PX + 1.12 * math.cos(a), FRONT - 0.04, PZ + H + 0.25 + 0.6 * math.sin(a)),
        mat['L2_Bulb'], inter, (1, 1, 1), pa)
cyl("Pachinko_Emblem", 0.28, 0.08, (PX, FRONT - 0.05, PZ + H + 0.55), mat['L2_Brass'], inter, (R(90), 0, 0), 0.03, pa)
tor("Pachinko_EmblemSwirl", 0.14, 0.03, (PX, FRONT - 0.1, PZ + H + 0.55), dark, inter, (R(90), 0, 0), pa)
cz = PZ + 1.55
cyl("Pachinko_Board", 0.88, 0.06, (PX, FRONT + 0.05, cz), cream, inter, (R(90), 0, 0), 0.02, pa, 64)
for row in range(7):
    for col in range(8):
        ox = (col - 3.5) * 0.2 + (0.1 if row % 2 else 0)
        oz = 0.6 - row * 0.2
        if ox * ox + oz * oz > 0.72 ** 2:
            continue
        cyl(f"Pachinko_Pin{row}_{col}", 0.018, 0.12, (PX + ox, FRONT - 0.02, cz + oz), mat['L2_Brass'], inter, (R(90), 0, 0), 0.006,
            pa, 10, 0)
for i, (bx, bz, bc) in enumerate([(-0.3, 0.4, chrome), (0.35, 0.05, chrome), (-0.05, -0.45, yel)]):
    sph(f"Pachinko_Ball{i}", 0.07, (PX + bx, FRONT - 0.06, cz + bz), bc, inter, (1, 1, 1), pa)
tor("Pachinko_Ring", 0.92, 0.09, (PX, FRONT - 0.06, cz), mat['L2_Brass'], inter, (R(90), 0, 0), pa)
cyl("Pachinko_Glass", 0.88, 0.03, (PX, FRONT - 0.12, cz), mat['L2_Glass'], inter, (R(90), 0, 0), 0.01, pa, 64, 0)
for i in range(4):
    a = R(45 + 90 * i)
    screw(f"Pachinko_RingScrew{i}", (PX + 1.0 * math.cos(a), FRONT - 0.08, cz + 1.0 * math.sin(a)), inter, pa, mat['L3_Copper'])
# ORDERS IN slot on the right (the potted plant hides the left side): stamped bills go in here,
# coins come out in the tray on the left
rbox("Pachinko_OrderPlate", (0.8, 0.14, 0.46), (PX + 0.5, FRONT - 0.04, PZ + 0.5), mat['L2_Brass'], inter, 0.07, pa)
rbox("Pachinko_OrderSlot", (0.58, 0.1, 0.06), (PX + 0.5, FRONT - 0.12, PZ + 0.58), dark, inter, 0.02, pa)
text_mesh("Pachinko_OrderText", "ORDERS IN", (PX + 0.5, FRONT - 0.12, PZ + 0.4), 0.085, 0.008, dark, inter, pa)
rbox("Pachinko_Tray", (1.0, 0.6, 0.36), (PX - 0.7, FRONT - 0.2, PZ + 0.45), yel, inter, 0.14, pa)
sph("Pachinko_CapsuleTop", 0.15, (PX - 0.7, FRONT - 0.25, PZ + 0.7), mat['L2_CapsuleBlue'], inter, (1, 1, 1), pa)
sph("Pachinko_CapsuleBottom", 0.15, (PX - 0.7, FRONT - 0.25, PZ + 0.66), cream, inter, (1, 1, 0.8), pa)
crank = empty("Pachinko_Crank", (PX + 1.38, PY - 0.2, PZ + 1.5), inter, pa)
cyl("Crank_Axle", 0.17, 0.3, (PX + 1.42, PY - 0.2, PZ + 1.5), mat['L2_Brass'], inter, (0, R(90), 0), 0.04, crank)
rbox("Crank_Arm", (0.14, 0.14, 0.75), (PX + 1.6, PY - 0.2, PZ + 1.8), yel, inter, 0.06, crank)
sph("Crank_Knob", 0.17, (PX + 1.6, PY - 0.2, PZ + 2.2), red, inter, (1, 1, 1), crank)

# ---------- Palm-leaf hand fan (quat mo) standing in a small wooden holder ----------
FX, FY = -3.4, 0.1
fan = empty("Prop_QuatMo", (FX, FY, DESK), inter)
rbox("QuatMo_Holder", (0.6, 0.35, 0.22), (FX, FY, DESK + 0.11), mat['L2_DarkWood'], inter, 0.06, fan)
rbox("QuatMo_HolderSlot", (0.3, 0.08, 0.04), (FX, FY, DESK + 0.22), mat['L2_DarkPlastic'], inter, 0.015, fan, (0, 0, 0), 0)
# outline: a broad, slightly squarish round blade on a flat tapering handle
pts = []
cz, a, bb, n = 1.12, 0.52, 0.56, 2.6
for i in range(64):
    t = math.radians(-78 + (336 * i / 63))
    c, sn = math.cos(t), math.sin(t)
    x = a * math.copysign(abs(c) ** (2 / n), c)
    y = cz + bb * math.copysign(abs(sn) ** (2 / n), sn)
    pts.append((x, y))
pts += [(-0.09, cz - bb * 0.93), (-0.075, 0.12), (-0.06, 0.0), (0.06, 0.0), (0.075, 0.12), (0.09, cz - bb * 0.93)]
grip = empty("QuatMo_Grip", (FX, FY, DESK + 0.15), inter, fan)   # pick-up point for the player
plate, rim_o = flat_shape("MoCau_Blade", pts, 0.012, mat['L3_MoCau'], inter, None, mat['L3_MoCauRim'], 0.024)   # the dark rim doubles as the ink outline
for ob in (plate, rim_o):
    ob.rotation_euler = (R(90), 0, R(-12))
    ob.location = (FX, FY - 0.02, DESK + 0.15)
    bend = ob.modifiers.new("Curve", 'SIMPLE_DEFORM')
    bend.deform_method = 'BEND'
    bend.deform_axis = 'Y'
    bend.angle = R(-28)
    ob.parent = grip
    ob.matrix_parent_inverse = grip.matrix_world.inverted()

# ---------- Hair dryer (right, nozzle points left at the pet) ----------
coral, coralD = mat['L2_DryerCoral'], mat['L2_DryerCoralDark']
dry = empty("Prop_HairDryer", (3.0, 0.0, DESK), inter)
rx, ry, rz = dry.location
cyl("Dryer_Body", 0.24, 0.7, (rx, ry, rz + 0.78), coral, inter, (0, R(90), 0), 0.1, dry)
sph("Dryer_Back", 0.24, (rx + 0.35, ry, rz + 0.78), coral, inter, (0.55, 1, 1), dry)
for i in range(3):
    tor(f"Dryer_Vent{i}", 0.06 + i * 0.05, 0.018, (rx + 0.47, ry, rz + 0.78), coralD, inter, (0, R(90), 0), dry)
cyl("Dryer_Nozzle", 0.16, 0.32, (rx - 0.5, ry, rz + 0.78), coralD, inter, (0, R(90), 0), 0.05, dry)
rbox("Dryer_Handle", (0.22, 0.26, 0.7), (rx + 0.08, ry, rz + 0.35), coral, inter, 0.1, dry, (0, R(12), 0))
rbox("Dryer_Switch", (0.09, 0.08, 0.16), (rx + 0.05, ry - 0.14, rz + 0.45), mat['L2_MachineYellow'], inter, 0.03, dry, (0, R(12), 0))
