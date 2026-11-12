"""Step 6: ink outlines on every visible object (inverted hull), like the
hand-drawn reference. Thickness grows with object size; tiny parts get none.
The palm-leaf fan (MoCau) is skipped: it is a thin bent plate, where a hull breaks,
so its dark rim is the outline instead."""
targets = ["LabV2_Room", "LabV2_Shelves", "LabV2_Window", "LabV2_Interactables", "LabV2_Decor", "LabV2_Work"]
skip_words = ("Sky", "Glass", "Scratch", "Crack", "Spill", "CoffeeRing", "Text", "Line", "Liquid", "Brew", "Glow", "Wall_Back", "Desk_Plank", "MoCau")
count = 0
for cname in targets:
    c = bpy.data.collections.get(cname)
    if c is None:
        continue
    for o in c.all_objects:
        if o.type != 'MESH' or any(w in o.name for w in skip_words):
            continue
        size = max(o.dimensions)
        if size < 0.12:
            continue
        add_outline(o, min(0.035, max(0.012, size * 0.012)))
        count += 1
print("outlined", count)
