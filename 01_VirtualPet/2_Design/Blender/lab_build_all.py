"""Builds the whole Wind Pet lab from the step scripts and saves Lab.blend
after every step, so a crash never loses work.
In Blender: Scripting tab > open this file > Run Script.
"""
import os
import bpy

BASE = r"C:\Users\ADMIN\OneDrive\Desktop\GameSystem2\01_VirtualPet\2_Design\Blender"
BLEND = os.path.join(BASE, "Lab.blend")
STEPS = ["lab_helpers.py", "lab_1_materials.py", "lab_2_desk_wall_shelves.py",
         "lab_3_window_machine_props.py", "lab_5_decor.py", "lab_8_work_props.py", "lab_4_camera_light.py",
         "lab_6_outline.py", "lab_7_checks.py"]

# Remove the old v1 lab (superseded) so object names never clash.
for cname in ("Lab_Room", "Lab_Furniture", "Lab_Interactables", "Lab_Reference", "Lab"):
    c = bpy.data.collections.get(cname)
    if c:
        for o in list(c.all_objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(c)
for n in ("LabSun", "LabPreviewCam"):
    o = bpy.data.objects.get(n)
    if o:
        bpy.data.objects.remove(o, do_unlink=True)
# Clear the v2 collections first, then purge orphan data, so names stay clean.
for c in [c for c in bpy.data.collections if c.name.startswith("LabV2")]:
    for o in list(c.all_objects):
        bpy.data.objects.remove(o, do_unlink=True)
try:
    bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
except Exception as e:
    print("purge skipped:", e)

ns = {"__name__": "lab_build"}
for step in STEPS:
    with open(os.path.join(BASE, step), encoding="utf-8") as f:
        exec(compile(f.read(), step, "exec"), ns)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("done:", step)
