"""Step 1: materials, rustic storybook-lab palette (warm plaster, red brick,
old dark wood, brass and copper, glowing potions)."""

# Wood: old, warm and a bit worn
mat_wood('L2_DeskWood', '#C98B4E', '#A0662F', '#6B3F1D', (0.12, 1.4, 1), 0.65, worn=0.65)
mat_wood('L2_ShelfWood', '#B97B45', '#94592C', '#5E3418', (0.2, 2.5, 1), 0.65, worn=0.4)
mat_wood('L2_DarkWood', '#8E5A33', '#6B3F1F', '#432410', (0.25, 2.5, 1), 0.7, worn=0.3)

# Walls
mat_paint('L2_Wallpaper', '#EBD6AE', '#D9BC8B', 0.85, noise_scale=2.5, ao=0.6)   # warm plaster
mat_paint('L2_WallTrim', '#8E5A33', '#6B3F1F', 0.7)
mat_paint('L3_Brick', '#C9643F', '#A84C2E', 0.8, noise_scale=8, ao=0.6)
mat_paint('L3_Mortar', '#D8C29A', '#C7AD82', 0.9)
mat_paint('L2_Stain', '#5E3A1E', '#4A2C15', 0.6)
mat_paint('L3_Crack', '#8C6A45', '#7A5A38', 0.8)

# Window
mat_paint('L2_WindowFrame', '#7A4A26', '#5E3519', 0.6)

# Metals
mat_paint('L2_Metal', '#8E979F', '#6F7880', 0.35, metal=0.9, noise_scale=20, ao=0.4)    # iron
mat_paint('L2_Brass', '#E0B04D', '#B8862E', 0.3, metal=0.95, noise_scale=20, ao=0.4)
mat_paint('L3_Copper', '#D2834F', '#A9623A', 0.3, metal=0.95, noise_scale=20, ao=0.4)
mat_paint('L3_RedValve', '#D23A2E', '#B32D24', 0.35, noise_scale=10)
mat_paint('L3_CauldronIron', '#3A3535', '#2A2626', 0.5, metal=0.6, noise_scale=10)

# Item machine (kept red/yellow, but warmer to sit in the room)
mat_paint('L2_MachineRed', '#D8392E', '#B92C24', 0.35, noise_scale=10, ao=0.5)
mat_paint('L2_MachineYellow', '#F3C24A', '#DCA634', 0.35, noise_scale=10, ao=0.5)
mat_paint('L2_Cream', '#FFF3DA', '#EEDDBB', 0.5)
mat_paint('L2_DarkPlastic', '#3A2E2A', '#2B221F', 0.5)

# Paper, ink, leather
mat_paint('L3_Paper', '#F3E4BE', '#E2CC9C', 0.85, noise_scale=12)
mat_paint('L3_Ink', '#4A3426', '#3A281C', 0.6)
mat_paint('L3_Leather', '#8B3A2A', '#6E2C20', 0.6)
mat_paint('L3_LeatherGreen', '#5F7A3A', '#4C632E', 0.6)

# Glass and glowing liquids
mat_glass('L2_Glass', '#E4F7EC', 0.28)
mat_emit('L2_Bulb', '#FFE48A', 4.0)
mat_emit('L2_SpecimenGlow', '#9FF0FF', 1.5)
mat_emit('L3_LanternGlow', '#FFC45E', 8.0)
for i, col in enumerate(['#72D94A', '#B45BE3', '#F3A23A']):
    mat_emit(f'L2_Liquid{i}', col, 0.6)
mat_emit('L3_LiquidPink', '#F27AAE', 0.6)
mat_emit('L3_LiquidBlue', '#5AAEF0', 0.6)
mat_emit('L3_CauldronBrew', '#86E04A', 1.2)

# Plants and pot
mat_paint('L3_Leaf', '#5DAA3A', '#4A8F2E', 0.5, noise_scale=6)
mat_paint('L3_Terracotta', '#C8683F', '#AE5532', 0.7, noise_scale=6)

# Sky behind the window: gradient, soft clouds and two rows of green hills
m, nodes, links = new_mat('L2_Sky')
b = nodes.get("Principled BSDF")
tc = nodes.new("ShaderNodeTexCoord")
sep = nodes.new("ShaderNodeSeparateXYZ")
links.new(tc.outputs["Generated"], sep.inputs[0])
sky = ramp(nodes, [(0.3, '#BFE8FF'), (1.0, '#4FA8EC')])
links.new(sep.outputs["Z"], sky.inputs["Fac"])
noise = nodes.new("ShaderNodeTexNoise")
noise.inputs["Scale"].default_value = 3
noise.inputs["Detail"].default_value = 3
links.new(tc.outputs["Generated"], noise.inputs["Vector"])
cl = ramp(nodes, [(0.56, '#000000'), (0.66, '#FFFFFF')])
links.new(noise.outputs["Fac"], cl.inputs["Fac"])
mx = nodes.new("ShaderNodeMix")
mx.data_type = 'RGBA'
links.new(cl.outputs["Color"], mx.inputs[0])
links.new(sky.outputs["Color"], mx.inputs[6])
mx.inputs[7].default_value = (1, 1, 1, 1)
color = mx.outputs[2]


def hill_layer(color_in, freq, amp, base, hex_col, phase):
    sx = nodes.new("ShaderNodeMath")
    sx.operation = 'MULTIPLY'
    sx.inputs[1].default_value = freq
    links.new(sep.outputs["X"], sx.inputs[0])
    add = nodes.new("ShaderNodeMath")
    add.operation = 'ADD'
    add.inputs[1].default_value = phase
    links.new(sx.outputs[0], add.inputs[0])
    sn = nodes.new("ShaderNodeMath")
    sn.operation = 'SINE'
    links.new(add.outputs[0], sn.inputs[0])
    h = nodes.new("ShaderNodeMath")
    h.operation = 'MULTIPLY_ADD'
    h.inputs[1].default_value = amp
    h.inputs[2].default_value = base
    links.new(sn.outputs[0], h.inputs[0])
    lt = nodes.new("ShaderNodeMath")
    lt.operation = 'LESS_THAN'
    links.new(sep.outputs["Z"], lt.inputs[0])
    links.new(h.outputs[0], lt.inputs[1])
    mixh = nodes.new("ShaderNodeMix")
    mixh.data_type = 'RGBA'
    links.new(lt.outputs[0], mixh.inputs[0])
    links.new(color_in, mixh.inputs[6])
    mixh.inputs[7].default_value = hexlin(hex_col)
    return mixh.outputs[2]


color = hill_layer(color, 9.0, 0.05, 0.38, '#9ED37A', 0.0)
color = hill_layer(color, 6.0, 0.06, 0.24, '#6DBB4E', 1.7)
links.new(color, b.inputs["Emission Color"])
b.inputs["Emission Strength"].default_value = 1.1
b.inputs["Base Color"].default_value = (0, 0, 0, 1)

# Palm-leaf fan (quat mo): pale areca sheath with long fibres and brown spots
m, nodes, links = new_mat('L3_MoCau')
b = nodes.get("Principled BSDF")
tc = nodes.new("ShaderNodeTexCoord")
fib = nodes.new("ShaderNodeTexWave")
fib.bands_direction = 'X'
fib.inputs["Scale"].default_value = 28
fib.inputs["Distortion"].default_value = 1.5
fib.inputs["Detail"].default_value = 4
links.new(tc.outputs["Object"], fib.inputs["Vector"])
fr = ramp(nodes, [(0.2, '#C99B5B'), (0.6, '#E2C287'), (0.95, '#F0D9A6')])
links.new(fib.outputs["Fac"], fr.inputs["Fac"])
sp = nodes.new("ShaderNodeTexNoise")
sp.inputs["Scale"].default_value = 7
sp.inputs["Detail"].default_value = 6
links.new(tc.outputs["Object"], sp.inputs["Vector"])
sr = ramp(nodes, [(0.58, '#FFFFFF'), (0.7, '#9A6A3A')])
links.new(sp.outputs["Fac"], sr.inputs["Fac"])
ms = nodes.new("ShaderNodeMix")
ms.data_type = 'RGBA'
ms.blend_type = 'MULTIPLY'
ms.inputs[0].default_value = 0.8
links.new(fr.outputs["Color"], ms.inputs[6])
links.new(sr.outputs["Color"], ms.inputs[7])
col = add_surface_detail(nodes, links, tc, ms.outputs[2], b, 0.7, 0.0, 0.2)
ao_mix(nodes, links, col, b, 0.4)
fb = nodes.new("ShaderNodeBump")
fb.inputs["Strength"].default_value = 0.15
links.new(fib.outputs["Fac"], fb.inputs["Height"])
links.new(fb.outputs["Normal"], b.inputs["Normal"])
m.diffuse_color = hexlin('#E2C287')
mat_paint('L3_MoCauRim', '#3A2414', '#2E1C10', 0.6)   # close to the ink colour: the rim is the fan's outline

# Interactable prop colours (bright, so players notice them)
mat_paint('L2_FanBlue', '#4FB3E8', '#3EA0D8', 0.3, noise_scale=12, ao=0.45)
mat_paint('L2_FanBlueDark', '#2F7FBF', '#2A72AD', 0.35, noise_scale=12, ao=0.45)
mat_paint('L2_FanBlade', '#FFFFFF', '#EEF3F7', 0.35)
mat_paint('L2_DryerCoral', '#FF7A8A', '#F06A7C', 0.3, noise_scale=12, ao=0.45)
mat_paint('L2_DryerCoralDark', '#D95266', '#C94A5C', 0.35, noise_scale=12, ao=0.45)
mat_paint('L2_CapsuleBlue', '#4FB3E8', '#3E9FD6', 0.25)
for i, col in enumerate(['#3F6FB5', '#C7922E', '#A8382E', '#4F8A52']):
    mat_paint(f'L2_Book{i}', col, col, 0.65)
for name, col in (('Crate_A', '#F3E4BE'), ('Crate_B', '#F3E4BE'), ('Crate_C', '#F3E4BE')):
    mat_paint(name + '_LabelMat', col, col, 0.7)
mat_glass('REF_Pet', '#FFFFFF', 0.2)
outline_material()
