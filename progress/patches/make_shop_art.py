"""Poker TD shop art (2026-11-24): upgrade card faces in the same style as the build cards,
and ShopZone.png (a dark mat with a dashed line and 3 faint slots, like ItemZone.png).
Reuses the helpers of make_ui_art_v2.py. Run: python3 make_shop_art.py <GameSystem2 root> <AssetPack root>"""
import os, sys
here = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(here, 'make_ui_art_v2.py'), encoding='utf-8').read()
cut = src.index('towers = [')
helpers = src[:cut] + src[src.index('def dashed_round_rect'):src.index('# ItemZone:')]
exec(helpers)

for name, idx, col, icon, kind, out in [
    ('', 'H+', '3F7FE6', 'cards_fan', 'UPGRADE', 'Upgrade_Hand'),
    ('+1 tower', 'T+', '2F8F5B', 'structure_tower', 'SLOT', 'Upgrade_TowerSlot'),
    ('+1 machine', 'M+', '2F8F5B', 'fire', 'SLOT', 'Upgrade_MachineSlot'),
    ('', 'U', 'C93C9F', 'lock_open', 'UNLOCK', 'Upgrade_Unlock'),
]:
    pic = tint_icon(icon, hexc(col), 150)
    face(name, idx, hexc(col), pic, icon, kind, os.path.join(faces, out + '.png'))

# ShopZone: 768 x 400, 3 slots (cards or pedestals)
W, H = 768 * 2, 400 * 2
mask = Image.new('L', (W, H), 0)
ImageDraw.Draw(mask).rounded_rectangle([20, 20, W - 21, H - 21], 90, fill=150)
mask = mask.filter(ImageFilter.GaussianBlur(10))
mat = Image.new('RGBA', (W, H), (0, 0, 0, 0))
mat.putalpha(mask)
d = ImageDraw.Draw(mat)
dashed_round_rect(d, (44, 44, W - 45, H - 45), 70, 34, 22, 8, (250, 240, 225, 230))
for i in range(3):
    cx = (148 + i * 236) * 2
    w, h = 190 * 2, 290 * 2
    dashed_round_rect(d, (cx - w / 2, H / 2 - h / 2, cx + w / 2, H / 2 + h / 2), 26, 18, 16, 4, (250, 240, 225, 90))
mat.resize((768, 400), Image.LANCZOS).save(os.path.join(ui, 'ShopZone.png'))
print('shop art done')

# Button3D: rounded button for the 3D shop (white, tinted in Unity), dark outline and a lower lip
W, H = 512 * 2, 160 * 2
b = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(b)
d.rounded_rectangle([8, 24, W - 9, H - 9], 60, fill=(40, 30, 25, 255))              # outline + lip
d.rounded_rectangle([20, 20, W - 21, H - 40], 50, fill=(170, 170, 170, 255))         # side (darker)
d.rounded_rectangle([20, 14, W - 21, H - 56], 50, fill=(255, 255, 255, 255))         # top face
b.resize((512, 160), Image.LANCZOS).save(os.path.join(ui, 'Button3D.png'))
print('button done')
