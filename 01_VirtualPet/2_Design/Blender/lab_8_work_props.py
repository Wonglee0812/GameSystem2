"""Step 8: the work desk props for the electricity job.
- Turbine: a glass bell jar on a brass base. The pet is put inside and spins a
  brass rotor; the counter window in the base shows the stored electricity.
  The rotor is its own top-level group, so Unity can spin it.
- Order printer: an old teleprinter that prints purchase orders (bills).
- Stamp and ink pad: the player's tools for approving a bill.
Same materials, bevels and outline rules as the rest of the lab."""
mat = bpy.data.materials
root = coll("LabV2")
work = coll("LabV2_Work", root)
clear_collection(work)
DESK = -1.0

brass, copper, iron = mat['L2_Brass'], mat['L3_Copper'], mat['L2_Metal']
dark, cream, yellow = mat['L2_DarkPlastic'], mat['L2_Cream'], mat['L2_MachineYellow']

# ---------- Turbine (bell jar) ----------
TX, TY = -0.4, 3.0
BASE_H = 0.45
TOP = DESK + BASE_H
tb = empty("Turbine", (TX, TY, DESK), work)
cyl("Turbine_Base", 1.25, BASE_H, (TX, TY, DESK + BASE_H / 2), brass, work, (0, 0, 0), 0.08, tb, 64)
tor("Turbine_BaseRing", 1.08, 0.06, (TX, TY, TOP), copper, work, (0, 0, 0), tb)
for i in range(4):
    a = R(45 + 90 * i)
    cyl(f"Turbine_Foot{i}", 0.14, 0.12, (TX + 1.05 * math.cos(a), TY + 1.05 * math.sin(a), DESK + 0.04), dark, work, (0, 0, 0),
        0.03, tb, 24)
# counter window on the front of the base (Unity writes the number into it)
rbox("Turbine_CounterPanel", (1.0, 0.16, 0.3), (TX, TY - 1.2, DESK + 0.24), dark, work, 0.05, tb)
rbox("Turbine_CounterFrame", (1.12, 0.12, 0.38), (TX, TY - 1.16, DESK + 0.24), copper, work, 0.05, tb)
for k in (-1, 1):
    screw(f"Turbine_CounterScrew{k}", (TX + k * 0.62, TY - 1.24, DESK + 0.24), work, tb, iron)
# the bell jar: a tall glass dome, a brass rim and a copper cap with a bulb
dome = sph("Turbine_Glass", 1.0, (TX, TY, TOP + 0.2), mat['L2_Glass'], work, (1, 1, 1.35), tb)
tor("Turbine_Rim", 1.02, 0.07, (TX, TY, TOP + 0.04), brass, work, (0, 0, 0), tb)
cap_z = TOP + 0.2 + 1.35 - 0.05
cyl("Turbine_Cap", 0.26, 0.2, (TX, TY, cap_z + 0.08), copper, work, (0, 0, 0), 0.05, tb, 32)
cyl("Turbine_CapCoil", 0.2, 0.14, (TX, TY, cap_z + 0.24), brass, work, (0, 0, 0), 0.03, tb, 32)
sph("Turbine_Bulb", 0.16, (TX, TY, cap_z + 0.44), mat['L2_Bulb'], work, (1, 1, 1.15), tb)
cyl("Turbine_Axle", 0.05, 1.45, (TX, TY, TOP + 0.75), iron, work, (0, 0, 0), 0.02, tb, 16)

# ---------- Turbine rotor (separate, spins in Unity) ----------
RZ = TOP + 1.35
rot = empty("TurbineRotor", (TX, TY, RZ), work)
sph("Rotor_Hub", 0.14, (TX, TY, RZ), copper, work, (1, 1, 1), rot)
for i in range(3):
    a = R(120 * i)
    bx, by = TX + 0.42 * math.cos(a), TY + 0.42 * math.sin(a)
    rbox(f"Rotor_Blade{i}", (0.62, 0.22, 0.04), (bx, by, RZ), brass, work, 0.02, rot, (R(18), 0, a))

# ---------- Order printer (teleprinter) ----------
PX, PY = -3.7, -0.6   # front-left of the desk, between the books and the fan, fully on screen
pr = empty("OrderPrinter", (PX, PY, DESK), work)
rbox("Printer_Body", (1.0, 0.8, 0.62), (PX, PY, DESK + 0.33), yellow, work, 0.14, pr)
rbox("Printer_Slot", (0.62, 0.14, 0.06), (PX, PY + 0.1, DESK + 0.66), dark, work, 0.02, pr)
rbox("Printer_Window", (0.72, 0.06, 0.22), (PX, PY - 0.4, DESK + 0.42), cream, work, 0.04, pr)
text_mesh("Printer_Text", "NEW ORDER", (PX, PY - 0.44, DESK + 0.41), 0.1, 0.01, mat['L3_Ink'], work, pr)
sph("Printer_Button", 0.08, (PX - 0.3, PY - 0.42, DESK + 0.16), mat['L2_MachineRed'], work, (1, 0.6, 1), pr)
sph("Printer_Lamp", 0.06, (PX - 0.1, PY - 0.42, DESK + 0.16), mat['L2_Bulb'], work, (1, 0.6, 1), pr)
for k in range(3):
    rbox(f"Printer_Trim{k}", (0.32 - k * 0.05, 0.03, 0.03), (PX + 0.22, PY - 0.41, DESK + 0.1 + 0.07 * k), brass, work, 0.01, pr)
# a page curling out of the top, so the printer reads as "printing"
rbox("Printer_Paper", (0.5, 0.02, 0.34), (PX, PY + 0.06, DESK + 0.82), mat['L3_Paper'], work, 0.01, pr, (R(-14), 0, 0))

# ---------- Stamp and ink pad (holdable tools) ----------
SX, SY = 0.0, -1.4
st = empty("Stamp", (SX, SY, DESK), work)
rbox("Stamp_Rubber", (0.3, 0.2, 0.04), (SX, SY, DESK + 0.02), mat['L3_RedValve'], work, 0.01, st)
rbox("Stamp_Block", (0.32, 0.22, 0.1), (SX, SY, DESK + 0.09), mat['L2_DarkWood'], work, 0.03, st)
cyl("Stamp_Ferrule", 0.09, 0.06, (SX, SY, DESK + 0.17), brass, work, (0, 0, 0), 0.02, st, 24)
cyl("Stamp_Neck", 0.05, 0.22, (SX, SY, DESK + 0.3), mat['L2_DarkWood'], work, (0, 0, 0), 0.02, st, 16)
sph("Stamp_Knob", 0.13, (SX, SY, DESK + 0.48), mat['L2_DeskWood'], work, (1, 1, 0.9), st)

IX, IY = 0.7, -1.5
ip = empty("InkPad", (IX, IY, DESK), work)
rbox("InkPad_Tin", (0.46, 0.32, 0.08), (IX, IY, DESK + 0.04), iron, work, 0.03, ip)
rbox("InkPad_Ink", (0.38, 0.24, 0.02), (IX, IY, DESK + 0.085), mat['L3_RedValve'], work, 0.005, ip)
rbox("InkPad_Lid", (0.46, 0.02, 0.32), (IX, IY + 0.2, DESK + 0.17), iron, work, 0.01, ip, (R(-20), 0, 0))
print("work props built")
