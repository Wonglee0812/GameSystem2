"""Brick Panic UI art (2026-11-24). Safe to run twice.
- copies the Poker TD 9-slice sprites (Panel9, Pill9, Button9: white with a dark outline) so both games share one look;
- Glow9: soft yellow halo for the selected card; Fill9: plain white rounded bar (no outline) for bar fills;
- Kenney board-game / game icons (white, tinted in Unity);
- three disaster icons drawn here (quake crack, tsunami wave, volcano) in the same white silhouette style.
Run on the device: python3 make_bp_ui_art.py <GameSystem2 root> <AssetPack root>"""
import os, sys, shutil, math
from PIL import Image, ImageDraw, ImageFilter

root, pack = sys.argv[1], sys.argv[2]
out = os.path.join(root, 'Assets/BrickPanic/Art/UI')
icons = os.path.join(out, 'Icons')
os.makedirs(icons, exist_ok=True)
ptd = os.path.join(root, 'Assets/Progression/Art/Resources/UI')
for n in ['Panel9', 'Pill9', 'Button9']:
    shutil.copyfile(os.path.join(ptd, n + '.png'), os.path.join(out, n + '.png'))

S = 4
def rounded(size, rad, fill, blur=0):
    w, h = size
    im = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, w * S - 1, h * S - 1], rad * S, fill=fill)
    im = im.resize((w, h), Image.LANCZOS)
    return im

# glow: a blurred rounded rect with a hole look comes from 9-slicing; keep the centre transparent-ish
g = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
d = ImageDraw.Draw(g)
d.rounded_rectangle([20, 20, 107, 107], 22, outline=(255, 255, 255, 255), width=10)
g = g.filter(ImageFilter.GaussianBlur(7))
g.save(os.path.join(out, 'Glow9.png'))
rounded((64, 64), 16, (255, 255, 255, 255)).save(os.path.join(out, 'Fill9.png'))

kb = os.path.join(pack, 'UI/kenney_board-game-icons/PNG/Double (128px)')
kg = os.path.join(pack, 'UI/kenney_game-icons/PNG/White/2x')
for src, name in [(kb, 'dollar'), (kb, 'character'), (kb, 'flag_triangle'), (kb, 'skull'), (kb, 'pawns'),
                  (kb, 'hourglass'), (kb, 'arrow_rotate'), (kb, 'structure_wall'), (kb, 'token'),
                  (kg, 'wrench'), (kg, 'warning'), (kg, 'star'), (kg, 'trophy'), (kg, 'return')]:
    p = os.path.join(src, name + '.png')
    im = Image.open(p).convert('RGBA')
    # make every icon white (the board-game icons are dark), keep alpha
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            a = px[x, y][3]
            px[x, y] = (255, 255, 255, a)
    im = im.crop(im.getbbox())
    c = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    k = 112 / max(im.size)
    im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
    c.paste(im, ((128 - im.width) // 2, (128 - im.height) // 2), im)
    c.save(os.path.join(icons, name + '.png'))

def canvas():
    im = Image.new('RGBA', (128 * S, 128 * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)
def save(im, name):
    im.resize((128, 128), Image.LANCZOS).save(os.path.join(icons, name + '.png'))
W = (255, 255, 255, 255)
T = (0, 0, 0, 0)

# tsunami: a curl that opens to the lower right, running into the sea
im, d = canvas()
k = S / 4
def box(x0, y0, x1, y1): return [x0 * k, y0 * k, x1 * k, y1 * k]
d.ellipse(box(70, 40, 450, 420), fill=W)
d.ellipse(box(190, 150, 390, 350), fill=T)
d.polygon([(290 * k, 250 * k), (512 * k, 250 * k), (512 * k, 512 * k), (290 * k, 512 * k)], fill=T)
d.ellipse(box(300, 215, 370, 285), fill=W)
d.polygon([(10 * k, 470 * k), (70 * k, 250 * k), (150 * k, 400 * k), (260 * k, 470 * k)], fill=W)
d.rounded_rectangle(box(10, 410, 300, 480), 30 * k, fill=W)
save(im, 'disaster_tsunami')

# quake: ground blocks split by a zig-zag crack
im, d = canvas()
d.rounded_rectangle([30, 250, 482, 480], 30, fill=W)
crack = [(250, 230), (215, 300), (275, 340), (225, 410), (265, 490), (300, 490), (262, 410), (312, 340), (252, 300), (290, 230)]
d.polygon(crack, fill=T)
for x, y, s in [(90, 150, 70), (330, 120, 90), (210, 60, 50)]:
    d.rounded_rectangle([x, y, x + s, y + s * 0.6], 10, fill=W)
d.line([(40, 210), (120, 180)], fill=W, width=18)
d.line([(470, 210), (390, 175)], fill=W, width=18)
save(im, 'disaster_quake')

# volcano: cone with a crater and a lava blob
im, d = canvas()
d.polygon([(20, 480), (190, 190), (322, 190), (492, 480)], fill=W)
d.polygon([(205, 190), (230, 240), (282, 240), (307, 190)], fill=T)
for x, y, r in [(256, 110, 46), (170, 60, 30), (350, 70, 34)]:
    d.ellipse([x - r, y - r, x + r, y + r], fill=W)
save(im, 'disaster_volcano')
print('ok', sorted(os.listdir(icons)))

# ---------- emote bubbles over people (drawn here: the Kenney emotes are only 32 px) ----------
emo = os.path.join(out, 'Emotes')
os.makedirs(emo, exist_ok=True)
INK = (43, 33, 51, 255)
def bubble():
    im = Image.new('RGBA', (128 * S, 144 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    o = 6 * S
    d.polygon([(48 * S, 110 * S), (64 * S, 138 * S), (80 * S, 110 * S)], fill=INK)
    d.rounded_rectangle([4 * S, 4 * S, 124 * S, 116 * S], 40 * S, fill=INK)
    d.rounded_rectangle([4 * S + o, 4 * S + o, 124 * S - o, 116 * S - o], 34 * S, fill=(255, 255, 255, 255))
    d.polygon([(54 * S, 106 * S), (64 * S, 126 * S), (74 * S, 106 * S)], fill=(255, 255, 255, 255))
    return im, d
def emsave(im, name):
    im.resize((128, 144), Image.LANCZOS).save(os.path.join(emo, name + '.png'))
def paste_icon(im, name, color, size):
    ic = Image.open(os.path.join(icons, name + '.png')).convert('RGBA').resize((size * S, size * S), Image.LANCZOS)
    px = ic.load()
    for y in range(ic.height):
        for x in range(ic.width):
            a = px[x, y][3]
            px[x, y] = color[:3] + (a,)
    im.paste(ic, ((128 * S - ic.width) // 2, (120 * S - ic.height) // 2), ic)

RED = (240, 60, 60, 255)
im, d = bubble()                                   # panic: red "!"
d.rounded_rectangle([54 * S, 22 * S, 74 * S, 72 * S], 10 * S, fill=RED)
d.ellipse([53 * S, 80 * S, 75 * S, 102 * S], fill=RED)
emsave(im, 'panic')
im, d = bubble()                                   # happy: heart
def heart(d, cx, cy, r, col):
    d.ellipse([cx - 2 * r, cy - r, cx, cy + r], fill=col)
    d.ellipse([cx, cy - r, cx + 2 * r, cy + r], fill=col)
    d.polygon([(cx - 2 * r + 2 * S, cy + r * 0.35), (cx, cy + 2.4 * r), (cx + 2 * r - 2 * S, cy + r * 0.35)], fill=col)
heart(d, 64 * S, 50 * S, 20 * S, (255, 80, 120, 255))
emsave(im, 'happy')
im, d = bubble()                                   # dazed: stars
paste_icon(im, 'star', (255, 190, 30, 255), 70)
emsave(im, 'dazed')
im, d = bubble()                                   # dead: skull
paste_icon(im, 'skull', (90, 85, 100, 255), 66)
emsave(im, 'dead')
im, d = bubble()                                   # scared: sweat drop
BL = (70, 160, 255, 255)
d.ellipse([42 * S, 46 * S, 86 * S, 90 * S], fill=BL)
d.polygon([(45 * S, 60 * S), (64 * S, 16 * S), (83 * S, 60 * S)], fill=BL)
emsave(im, 'scared')
print('emotes', sorted(os.listdir(emo)))

# ---------- crack texture for worn bricks: alpha = when a crack part shows (high alpha first, so cracks grow) ----------
import random
random.seed(7)
N = 512
crack = Image.new('RGBA', (N, N), (40, 30, 30, 0))
cd = ImageDraw.Draw(crack)
def grow(x, y, ang, length, depth, start):
    steps = int(length / 10)
    for k in range(steps):
        t = start + (k / max(1, steps)) * (1 - start) * 0.9
        a = int(255 * (1 - t * 0.85))
        nx = x + math.cos(ang) * 10
        ny = y + math.sin(ang) * 10
        cd.line([(x, y), (nx, ny)], fill=(40, 30, 30, a), width=max(2, 7 - depth * 2))
        x, y = nx, ny
        ang += random.uniform(-0.5, 0.5)
        if depth < 2 and random.random() < 0.08:
            grow(x, y, ang + random.choice([-1, 1]) * random.uniform(0.6, 1.2), length * 0.45, depth + 1, t)
for cx, cy in [(130, 140), (380, 330), (260, 420), (420, 90)]:
    for _ in range(3):
        grow(cx, cy, random.uniform(0, math.pi * 2), random.uniform(140, 260), 0, 0.0)
crack.save(os.path.join(out, 'Crack.png'))
print('crack ok')

# ---------- tool icons (hammer, eraser) and the tsunami forecast arrow ----------
im, d = canvas()
k = S / 4
d.rounded_rectangle([110 * k, 120 * k, 400 * k, 250 * k], 30 * k, fill=W)                    # hammer head
d.polygon([(110 * k, 150 * k), (40 * k, 120 * k), (40 * k, 250 * k), (110 * k, 220 * k)], fill=W)   # claw side
d.rounded_rectangle([215 * k, 230 * k, 295 * k, 480 * k], 28 * k, fill=W)                    # handle
im = im.rotate(-30, resample=Image.BICUBIC, center=(256 * k, 300 * k))
save(im, 'tool_build')
im, d = canvas()
body = Image.new('RGBA', (128 * S, 128 * S), (0, 0, 0, 0))
bd = ImageDraw.Draw(body)
bd.rounded_rectangle([80 * k, 170 * k, 432 * k, 342 * k], 40 * k, fill=W)
bd.rectangle([256 * k, 186 * k, 270 * k, 326 * k], fill=T)                                    # band line
body = body.rotate(35, resample=Image.BICUBIC)
im.paste(body, (0, 0), body)
d = ImageDraw.Draw(im)
d.rounded_rectangle([60 * k, 430 * k, 452 * k, 456 * k], 13 * k, fill=W)                      # rubbed line
save(im, 'tool_erase')

A = 512
arrow = Image.new('RGBA', (A, A), (255, 255, 255, 0))
ad = ImageDraw.Draw(arrow)
for i, a in enumerate([90, 160, 230]):                                                        # three chevrons, stronger at the tip
    y = 330 - i * 120
    ad.polygon([(256, y - 150), (470, y + 20), (390, y + 70), (256, y - 40), (122, y + 70), (42, y + 20)], fill=(255, 255, 255, a))
arrow = arrow.filter(ImageFilter.GaussianBlur(3))
arrow.save(os.path.join(out, 'TsunamiArrow.png'))
print('tools + arrow ok')

# ---------- new disaster icons (2026-11-25): tornado, lightning ----------
im, d = canvas()
k = S / 4
for i, (y, w) in enumerate([(70, 210), (150, 165), (225, 125), (295, 90), (360, 60), (420, 34)]):
    x = 256 + (i % 2) * 18 - 9
    d.rounded_rectangle([(x - w) * k, (y - 26) * k, (x + w) * k, (y + 26) * k], 26 * k, fill=W)
d.rounded_rectangle([210 * k, 450 * k, 262 * k, 490 * k], 18 * k, fill=W)
save(im, 'disaster_tornado')
im, d = canvas()
d.polygon([(300 * k, 20 * k), (120 * k, 290 * k), (240 * k, 290 * k), (190 * k, 492 * k), (400 * k, 200 * k), (275 * k, 200 * k), (350 * k, 20 * k)], fill=W)
save(im, 'disaster_lightning')
print('new disaster icons ok')
