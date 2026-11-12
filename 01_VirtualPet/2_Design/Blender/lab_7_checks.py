"""Step 7: automatic overlap checks, so things never sit inside each other.
A) wall papers / signs must not be covered by bricks or furniture
B) bricks must not cut into shelves, window, pipes or other decor
C) props on the desk must not intersect each other
Results go to build_log.txt next to this script."""
import os
from mathutils import Vector

dg = bpy.context.evaluated_depsgraph_get()


def aabb(o):
    ev = o.evaluated_get(dg)
    pts = [ev.matrix_world @ Vector(c) for c in ev.bound_box]
    return (min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts),
            max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))


def hit(a, b, pad=0.0):
    return all(a[i] < b[i + 3] - pad and b[i] < a[i + 3] - pad for i in range(3))


def union(boxes):
    return tuple(min(b[i] for b in boxes) for i in range(3)) + tuple(max(b[i] for b in boxes) for i in range(3, 6))


def meshes(c):
    return [o for o in c.all_objects if o.type == 'MESH']


def top_group(o):
    while o.parent is not None:
        o = o.parent
    return o


cols = {n: bpy.data.collections.get(n) for n in ("LabV2_Room", "LabV2_Shelves", "LabV2_Window", "LabV2_Interactables", "LabV2_Decor")}
all_mesh = [o for c in cols.values() if c for o in meshes(c)]
bricks = [o for o in all_mesh if o.name.startswith("Brick_")]
decals = [o for o in all_mesh if o.name.startswith(("Note_", "Sketch_", "Sign_"))]
solids = [o for o in all_mesh if not o.name.startswith(("Brick_", "Wall_", "Desk_", "Note_", "Sketch_", "Sign_"))]
issues = []

for d in decals:
    bd = aabb(d)
    for b in bricks:
        if hit(bd, aabb(b)):
            issues.append(f"A: {d.name} is covered by {b.name}")
for b in bricks:
    bb = aabb(b)
    for s in solids:
        if hit(bb, aabb(s), 0.005):
            issues.append(f"B: {b.name} cuts into {s.name}")

groups = {}
for c in (cols["LabV2_Decor"], cols["LabV2_Interactables"]):
    for o in meshes(c):
        g = top_group(o)
        if g.name in ("Lantern", "Pipes", "LabSign", "WindSketch", "Window"):
            continue
        groups.setdefault(g.name, []).append((o.name, aabb(o)))
names = sorted(groups)
for i, a in enumerate(names):
    for b in names[i + 1:]:
        for na, ba in groups[a]:
            found = next((nb for nb, bb in groups[b] if hit(ba, bb, 0.005)), None)
            if found:
                issues.append(f"C: {a}/{na} touches {b}/{found}")
                break

log = os.path.join(os.path.dirname(bpy.data.filepath), "check_log.txt")
with open(log, "w", encoding="utf-8") as f:
    f.write(f"{len(issues)} issue(s)\n" + "\n".join(issues))
print(len(issues), "issues")
