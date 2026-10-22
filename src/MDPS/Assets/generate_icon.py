"""Create the MDPS Windows icon from simple, scalable shapes."""

from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
SIZE = 1024

canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
mask = Image.new("L", (SIZE, SIZE), 0)
ImageDraw.Draw(mask).rounded_rectangle((35, 35, 989, 989), radius=210, fill=255)

background = Image.new("RGBA", (SIZE, SIZE))
pixels = background.load()
for y in range(SIZE):
    for x in range(SIZE):
        glow = max(0, 1 - ((x - 790) ** 2 + (y - 750) ** 2) ** 0.5 / 860)
        pixels[x, y] = (14 + int(8 * glow), 22 + int(19 * glow), 35 + int(21 * glow), 255)
canvas.paste(background, (0, 0), mask)

draw = ImageDraw.Draw(canvas)
draw.rounded_rectangle((35, 35, 989, 989), radius=210, outline=(104, 231, 202, 155), width=24)

# The large M remains legible at 16 px; the three bars suggest a DPS meter.
path = [(210, 620), (210, 272), (512, 540), (814, 272), (814, 620)]
draw.line(path, fill=(90, 235, 206, 255), width=100, joint="curve")
for x, y in (path[0], path[-1]):
    draw.ellipse((x - 50, y - 50, x + 50, y + 50), fill=(90, 235, 206, 255))

for left, top, right, color in (
    (228, 782, 388, (65, 183, 159, 255)),
    (432, 728, 592, (91, 221, 194, 255)),
    (636, 672, 796, (229, 132, 158, 255)),
):
    draw.rounded_rectangle((left, top, right, 865), radius=35, fill=color)

sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
canvas.save(HERE / "mdps.ico", format="ICO", sizes=sizes)
canvas.resize((256, 256), Image.Resampling.LANCZOS).save(HERE / "mdps-preview.png")
