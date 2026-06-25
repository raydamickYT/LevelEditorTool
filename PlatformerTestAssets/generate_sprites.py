"""
Generates clean platformer placeholder sprites with REAL alpha transparency.
Output: ./sprites/*.png (64x64 RGBA).

These map to the pgattic/platformer JSON categories used by the level editor:
    platform -> boxes, lava -> lava, key -> keys,
    goal -> end, player_start -> start, portal -> portals
"""

import os
from PIL import Image, ImageDraw

SIZE = 64
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "sprites")
TRANSPARENT = (0, 0, 0, 0)


def new_canvas():
    img = Image.new("RGBA", (SIZE, SIZE), TRANSPARENT)
    return img, ImageDraw.Draw(img)


def save(img, name):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    print("wrote", path)


def make_platform():
    img, d = new_canvas()
    body = (24, 26, 32, 255)
    edge = (60, 62, 72, 255)
    d.rectangle([2, 24, 61, 40], fill=body)
    d.rectangle([2, 24, 61, 27], fill=edge)  # subtle top highlight
    save(img, "platform.png")


def make_lava():
    img, d = new_canvas()
    red = (208, 40, 30, 255)
    orange = (245, 140, 45, 255)
    top = 22
    d.rectangle([4, top, 59, 60], fill=red)
    # sawtooth crest along the top edge
    step = 8
    for x in range(4, 60, step):
        d.polygon(
            [(x, top), (x + step // 2, top - 6), (x + step, top)],
            fill=orange,
        )
    d.rectangle([4, top, 59, top + 3], fill=orange)
    save(img, "lava.png")


def make_key():
    img, d = new_canvas()
    gold = (240, 205, 45, 255)
    outline = (120, 90, 12, 255)
    cx, cy, r = 32, 32, 22
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=gold, outline=outline, width=3)
    # keyhole
    d.ellipse([cx - 5, cy - 9, cx + 5, cy + 1], fill=outline)
    d.polygon([(cx - 2, cy), (cx + 2, cy), (cx + 1, cy + 11), (cx - 1, cy + 11)], fill=outline)
    save(img, "key.png")


def make_goal():
    img, d = new_canvas()
    green = (40, 190, 70, 255)
    cx, cy = 32, 32
    d.ellipse([cx - 24, cy - 24, cx + 24, cy + 24], fill=green)   # outer disk
    d.ellipse([cx - 13, cy - 13, cx + 13, cy + 13], fill=TRANSPARENT)  # punch hole -> ring
    d.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], fill=green)        # center dot
    save(img, "goal.png")


def make_player_start():
    img, d = new_canvas()
    gray = (150, 150, 150, 255)
    outline = (70, 70, 70, 255)
    d.rounded_rectangle([24, 10, 40, 54], radius=4, fill=gray, outline=outline, width=2)
    save(img, "player_start.png")


def make_portal():
    img, d = new_canvas()
    purple = (150, 70, 210, 255)
    light = (190, 130, 235, 255)
    cx, cy = 32, 32
    d.ellipse([cx - 16, cy - 26, cx + 16, cy + 26], fill=purple)        # outer oval
    d.ellipse([cx - 9, cy - 18, cx + 9, cy + 18], fill=TRANSPARENT)     # punch hole -> ring
    d.ellipse([cx - 16, cy - 26, cx + 16, cy + 26], outline=light, width=2)
    save(img, "portal.png")


def main():
    make_platform()
    make_lava()
    make_key()
    make_goal()
    make_player_start()
    make_portal()
    print("done ->", OUT_DIR)


if __name__ == "__main__":
    main()
