"""균열 숏컷 봉쇄·해제 시트를 대리석으로 다시 만든다.

`DoorShortcutTransitions_v1.png`은 PIL로 그린 플레이스홀더였다 — 32칸이 전부 창백한
사각형 두 개라, 화면에서는 문이 아니라 "덜 만든 흰 막대 두 개"로 보였다.
지형 타일셋 v2가 이미 일러스트 대리석을 갖고 있으므로 거기서 문짝을 잘라 쓴다.

기하는 v1과 똑같이 유지한다(8열 x 4행, 셀 192x192). 슬라이서와 애니메이터가 그대로
같은 이름·같은 프레임 수를 읽어야 하기 때문이다.

행 의미(FractureAnimationArtSlicer):
  1행 FractureSealClose      닫힌다 — 두 문짝이 가운데로 모인다
  2행 FractureSealOpen       열린다 — 닫힘의 역순
  3행 FractureShortcutOpen   숏컷 개방
  4행 FractureSecretPassage  비밀 통로
"""

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[4]
ART = ROOT / "HiddenWeight/Assets/Art/Fracture"
TERRAIN = ART / "Environment/Terrain/Fracture_TerrainTiles_v2.png"
OUT = ART / "Environment/Interactables/Animation/DoorShortcutTransitions_v1.png"

COLUMNS, ROWS = 8, 4
CELL = 192
# 문짝 한 짝의 규격. 셀 안에서 위아래로 약간 여백을 둔다.
LEAF_W, LEAF_H = 74, 150
TOP = (CELL - LEAF_H) // 2


def marble_leaf():
    """지형 타일의 벽 켜(r2)에서 문짝 한 짝을 잘라 낸다."""
    sheet = Image.open(TERRAIN).convert("RGBA")
    cell = sheet.width // 6
    # r2_c3 — 무늬가 가장 고른 벽 켜. 세로로 세워 문짝으로 쓴다.
    tile = sheet.crop((2 * cell, cell, 3 * cell, 2 * cell))
    leaf = tile.resize((LEAF_H, LEAF_W), Image.LANCZOS).transpose(Image.ROTATE_90)
    return leaf


def frame(leaf, closed):
    """문짝 두 짝을 closed(0~1)만큼 가운데로 모은 한 프레임."""
    canvas = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))

    # 활짝 열리면 양옆 바깥으로 물러나고, 닫히면 가운데에서 맞닿는다.
    open_x = -LEAF_W * 0.35
    closed_x = CELL / 2 - LEAF_W
    left = round(open_x + (closed_x - open_x) * closed)
    right = CELL - left - LEAF_W

    canvas.alpha_composite(leaf, (left, TOP))
    canvas.alpha_composite(leaf.transpose(Image.FLIP_LEFT_RIGHT), (right, TOP))
    return canvas


def build():
    leaf = marble_leaf()
    out = Image.new("RGBA", (COLUMNS * CELL, ROWS * CELL), (0, 0, 0, 0))

    for row in range(ROWS):
        for col in range(COLUMNS):
            t = col / (COLUMNS - 1)
            # 1·3·4행은 닫히는 방향, 2행(해제)만 반대로 간다.
            closed = 1.0 - t if row == 1 else t
            out.alpha_composite(frame(leaf, closed), (col * CELL, row * CELL))

    out.save(OUT)
    print(f"wrote {OUT} {out.size}")


if __name__ == "__main__":
    build()
