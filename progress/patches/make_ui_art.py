"""Poker TD UI art: button/panel 9-slice sprites, corner fade, build/machine/item card faces.
Run on the device: python3 make_ui_art.py <GameSystem2 root> <AssetPack root>"""
import os, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np

root, pack = sys.argv[1], sys.argv[2]
ui = os.path.join(root, 'Assets/Progression/Art/Resources/UI')
icons_out = os.path.join(ui, 'Icons')
faces = os.path.join(root, 'Assets/Progression/Art/Resources/BuildCards')
for d in (ui, icons_out, faces):
    os.makedirs(d, exist_ok=True)
fonts = os.path.join(root, 'Assets/Progression/Art/Fonts')
F_TITLE = os.path.join(fonts, 'Action Comics Black.ttf')
F_NUM = os.path.join(fonts, 'Super Chips.ttf')
renders = os.path.join(root, '_to_delete/icons')
OUTLINE = (45, 45, 45, 255)
S = 4  # supersampling


def rr(size, radius, fill, outline=None, width=0):
    im = Image.new('RGBA', size, (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, size[0] - 1, size[1] - 1], radius, fill=outline or fill)
    if outline:
        d.rounded_rectangle([width, width, size[0] - 1 - width, size[1] - 1 - width], max(1, radius - width), fill=fill)
    return im


def down(im, size):
    return im.resize(size, Image.LANCZOS)


# 1. button: white body, grey bottom lip, dark outline (tinted by Image.color in Unity)
b = Image.new('RGBA', (96 * S, 96 * S), (0, 0, 0, 0))
d = ImageDraw.Draw(b)
d.rounded_rectangle([0, 0, 96 * S - 1, 96 * S - 1], 24 * S, fill=OUTLINE)
d.rounded_rectangle([3 * S, 3 * S, 93 * S - 1, 93 * S - 1], 21 * S, fill=(185, 185, 185, 255))   # lip
d.rounded_rectangle([3 * S, 3 * S, 93 * S - 1, 83 * S - 1], 21 * S, fill=(255, 255, 255, 255))   # face
d.rounded_rectangle([10 * S, 8 * S, 86 * S, 20 * S], 6 * S, fill=(255, 255, 255, 255))
down(b, (96, 96)).save(os.path.join(ui, 'Button9.png'))

# 2. panel: plain rounded white with thin dark outline
down(rr((96 * S, 96 * S), 20 * S, (255, 255, 255, 255), OUTLINE, 3 * S), (96, 96)).save(os.path.join(ui, 'Panel9.png'))
# price tag / pill
down(rr((64 * S, 64 * S), 30 * S, (255, 255, 255, 255), OUTLINE, 3 * S), (64, 64)).save(os.path.join(ui, 'Pill9.png'))

# 3. black corner fade (bottom-left; mirrored in Unity for other corners)
w, h = 1024, 512
y, x = np.mgrid[0:h, 0:w]
dx, dy = x / (w * 0.95), (h - 1 - y) / (h * 0.95)
dist = np.sqrt(dx ** 2 + dy ** 2)
a = np.clip(1.0 - dist, 0, 1) ** 1.6 * 235
fade = np.zeros((h, w, 4), np.uint8)
fade[..., 3] = a.astype(np.uint8)
Image.fromarray(fade).save(os.path.join(ui, 'FadeCorner.png'))

# 4. icons from Kenney board-game icons (white versions are tinted in Unity)
kicons = os.path.join(pack, 'UI/kenney_board-game-icons/PNG/Double (128px)')
for n in ['suit_hearts', 'tokens_stack', 'structure_tower', 'flask_full', 'cards_fan', 'crown_a', 'hourglass', 'skull', 'fire', 'cards_stack', 'token', 'pawns']:
    src = os.path.join(kicons, n + '.png')
    if os.path.exists(src):
        Image.open(src).convert('RGBA').save(os.path.join(icons_out, n + '.png'))


# 5. card faces, same size and corner as the playing cards (264 x 400, outline 2 px)
CW, CH = 264, 400
RAD = 23


def crop_alpha(im):
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def fit(im, box):
    im = crop_alpha(im)
    s = min(box[0] / im.width, box[1] / im.height)
    return im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)


def text_outlined(draw, xy, text, font, fill, stroke, sw):
    draw.text(xy, text, font=font, fill=fill, anchor='mm', stroke_width=sw, stroke_fill=stroke)


def can_title(text):
    return all(c.isalpha() or c == ' ' for c in text)


def face(name, color, icon_img, kind, out):
    c = np.array(color)
    im = Image.new('RGBA', (CW * S, CH * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, CW * S - 1, CH * S - 1], RAD * S, fill=OUTLINE)
    d.rounded_rectangle([2 * S, 2 * S, CW * S - 1 - 2 * S, CH * S - 1 - 2 * S], (RAD - 2) * S, fill=(251, 246, 234, 255))
    # inner frame
    m = 12 * S
    d.rounded_rectangle([m, m, CW * S - 1 - m, CH * S - 1 - m], 14 * S, outline=tuple(int(v) for v in c) + (255,), width=3 * S)
    # header band
    d.rounded_rectangle([m, m, CW * S - 1 - m, m + 66 * S], 14 * S, fill=tuple(int(v) for v in c) + (255,))
    d.rectangle([m, m + 40 * S, CW * S - 1 - m, m + 66 * S], fill=tuple(int(v) for v in c) + (255,))
    # glow behind the icon
    glow = Image.new('RGBA', im.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    light = tuple(int(v * 0.35 + 255 * 0.65) for v in c)
    cx, cy = CW * S // 2, 205 * S
    gd.ellipse([cx - 100 * S, cy - 92 * S, cx + 100 * S, cy + 92 * S], fill=light + (255,))
    glow = glow.filter(ImageFilter.GaussianBlur(14 * S))
    im = Image.alpha_composite(im, glow)
    d = ImageDraw.Draw(im)
    # icon
    if icon_img is not None:
        ic = fit(icon_img, (200 * S, 180 * S))
        im.alpha_composite(ic, (cx - ic.width // 2, cy - ic.height // 2 + 6 * S))
    # name in the header
    font_path = F_TITLE if can_title(name) else F_NUM
    size = 44
    while size > 12:   # shrink until the name fits the header band
        f = ImageFont.truetype(font_path, size * S)
        # measure the real ink: this font draws far outside its own metrics
        probe = Image.new('L', (CW * S * 2, 200 * S), 0)
        ImageDraw.Draw(probe).text((CW * S, 100 * S), name.upper(), font=f, fill=255, anchor='mm', stroke_width=3 * S, stroke_fill=255)
        x0, y0, x1, y1 = probe.getbbox() or (0, 0, 0, 0)
        if x1 - x0 <= 200 * S and y1 - y0 <= 46 * S:
            break
        size -= 2
    text_outlined(d, (cx, m + 34 * S), name.upper(), f, (255, 255, 255, 255), OUTLINE, 3 * S)
    # kind at the bottom
    f2 = ImageFont.truetype(F_NUM, 24 * S)
    text_outlined(d, (cx, CH * S - 44 * S), kind, f2, tuple(int(v * 0.7) for v in c) + (255,), (251, 246, 234, 255), 0)
    down(im, (CW, CH)).save(out)


def hexc(h):
    return [int(h[i:i + 2], 16) for i in (0, 2, 4)]


def render(n):
    p = os.path.join(renders, n + '.png')
    return Image.open(p).convert('RGBA') if os.path.exists(p) else None


def up(im):
    return im.resize((im.width * S, im.height * S), Image.LANCZOS) if im is not None else None


towers = [('Cannon', '3E4A6B', 'weapon-cannon'), ('Gatling', 'C9961E', 'weapon-turret'), ('Trap', '8A3A8F', 'weapon-catapult')]
for name, col, model in towers:
    face(name, hexc(col), up(render(model)), 'TOWER', os.path.join(faces, f'Tower_{name}.png'))
machines = [('+Chips', '3F7FE6'), ('+%Chips', '2AA8B8'), ('+Mult', 'E0483B'), ('xMult', 'C93C9F')]
crystal = up(render('detail-crystal-large'))
for i, (name, col) in enumerate(machines):
    face(name, hexc(col), crystal, 'MACHINE', os.path.join(faces, f'Machine_{i}.png'))
face('Machine', hexc('6B6B7A'), crystal, 'MACHINE', os.path.join(faces, 'Machine.png'))
flask = Image.open(os.path.join(kicons, 'flask_full.png')).convert('RGBA')
flask = np.array(flask); flask[..., :3] = np.array([60, 140, 90]) * (flask[..., :3] / 255.0)   # tint the white icon green
face('Item', hexc('2F8F5B'), up(Image.fromarray(flask.astype(np.uint8))), 'ITEM', os.path.join(faces, 'Item.png'))
print('done', sorted(os.listdir(faces)), sorted(os.listdir(icons_out)))
