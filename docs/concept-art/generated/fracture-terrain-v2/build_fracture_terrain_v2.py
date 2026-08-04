"""균열 지형 타일셋 v2 생성기.

v1(`build_fracture_gameplay_art.py`의 `terrain_tiles()`)은 PIL 다각형에 fill+outline을 준
플레이스홀더라 24칸이 전부 같은 납작한 사각형이었다. 게임 화면이 박스로 보이는 원인이다.

이 스크립트는 새로 그리지 않는다. 균열에는 이미 이미지 생성 패스를 거친 일러스트가 있고
(`Fracture_Platforms_v1.png` = 고딕 크리스탈 발판, 정투영 측면도), 4K 룸 배경 15장에는
같은 화가의 기둥·아치가 있다. 거기서 잘라 조립하므로 팔레트와 화풍이 자동으로 일치한다.

출력 규격은 `FractureEnvironmentArtSlicer`의 시트 정의와 맞물린다:
  - 6열 x 4행, 셀 256x176 → 시트 1536x704
  - 피벗 Bottom, PPU 32
  - 이름은 슬라이서가 붙인다(FractureTerrain_r{행}_c{열}, r1이 최상단)

행 배정은 런타임 선택기(`CameraLockedRoomBackground`)가 요구하는 역할과 1:1이다:
  r1 바닥 윗면   좌 끝단 / 중간 4종 / 우 끝단
  r2 세로 벽면   좌향 / 우향 / 좁은 기둥 / 넓은 기둥 / 상단 모서리 좌·우
  r3 천장·아랫면 좌 / 중 / 우 / 부유 발판 밑면 3종
  r4 특수        아치 / 경사 좌·우 / 붕괴 직전 / 부유 끝단 2종
"""

from pathlib import Path
import random

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageStat

ROOT = Path(__file__).resolve().parents[4]
ART = ROOT / "HiddenWeight/Assets/Art/Fracture"
PLATFORMS = ART / "Environment/Terrain/Fracture_Platforms_v1.png"
ROOMS = ART / "Rooms4K"
OUT = ART / "Environment/Terrain/Fracture_TerrainTiles_v2.png"
MODULE_OUT = ART / "Environment/Terrain"
MODULE_NAMES = {
    "SurfaceLeft": "Fracture_TraversalSurfaceLeft_v3.png",
    "SurfaceMiddle": "Fracture_TraversalSurfaceMiddle_v3.png",
    "SurfaceRight": "Fracture_TraversalSurfaceRight_v3.png",
    "WallTop": "Fracture_TraversalWallTop_v3.png",
    "WallMiddle": "Fracture_TraversalWallMiddle_v3.png",
    "WallBottom": "Fracture_TraversalWallBottom_v3.png",
    "Fill": "Fracture_TraversalFill_v3.png",
}

COLUMNS, ROWS = 6, 4
# 셀은 정사각이고 시트는 1536x1024다. 크기를 2의 거듭제곱에서 벗어나게 하면 유니티가 늘려
# 임포트하고(1536x704 → 2048x512), 슬라이서가 그 크기로 계산한 좌표 일부가 텍스처 밖으로
# 나가 오른쪽 두 열의 서브에셋이 통째로 사라진다. v1과 같은 규격을 유지할 것.
CELL_W, CELL_H = 256, 256

# 발판 시트는 3열 x 4행이고 셀은 512x256이다. 각 칸의 그림은 위에서부터
# 꽃 / 대리석 상판 / 고딕 트레이서리 / 크리스탈 밑면 / 현수 케이블 순으로 쌓여 있다.
# 지형 타일에는 케이블이 필요 없다(발판에 매달린 장식이라 바닥에 붙으면 어색하다).
PLATFORM_CELL = (512, 256)
PLATFORM_GRID = (3, 4)


def load_platforms():
    """발판 시트를 읽고 크로마키 잔재를 지운다."""
    sheet = Image.open(PLATFORMS).convert("RGBA")
    return despill(sheet)


def despill(im):
    """녹색이 우세한 픽셀을 주변 색으로 눌러 크로마키 얼룩을 없앤다.

    이 시트는 알파가 0/255 이진이라 반투명 가장자리 번짐은 없지만, 불투명 영역 안에
    녹색 반점이 4% 남아 있다(등나무 잎 주변). 색상만 중화하고 알파는 건드리지 않는다.
    """
    r, g, b, a = im.split()
    # 녹색이 적·청 평균보다 튀는 만큼만 끌어내린다.
    rb_mean = ImageChops.add(r.point(lambda v: v // 2), b.point(lambda v: v // 2))
    g_clamped = ImageChops.darker(g, rb_mean.point(lambda v: min(255, int(v * 1.12))))
    return Image.merge("RGBA", (r, g_clamped, b, a))


def platform_cell(sheet, col, row):
    """발판 한 칸을 잘라 내용 경계로 다듬는다."""
    cw, ch = PLATFORM_CELL
    cell = sheet.crop((col * cw, row * ch, (col + 1) * cw, (row + 1) * ch))
    box = cell.getbbox()
    return cell.crop(box) if box else cell


def strip_cable(cell):
    """현수 케이블(그림 아래쪽 매달린 선)을 잘라 낸다.

    케이블은 크리스탈 밑면이 끝난 뒤 빈 줄을 두고 시작한다. 케이블 자체도 가로로는 길어서
    "픽셀이 적은 줄"로는 구분되지 않는다 — 본체와 케이블 사이의 **빈 구간**을 찾아야 한다.
    본체 밀도를 한 번 넘긴 뒤 처음으로 거의 비는 줄에서 자른다.
    """
    alpha = cell.getchannel("A")
    width, height = cell.size
    counts = [
        sum(1 for x in range(width) if alpha.getpixel((x, y)) > 8)
        for y in range(height)
    ]
    peak = max(counts) if counts else 0
    if peak == 0:
        return cell

    seen_body = False
    for y in range(height):
        if counts[y] >= peak * 0.40:
            seen_body = True
        elif seen_body and counts[y] <= peak * 0.10:
            return cell.crop((0, 0, width, y))
    return cell


def grade(im, top_scale=1.0, side_scale=1.0, glass_scale=1.0, saturation=1.0):
    """명도를 상단·측면·하단 세 단으로 고정하고 채도를 눌러 준다.

    원화는 청록 유리와 금색 장식의 채도가 배경보다 높아, 그대로 쓰면 지형이 세계의 일부가
    아니라 화면 위에 얹힌 UI 조각처럼 보인다. 또 조각마다 원화의 조명이 조금씩 달라 이어
    붙이면 연결부가 드러난다. 밟는 면(밝음) / 측면(한 단 어두운 라벤더) / 아랫면(청록 유리)의
    관계를 코드로 고정하면 어느 조각에서 잘라 왔든 같은 위계로 읽힌다.
    """
    px = im.convert("RGBA").load()
    width, height = im.size
    out = Image.new("RGBA", im.size)
    dst = out.load()

    # 세로 위치로 세 단을 나눈다. 발판 그림은 항상 위에서부터 상판 / 트레이서리 / 크리스탈이다.
    #
    # 경계에서 배율을 뚝 바꾸면 안 된다 — 그림 위에 가로 띠가 그어져 원화에 없던 층이 생긴다.
    # 세 값 사이를 부드럽게 이어 "위가 밝고 아래로 갈수록 유리"라는 관계만 남긴다.
    def band_at(t):
        if t <= 0.30:
            return top_scale
        if t <= 0.55:
            k = (t - 0.30) / 0.25
            return top_scale + (side_scale - top_scale) * (k * k * (3 - 2 * k))
        if t <= 0.78:
            k = (t - 0.55) / 0.23
            return side_scale + (glass_scale - side_scale) * (k * k * (3 - 2 * k))
        return glass_scale

    bands = [band_at(y / max(1, height - 1)) for y in range(height)]

    for y in range(height):
        band = bands[y]
        for x in range(width):
            r, g, b, a = px[x, y]
            if a == 0:
                dst[x, y] = (0, 0, 0, 0)
                continue
            grey = (r * 299 + g * 587 + b * 114) // 1000
            r = grey + (r - grey) * saturation
            g = grey + (g - grey) * saturation
            b = grey + (b - grey) * saturation
            dst[x, y] = (
                min(255, max(0, int(r * band))),
                min(255, max(0, int(g * band))),
                min(255, max(0, int(b * band))),
                a,
            )
    return out


def solid_top(cell):
    """대리석 상판이 시작하는 줄부터 잘라 낸다.

    상판 위에는 꽃이 듬성듬성 솟아 있어 그 구간은 대부분 투명하다. 그대로 두면 두 가지가
    깨진다 — 바닥 타일은 그림의 맨 위가 밟는 면이 되어야 하는데 꽃 끝이 그 자리를 차지해
    플레이어가 대리석 위로 떠 보이고, 벽으로 쌓으면 투명한 띠가 켜 사이의 검은 줄이 된다.
    """
    width, height = cell.size
    alpha = cell.getchannel("A")
    for y in range(height):
        filled = sum(1 for x in range(0, width, 2) if alpha.getpixel((x, y)) > 8)
        if filled >= (width // 2) * 0.94:
            return cell.crop((0, y, width, height))
    return cell


def slab_only(cell):
    """대리석 상판과 트레이서리만 남기고 크리스탈 밑면을 뺀다.

    크리스탈은 아래로 자라는 장식이라 세로로 쌓는 벽 켜에 쓰면 옆으로 뻗는 가시가 된다.
    12칸이 모두 같은 구성(꽃 / 상판 / 트레이서리 / 크리스탈)이라 색 판정을 돌리는 것보다
    고정 비율로 자르는 편이 예측 가능하다.
    """
    trimmed = solid_top(cell)
    width, height = trimmed.size
    return trimmed.crop((0, 0, width, max(1, round(height * 0.62))))


def clean_tail(cell):
    """현수 케이블의 남은 부착부를 지운다.

    `strip_cable`은 본체 아래로 매달린 부분만 잘라 낸다. 케이블이 발판 모서리에서 시작해
    아래로 뻗는 첫 구간은 본체 높이 안에 있어 그대로 남고, 끝단 타일에 가는 사선으로 보인다.
    본체 아래쪽에서 픽셀이 성긴 줄은 크리스탈이 아니라 케이블이므로 그 줄을 비운다.
    """
    alpha = cell.getchannel("A")
    width, height = cell.size
    counts = [
        sum(1 for x in range(width) if alpha.getpixel((x, y)) > 8)
        for y in range(height)
    ]
    peak = max(counts) if counts else 0
    if peak == 0:
        return cell

    out = cell.copy()
    clear = Image.new("RGBA", (width, 1), (0, 0, 0, 0))
    for y in range(round(height * 0.62), height):
        if counts[y] < peak * 0.30:
            out.paste(clear, (0, y))
    return out


def cover(im, size, ax="center", ay="top"):
    """비율을 지킨 채 셀을 **빈틈없이** 채우고 넘치는 쪽을 자른다.

    여백을 남기면 안 된다. 유니티 스프라이트의 경계는 셀 사각형 전체이므로, 그림이 셀보다
    작으면 런타임이 타일을 이어 붙였을 때 그 여백만큼 벌어져 바닥에 줄무늬 구멍이 생긴다.

    세로 기준은 위쪽이 기본이다 — 바닥 타일은 그림의 윗면이 곧 밟는 면이라 위가 잘리면
    안 된다. 끝단 타일은 마감된 모서리가 바깥쪽에 있으므로 ax로 그쪽을 붙잡는다.
    """
    scale = max(size[0] / im.width, size[1] / im.height)
    scaled = im.resize(
        (max(size[0], round(im.width * scale)), max(size[1], round(im.height * scale))),
        Image.LANCZOS,
    )
    left = {
        "left": 0,
        "center": (scaled.width - size[0]) // 2,
        "right": scaled.width - size[0],
    }[ax]
    top = {
        "top": 0,
        "center": (scaled.height - size[1]) // 2,
        "bottom": scaled.height - size[1],
    }[ay]
    return scaled.crop((left, top, left + size[0], top + size[1]))


def slice_h(im, start, end):
    """가로 비율 구간을 잘라 낸다."""
    return im.crop((round(im.width * start), 0, round(im.width * end), im.height))


def tilt(im, degrees):
    """수직선을 미세하게 기울인다(설계 2.1 — 균열은 수직이 어긋나 있다)."""
    return im.rotate(degrees, resample=Image.BICUBIC, expand=True)


def crack(im, seed_x):
    """붕괴 예정 타일의 아주 약한 균열. 외형만으로 구분되면 예지의 존재 이유가 사라지므로
    (설계 2.1) 알파를 낮게 유지한다."""
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y = seed_x, im.height // 4
    points = [(x, y)]
    for step in range(5):
        x += 9 if step % 2 else -6
        y += im.height // 9
        points.append((x, y))
    d.line(points, fill=(96, 84, 140, 70), width=3)
    out = im.copy()
    out.alpha_composite(layer.filter(ImageFilter.GaussianBlur(1)))
    return out


def smooth_vertical_seam(im, seam_x, radius=10):
    """서로 다른 원화 조각이 만나는 세로 경계를 좁은 구간에서 보간한다."""
    out = im.copy()
    px = out.load()
    left_x = max(0, seam_x - radius)
    right_x = min(out.width - 1, seam_x + radius)
    span = max(1, right_x - left_x)
    for y in range(out.height):
        left = px[left_x, y]
        right = px[right_x, y]
        for x in range(left_x + 1, right_x):
            t = (x - left_x) / span
            px[x, y] = tuple(round(a + (b - a) * t) for a, b in zip(left, right))
    return out


def feather_repeat_edges(im, radius=10):
    """반복되는 중앙 모듈의 마지막·첫 픽셀을 같게 만들고 안쪽으로 부드럽게 푼다."""
    out = im.copy()
    px = out.load()
    radius = min(radius, out.width // 4)
    for y in range(out.height):
        for distance in range(radius):
            lx = distance
            rx = out.width - 1 - distance
            left, right = px[lx, y], px[rx, y]
            average = tuple(round((a + b) * 0.5) for a, b in zip(left, right))
            t = distance / max(1, radius - 1)
            px[lx, y] = tuple(round(a + (b - a) * t) for a, b in zip(average, left))
            px[rx, y] = tuple(round(a + (b - a) * t) for a, b in zip(average, right))
    return out


def make_calm_wall_strip(wall_tiles, size=(256, 768)):
    """기존 벽 팔레트를 저주파 질감으로 바꿔 블록 문양의 반복을 숨긴다."""
    # 실제 원화에서 평균 팔레트만 가져온다. 문양을 흐리게 남기면 결국 흐릿한 블록이
    # 반복되므로, 중앙 면은 재질만 말하고 장식은 상·하단 캡에 맡긴다.
    means = [ImageStat.Stat(tile.convert("RGB")).mean[:3] for tile in wall_tiles]
    base = tuple(round(sum(values) / len(values)) for values in zip(*means))
    top = tuple(min(255, round(value * 1.07)) for value in base)
    bottom = (
        max(0, round(base[0] * 0.82)),
        min(255, round(base[1] * 0.94)),
        min(255, round(base[2] * 1.08)),
    )

    rng = random.Random(314159)
    noise = Image.new("L", (32, 96))
    noise.putdata([rng.randrange(72, 184) for _ in range(32 * 96)])
    noise = noise.filter(ImageFilter.GaussianBlur(2.2)).resize(
        size, Image.Resampling.BICUBIC)

    opaque = Image.new("RGBA", size, (0, 0, 0, 255))
    pixels = opaque.load()
    noise_pixels = noise.load()
    for y in range(size[1]):
        t = y / max(1, size[1] - 1)
        color = tuple(round(a + (b - a) * t) for a, b in zip(top, bottom))
        for x in range(size[0]):
            variation = (noise_pixels[x, y] - 128) * 0.10
            pixels[x, y] = tuple(
                min(255, max(0, round(channel + variation))) for channel in color
            ) + (255,)

    # 가장자리의 얇은 세로 몰딩은 벽을 하나의 구조물로 묶되, 칸마다 반복되는 문양은 만들지 않는다.
    rails = Image.new("RGBA", size, (0, 0, 0, 0))
    rail_draw = ImageDraw.Draw(rails)
    for x in (13, size[0] - 14):
        rail_draw.line((x, 0, x, size[1]), fill=(238, 229, 244, 44), width=2)
    for x in (17, size[0] - 18):
        rail_draw.line((x, 0, x, size[1]), fill=(76, 88, 128, 38), width=2)
    opaque.alpha_composite(rails)

    # 긴 대리석 결. 규칙적인 타일 경계가 아니라 전체 높이를 가로지르는 낮은 대비의 선이다.
    veins = Image.new("RGBA", size, (0, 0, 0, 0))
    vein_draw = ImageDraw.Draw(veins)
    for _ in range(7):
        x = rng.randrange(28, size[0] - 28)
        points = []
        for y in range(-20, size[1] + 80, 80):
            x = min(size[0] - 24, max(24, x + rng.randrange(-18, 19)))
            points.append((x, y))
        vein_draw.line(points, fill=(92, 91, 132, 18), width=2)
    opaque.alpha_composite(veins.filter(ImageFilter.GaussianBlur(0.7)))

    dark = tuple(max(0, round(value * 0.72)) for value in base) + (30,)
    light = tuple(min(255, round(value * 1.08)) for value in base) + (22,)
    joints = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(joints)
    for y in range(256, size[1], 256):
        draw.line((0, y, size[0], y), fill=dark, width=2)
        draw.line((0, y + 2, size[0], y + 2), fill=light, width=1)
    opaque.alpha_composite(joints)
    return feather_repeat_edges(opaque, 8)


def make_wall_cap(tile, top):
    """연속 벽의 시작과 끝에만 쓰는 256x192 마감 조각."""
    fitted = cover(tile, (256, 256), ay="top" if top else "bottom")
    return fitted.crop((0, 0, 256, 192) if top else (0, 64, 256, 256))


def make_low_contrast_fill(wall_middle):
    """두꺼운 충돌 덩어리 안쪽을 위한, 장식 없는 저대비 재질."""
    fill = wall_middle.resize((512, 512), Image.Resampling.BICUBIC)
    fill = ImageEnhance.Contrast(fill).enhance(0.28)
    return feather_repeat_edges(fill, 12)


def build_continuous_modules(graded_tiles):
    """v2의 역할별 셀에서 긴 수평·수직 v3 모듈을 파생한다."""
    surface_middle = Image.new("RGBA", (1024, 256), (0, 0, 0, 0))
    for index, tile in enumerate(graded_tiles[1:5]):
        surface_middle.alpha_composite(tile, (index * CELL_W, 0))
    for seam_x in (256, 512, 768):
        surface_middle = smooth_vertical_seam(surface_middle, seam_x)
    surface_middle = feather_repeat_edges(surface_middle)

    wall_middle = make_calm_wall_strip(graded_tiles[6:10])
    modules = {
        "SurfaceLeft": graded_tiles[0],
        "SurfaceMiddle": surface_middle,
        "SurfaceRight": graded_tiles[5],
        "WallTop": make_wall_cap(graded_tiles[10], top=True),
        "WallMiddle": wall_middle,
        "WallBottom": make_wall_cap(graded_tiles[14], top=False),
        "Fill": make_low_contrast_fill(wall_middle),
    }

    expected = {
        "SurfaceLeft": (256, 256),
        "SurfaceMiddle": (1024, 256),
        "SurfaceRight": (256, 256),
        "WallTop": (256, 192),
        "WallMiddle": (256, 768),
        "WallBottom": (256, 192),
        "Fill": (512, 512),
    }
    MODULE_OUT.mkdir(parents=True, exist_ok=True)
    for role, image in modules.items():
        image = image.convert("RGBA")
        if image.size != expected[role] or image.getbbox() is None:
            raise ValueError(f"invalid {role}: size={image.size} bbox={image.getbbox()}")
        path = MODULE_OUT / MODULE_NAMES[role]
        image.save(path)
        print(f"wrote {path} {image.size}")
    return modules


def build():
    sheet = load_platforms()
    # 폭이 다른 세 종류를 모두 쓴다. 좁은 칸은 끝단, 넓은 칸은 중간 반복에 적합하다.
    # 바닥·천장 타일은 대리석 윗면이 곧 밟는 면이므로 꽃 구간을 떼고 시작한다.
    def body(col, row):
        return solid_top(clean_tail(strip_cable(platform_cell(sheet, col, row))))

    narrow = body(0, 0)
    medium = body(1, 1)
    wide = body(2, 0)
    wide_alt = body(2, 2)

    cell = (CELL_W, CELL_H)
    tiles = []

    # --- r1: 바닥 윗면 ---
    # 좌·우 끝단은 발판 그림의 실제 끝을 쓴다. 마감된 모서리가 이미 그려져 있으므로
    # 그 모서리가 잘리지 않도록 바깥쪽으로 붙여 자른다.
    tiles.append(cover(slice_h(narrow, 0.0, 0.34), cell, ax="left"))
    tiles.append(cover(slice_h(wide, 0.30, 0.58), cell))
    tiles.append(cover(slice_h(medium, 0.32, 0.62), cell))
    tiles.append(cover(slice_h(wide_alt, 0.26, 0.56), cell))
    tiles.append(cover(slice_h(wide, 0.48, 0.76), cell))
    tiles.append(cover(slice_h(narrow, 0.66, 1.0), cell, ax="right"))

    # --- r2: 세로 벽면 ---
    # 4K 룸 배경은 알파가 없어 하늘째로 잘려 나온다(불투명 사각형이 된다). 벽면도 발판에서
    # 파생한다.
    #
    # 조각을 90도 돌려 기둥을 만들면 폭이 좁아 셀을 채우려면 3배 확대해야 하고 뭉갠다.
    # 대신 크리스탈을 뺀 대리석 **가로 켜**를 그대로 쓴다 — 런타임이 세로로 쌓으면
    # 고딕 석조의 층이 되어 원본 해상도를 그대로 유지한다.
    slab_narrow = slab_only(narrow)
    slab_medium = slab_only(medium)
    slab_wide = slab_only(wide)
    slab_alt = slab_only(wide_alt)
    tiles.append(cover(slice_h(slab_medium, 0.30, 0.60), cell, ay="center"))
    tiles.append(cover(
        slice_h(slab_medium, 0.30, 0.60).transpose(Image.FLIP_LEFT_RIGHT), cell, ay="center"))
    tiles.append(cover(slice_h(slab_narrow, 0.32, 0.68), cell, ay="center"))
    tiles.append(cover(slice_h(slab_wide, 0.36, 0.66), cell, ay="center"))
    # 상단 모서리: 바닥 끝단의 마감을 그대로 쓴다. 벽이 바닥과 만나는 자리라 이어져야 한다.
    tiles.append(cover(slice_h(slab_alt, 0.0, 0.24), cell, ax="left", ay="center"))
    tiles.append(cover(slice_h(slab_alt, 0.76, 1.0), cell, ax="right", ay="center"))

    # --- r3: 천장·아랫면 ---
    # 발판을 뒤집으면 크리스탈이 위로 자라 천장이 된다. 천장은 아래쪽 끝이 플레이어가
    # 부딪히는 면이므로 아래를 기준으로 자른다.
    flipped = wide.transpose(Image.FLIP_TOP_BOTTOM)
    flipped_alt = wide_alt.transpose(Image.FLIP_TOP_BOTTOM)
    tiles.append(cover(slice_h(flipped, 0.0, 0.30), cell, ax="left", ay="bottom"))
    tiles.append(cover(slice_h(flipped, 0.34, 0.66), cell, ay="bottom"))
    tiles.append(cover(slice_h(flipped, 0.70, 1.0), cell, ax="right", ay="bottom"))
    tiles.append(cover(slice_h(flipped_alt, 0.20, 0.50), cell, ay="bottom"))
    tiles.append(cover(slice_h(medium, 0.20, 0.50), cell))
    tiles.append(cover(slice_h(wide_alt, 0.44, 0.74), cell))

    # --- r4: 특수 ---
    # 아치는 넣지 않는다. 런타임 선택기가 쓰지 않는 역할이라 칸만 차지한다.
    tiles.append(crack(cover(slice_h(medium, 0.32, 0.62), cell), CELL_W // 2))
    tiles.append(cover(tilt(slice_h(wide, 0.24, 0.66), 7), cell))
    tiles.append(cover(tilt(slice_h(wide, 0.24, 0.66), -7), cell))
    tiles.append(cover(slice_h(wide_alt, 0.0, 0.30), cell, ax="left"))
    tiles.append(cover(slice_h(wide_alt, 0.70, 1.0), cell, ax="right"))
    tiles.append(cover(slice_h(narrow, 0.30, 0.70), cell))

    # 행별로 명도 3단과 채도를 고정한다. 어느 조각에서 잘라 왔든 같은 위계로 읽히게 하는 것이
    # 목적이다 — 밟는 면이 가장 밝고, 측면이 한 단 어두운 라벤더, 아랫면이 청록 유리.
    #
    # 꽃 장식은 바닥·벽 타일에서 이미 빠져 있다(solid_top이 상판 윗줄부터 자른다). 끝단에만
    # 남기고 싶어도 그럴 수 없다 — 그림의 맨 위가 곧 밟는 면이라, 끝단에만 꽃을 남기면 그
    # 타일의 대리석이 옆 타일보다 아래로 내려가 이음매에 턱이 생긴다. 대신 특수 행(r4)에는
    # 남겨 둔다.
    SAT = 0.8
    graders = [
        lambda t: grade(t, 1.0, 0.84, 0.9, SAT),    # r1 바닥: 위 밝음 → 아래 유리
        lambda t: grade(t, 0.84, 0.8, 0.78, SAT),   # r2 벽면: 통째로 한 단 어둡게
        lambda t: grade(t, 0.9, 0.84, 1.0, SAT),    # r3 천장: 뒤집혀 있어 순서도 뒤집는다
        lambda t: grade(t, 1.0, 0.86, 0.92, SAT),   # r4 특수
    ]

    graded_tiles = []
    out = Image.new("RGBA", (COLUMNS * CELL_W, ROWS * CELL_H), (0, 0, 0, 0))
    for index, tile in enumerate(tiles):
        col, row = index % COLUMNS, index // COLUMNS
        graded = graders[row](tile)
        graded_tiles.append(graded)
        out.alpha_composite(graded, (col * CELL_W, row * CELL_H))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT)
    print(f"wrote {OUT} {out.size}")
    build_continuous_modules(graded_tiles)
    return out


if __name__ == "__main__":
    build()
