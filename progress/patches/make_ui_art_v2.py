"""Poker TD UI art v2 (2026-11-24, Wong's feedback):
- build/machine/item card faces in the same style as the playing cards: white face, thin dark
  outline, corner index (letters + small icon) top-left and, turned around, bottom-right;
  the model in the middle; the name and the kind (TOWER/MACHINE/ITEM) at the bottom;
- ZoneFade.png: soft black rounded area for the HUD card zones (9-slice, alpha set in Unity);
- ItemZone.png: black rounded mat with a dashed line and 4 faint dashed slots, for the 3D item area.
Run on the device: python3 make_ui_art_v2.py <GameSystem2 root> <AssetPack root>"""
import os, sys, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np

root, pack = sys.argv[1], sys.argv[2]
ui = os.path.join(root, 'Assets/Progression/Art/Resources/UI')
faces = os.path.join(root, 'Assets/Progression/Art/Resources/BuildCards')
F_NUM = os.path.join(root, 'Assets/Progression/Art/Fonts/Super Chips.ttf')
renders = os.path.join(root, '_to_delete/icons')
kicons = os.path.join(pack, 'UI/kenney_board-game-icons/PNG/Double (128px)')
OUTLINE = (45, 45, 45, 255)
S = 4
CW, CH, RAD = 264, 400, 23


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def tint_icon(name, color, size):
    im = Image.open(os.path.join(kicons, name + '.png')).convert('RGBA')
    a = np.array(im).astype(float)
    a[..., :3] = np.array(color) * (a[..., :3] / 255.0)
    im = Image.fromarray(a.astype(np.uint8))
    im = im.crop(im.getbbox())
    s = size / max(im.width, im.height)
    return im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)


def render(n):
    p = os.path.join(renders, n + '.png')
    if not os.path.exists(p):
        return None
    im = Image.open(p).convert('RGBA')
    return im.crop(im.getbbox())


def fit(im, w, h):
    s = min(w / im.width, h / im.height)
    return im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)


def corner(index, color, icon):
    """The corner block: letters over a small icon, like rank over suit."""
    blk = Image.new('RGBA', (90 * S, 110 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(blk)
    size = 46 if len(index) == 1 else 34
    f = ImageFont.truetype(F_NUM, size * S)
    d.text((45 * S, 30 * S), index, font=f, fill=color + (255,), anchor='mm')
    ic = tint_icon(icon, color, 30 * S)
    blk.alpha_composite(ic, (45 * S - ic.width // 2, 72 * S - ic.height // 2))
    return blk


def face(name, index, color, model, icon, kind, out):
    im = Image.new('RGBA', (CW * S, CH * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, CW * S - 1, CH * S - 1], RAD * S, fill=OUTLINE)
    d.rounded_rectangle([2 * S, 2 * S, CW * S - 1 - 2 * S, CH * S - 1 - 2 * S], (RAD - 2) * S, fill=(255, 255, 255, 255))
    # a faint coloured disc behind the model
    glow = Image.new('RGBA', im.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    cx, cy = CW * S // 2, 186 * S
    light = tuple(int(v * 0.25 + 255 * 0.75) for v in color)
    gd.ellipse([cx - 92 * S, cy - 86 * S, cx + 92 * S, cy + 86 * S], fill=light + (255,))
    im = Image.alpha_composite(im, glow.filter(ImageFilter.GaussianBlur(10 * S)))
    if model is not None:
        m = fit(model.resize((model.width * S, model.height * S), Image.LANCZOS), 176 * S, 170 * S)
        im.alpha_composite(m, (cx - m.width // 2, cy - m.height // 2))
    # corners: top-left, and the same block turned around at the bottom-right
    blk = corner(index, color, icon)
    im.alpha_composite(blk, (2 * S, 6 * S))
    im.alpha_composite(blk.rotate(180), (CW * S - blk.width - 2 * S, CH * S - blk.height - 6 * S))
    d = ImageDraw.Draw(im)
    # name and kind at the bottom, between the corner blocks
    f1 = ImageFont.truetype(F_NUM, 30 * S)
    d.text((cx, 304 * S), name.upper(), font=f1, fill=(45, 45, 45, 255), anchor='mm')
    f2 = ImageFont.truetype(F_NUM, 22 * S)
    d.text((cx, 334 * S), kind, font=f2, fill=tuple(int(v * 0.75) for v in color) + (255,), anchor='mm')
    im.resize((CW, CH), Image.LANCZOS).save(out)


towers = [('Cannon', 'C', '3E4A6B', 'weapon-cannon'), ('Gatling', 'G', 'B8860B', 'weapon-turret'), ('Trap', 'T', '8A3A8F', 'trap-spikes')]
for name, idx, col, model in towers:
    face(name, idx, hexc(col), render(model), 'structure_tower', 'TOWER', os.path.join(faces, f'Tower_{name}.png'))
crystal = render('detail-crystal-large')
machines = [('+Chips', '+C', '3F7FE6'), ('+%Chips', '%C', '2AA8B8'), ('+Mult', '+M', 'E0483B'), ('xMult', 'xM', 'C93C9F')]
for i, (name, idx, col) in enumerate(machines):
    face(name, idx, hexc(col), crystal, 'token', 'MACHINE', os.path.join(faces, f'Machine_{i}.png'))
face('Machine', 'M', hexc('6B6B7A'), crystal, 'token', 'MACHINE', os.path.join(faces, 'Machine.png'))
flask = tint_icon('flask_full', hexc('2F8F5B'), 150)
face('', 'I', hexc('2F8F5B'), flask, 'flask_full', 'ITEM', os.path.join(faces, 'Item.png'))   # the item name is shown under the card

# ZoneFade: soft black rounded rectangle, 9-slice border 64 px
z = Image.new('L', (256, 256), 0)
ImageDraw.Draw(z).rounded_rectangle([24, 24, 231, 231], 40, fill=255)
z = z.filter(ImageFilter.GaussianBlur(12))
zone = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
zone.putalpha(z)
zone.save(os.path.join(ui, 'ZoneFade.png'))


def dashed_round_rect(draw, box, radius, dash, gap, width, fill):
    """Dashes along a rounded rectangle (walks the outline and draws every other piece)."""
    x0, y0, x1, y1 = box
    pts = []
    def arc(cx, cy, a0, a1):
        for k in range(24):
            a = math.radians(a0 + (a1 - a0) * k / 23)
            pts.append((cx + radius * math.cos(a), cy + radius * math.sin(a)))
    arc(x0 + radius, y0 + radius, 180, 270)
    arc(x1 - radius, y0 + radius, 270, 360)
    arc(x1 - radius, y1 - radius, 0, 90)
    arc(x0 + radius, y1 - radius, 90, 180)
    pts.append(pts[0])
    # resample at 1 px steps
    path = [pts[0]]
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = max(1, int(math.hypot(bx - ax, by - ay)))
        for k in range(1, n + 1):
            path.append((ax + (bx - ax) * k / n, ay + (by - ay) * k / n))
    period = dash + gap
    for i in range(0, len(path) - dash, period):
        draw.line(path[i:i + dash], fill=fill, width=width, joint='curve')


# ItemZone: 768 x 400, 4 slots of 157 x 238 px
W, H = 768 * 2, 400 * 2   # lower mat (Wong 2026-11-24: it was taller than the map)
mask = Image.new('L', (W, H), 0)
ImageDraw.Draw(mask).rounded_rectangle([20, 20, W - 21, H - 21], 90, fill=150)
mask = mask.filter(ImageFilter.GaussianBlur(10))
mat = Image.new('RGBA', (W, H), (0, 0, 0, 0))
mat.putalpha(mask)
d = ImageDraw.Draw(mat)
dashed_round_rect(d, (44, 44, W - 45, H - 45), 70, 34, 22, 8, (250, 240, 225, 230))
for i in range(4):
    cx = (118.5 + i * 177) * 2
    w, h = 157 * 2, 238 * 2
    dashed_round_rect(d, (cx - w / 2, H / 2 - h / 2, cx + w / 2, H / 2 + h / 2), 26, 18, 16, 4, (250, 240, 225, 90))
mat.resize((768, 400), Image.LANCZOS).save(os.path.join(ui, "ItemZone.png"))
print('done')
