"""Builds Assets/Resources/CookieFont.ttf, the game's hand-drawn pixel font.

The letters copy the hand-drawn 1-pixel ink line of the lettering in the UI sprites
(Cookies.png, Meters.png, cookieTitle.png), without the sprites' drop shadow.

Edit a glyph below ('#' = ink), then run from the repo root:
    pip install fonttools
    python Tools/CookieFont/make_cookie_font.py
"""
from pathlib import Path

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

PIXEL = 64                 # font units per pixel; an em is 16 pixels
UNITS_PER_EM = 16 * PIXEL
BASELINE = 10              # row of the lowest pixel of a capital; rows 11-13 hold descenders
CAP_HEIGHT = 11
X_HEIGHT = 7
ASCENT = 12 * PIXEL
DESCENT = 4 * PIXEL        # descenders plus a pixel of room under them
LINE_GAP = 2 * PIXEL
SPACE_WIDTH = 4 * PIXEL
OUTPUT = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "CookieFont.ttf"


def glyph(rows, bottom=BASELINE):
    """A glyph drawn as rows of '#'/'.', whose last row sits on the given row (baseline by default)."""
    return bottom, rows.split()


def mirror(spec):
    bottom, rows = spec
    return bottom, [row[::-1] for row in rows]


GLYPHS = {
    # Capitals: 11 rows tall.
    "A": glyph("""....#... ...#.#.. ...#.#.. ..#...#. ..#...#. .#.....# .####### .#.....# #......# #......# #......#"""),
    "B": glyph("""#####.. #....#. #.....# #.....# #....#. ######. #.....# #.....# #.....# #....#. #####.."""),
    "C": glyph("""..#####. .#.....# #......# #....... #....... #....... #....... #....... #....... .#.....# ..#####."""),
    "D": glyph("""####... #...##. #.....# #.....# #.....# #.....# #.....# #.....# #.....# #...##. ####..."""),
    "E": glyph("""###### #..... #..... #..... #..... #####. #..... #..... #..... #..... ######"""),
    "F": glyph("""###### #..... #..... #..... #..... #####. #..... #..... #..... #..... #....."""),
    "G": glyph("""..#####. .#.....# #....... #....... #....... #...#### #......# #......# #......# .#.....# ..#####."""),
    "H": glyph("""#.....# #.....# #.....# #.....# #.....# ####### #.....# #.....# #.....# #.....# #.....#"""),
    "I": glyph("""##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. #####"""),
    "J": glyph("""..#### .....# .....# .....# .....# .....# .....# .....# #....# #....# .####."""),
    "K": glyph("""#.....# #....#. #...#.. #..#... #.#.... ##..... #.#.... #..#... #...#.. #....#. #.....#"""),
    "L": glyph("""#..... #..... #..... #..... #..... #..... #..... #..... #..... #..... ######"""),
    "M": glyph(""".#.....#. .##...##. .#.#.#.#. .#.#.#.#. #..#.#..# #..#.#..# #...#...# #...#...# #.......# #.......# #.......#"""),
    "N": glyph("""#.....# ##....# ##....# #.#...# #.#...# #..#..# #...#.# #...#.# #....## #....## #.....#"""),
    "O": glyph("""..####.. .#....#. #......# #......# #......# #......# #......# #......# #......# .#....#. ..####.."""),
    "P": glyph("""#####.. #....#. #.....# #.....# #....#. #####.. #...... #...... #...... #...... #......"""),
    "Q": glyph("""..####.. .#....#. #......# #......# #......# #......# #......# #...#..# #....#.# .#....#. ..####.#"""),
    "R": glyph("""#####.. #....#. #.....# #.....# #....#. #####.. #..#... #...#.. #....#. #....#. #.....#"""),
    "S": glyph(""".#####. #.....# #...... #...... .#..... ..###.. .....#. ......# ......# #.....# .#####."""),
    "T": glyph("""####### ...#... ...#... ...#... ...#... ...#... ...#... ...#... ...#... ...#... ...#..."""),
    "U": glyph("""#.....# #.....# #.....# #.....# #.....# #.....# #.....# #.....# #.....# .#...#. ..###.."""),
    "V": glyph("""#.....# #.....# #.....# .#...#. .#...#. .#...#. ..#.#.. ..#.#.. ..#.#.. ...#... ...#..."""),
    "W": glyph("""#.......# #.......# #.......# #...#...# #...#...# #..#.#..# #..#.#..# .#.#.#.#. .#.#.#.#. ..#...#.. ..#...#.."""),
    "X": glyph("""#.....# #.....# .#...#. .#...#. ..#.#.. ...#... ..#.#.. .#...#. .#...#. #.....# #.....#"""),
    "Y": glyph("""#.....# #.....# .#...#. .#...#. ..#.#.. ...#... ...#... ...#... ...#... ...#... ...#..."""),
    "Z": glyph("""####### ......# .....#. .....#. ....#.. ...#... ..#.... .#..... .#..... #...... #######"""),
    # Lower case: 7 rows tall, ascenders reach cap height, descenders drop 3 rows.
    "a": glyph("""..###.# .#...## #.....# #.....# #.....# .#...## ..###.#"""),
    "b": glyph("""#...... #...... #...... #...... #.###.. ##...#. #.....# #.....# #.....# ##...#. #.###.."""),
    "c": glyph("""..###. .#...# #..... #..... #..... .#...# ..###."""),
    "d": glyph("""......# ......# ......# ......# ..###.# .#...## #.....# #.....# #.....# .#...## ..###.#"""),
    "e": glyph("""..###.. .#...#. #.....# ####### #...... .#..... ..####."""),
    "f": glyph("""...## ..#.. ..#.. ..#.. ##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#.."""),
    "g": glyph("""..###.# .#...## #.....# #.....# #.....# .#...## ..###.# ......# .#...#. ..###..""", 13),
    "h": glyph("""#...... #...... #...... #...... #.###.. ##...#. #.....# #.....# #.....# #.....# #.....#"""),
    "i": glyph("""# . # # # # # # #"""),
    "j": glyph("""...# .... ..## ...# ...# ...# ...# ...# ...# ...# #..# .##.""", 13),
    "k": glyph("""#..... #..... #..... #..... #...#. #..#.. #.#... ##.... #.#... #..#.. #...##"""),
    "l": glyph("""#. #. #. #. #. #. #. #. #. #. .#"""),
    "m": glyph("""#.##..##. ##..##..# #...#...# #...#...# #...#...# #...#...# #...#...#"""),
    "n": glyph("""#.###. ##...# #....# #....# #....# #....# #....#"""),
    "o": glyph("""..###.. .#...#. #.....# #.....# #.....# .#...#. ..###.."""),
    "p": glyph("""#.###.. ##...#. #.....# #.....# #.....# ##...#. #.###.. #...... #...... #......""", 13),
    "q": glyph("""..###.# .#...## #.....# #.....# #.....# .#...## ..###.# ......# ......# ......#""", 13),
    "r": glyph("""#.### ##... #.... #.... #.... #.... #...."""),
    "s": glyph(""".####. #..... #..... .####. .....# .....# #####."""),
    "t": glyph(""".#... .#... ##### .#... .#... .#... .#... .#... ..###"""),
    "u": glyph("""#.....# #.....# #.....# #.....# #.....# .#...## ..###.#"""),
    "v": glyph("""#.....# #.....# .#...#. .#...#. ..#.#.. ..#.#.. ...#..."""),
    "w": glyph("""#.......# #.......# #...#...# #...#...# .#.#.#.#. .#.#.#.#. ..#...#.."""),
    "x": glyph("""#.....# .#...#. ..#.#.. ...#... ..#.#.. .#...#. #.....#"""),
    "y": glyph("""#.....# #.....# #.....# #.....# #.....# .#...## ..###.# ......# .#...#. ..###..""", 13),
    "z": glyph("""####### .....#. ....#.. ...#... ..#.... .#..... #######"""),
    # Digits are narrower than the letters so 0 and O don't look alike.
    "0": glyph(""".####. #....# #....# #....# #....# #....# #....# #....# #....# #....# .####."""),
    "1": glyph("""..#.. .##.. #.#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.. #####"""),
    "2": glyph(""".####. #....# .....# .....# ....#. ...#.. ..#... .#.... #..... #..... ######"""),
    "3": glyph(""".####. #....# .....# .....# ....#. ..##.. ....#. .....# .....# #....# .####."""),
    "4": glyph("""....#. ...##. ...##. ..#.#. ..#.#. .#..#. #...#. ###### ....#. ....#. ....#."""),
    "5": glyph("""###### #..... #..... #..... #####. .....# .....# .....# .....# #....# .####."""),
    "6": glyph("""..###. .#.... #..... #..... #.###. ##...# #....# #....# #....# #....# .####."""),
    "7": glyph("""###### .....# .....# ....#. ....#. ...#.. ...#.. ..#... ..#... ..#... ..#..."""),
    "8": glyph(""".####. #....# #....# #....# .#..#. ..##.. .#..#. #....# #....# #....# .####."""),
    "9": glyph(""".####. #....# #....# #....# #....# .#...# ..#### .....# .....# ....#. .###.."""),
    # Punctuation and symbols.
    "!": glyph("""# # # # # # # # . . #"""),
    '"': glyph("""#.# #.# #.#""", 2),
    "#": glyph(""".#..#. .#..#. ###### .#..#. .#..#. ###### .#..#. .#..#."""),
    "$": glyph("""..#.. .#### #.#.. #.#.. #.#.. .###. ..#.# ..#.# ..#.# ####. ..#.."""),
    "%": glyph("""##....# ##...#. .....#. ....#.. ....#.. ...#... ..#.... ..#.... .#..... .#...## #....##"""),
    "&": glyph(""".##.... #..#... #..#... #.#.... .#..... #.#..#. #..#.#. #...#.. #...##. .###..#"""),
    "'": glyph("""# # #""", 2),
    "(": glyph("""..# .#. .#. #.. #.. #.. #.. #.. #.. .#. .#. ..#""", 11),
    "*": glyph("""..#.. #.#.# .###. #.#.# ..#..""", 5),
    "+": glyph("""..#.. ..#.. ##### ..#.. ..#..""", 9),
    ",": glyph(""".# .# #.""", 12),
    "-": glyph("""####""", 7),
    ".": glyph("""#"""),
    "/": glyph(""".....# .....# ....#. ....#. ...#.. ...#.. ..#... ..#... .#.... .#.... #....."""),
    ":": glyph("""# . . . . #"""),
    ";": glyph(""".# .. .. .. .. .# .# #.""", 12),
    "<": glyph("""....# ...#. .##.. #.... .##.. ...#. ....#""", 9),
    "=": glyph("""##### ..... #####""", 8),
    "?": glyph(""".####. #....# .....# .....# ....#. ...#.. ..#... ..#... ...... ...... ..#..."""),
    "@": glyph("""..####.. .#....#. #..##..# #.#..#.# #.#..#.# #.#.##.# #..#.##. #....... .#...... ..#####.""", 11),
    "[": glyph("""### #.. #.. #.. #.. #.. #.. #.. #.. #.. #.. ###""", 11),
    "^": glyph("""..#.. .#.#. #...#""", 2),
    "_": glyph("""######""", 13),
    "`": glyph("""#. .#""", 1),
    "{": glyph("""..## .#.. .#.. .#.. .#.. #... .#.. .#.. .#.. .#.. .#.. ..##""", 11),
    "|": glyph("""# # # # # # # # # # # # #""", 12),
    "~": glyph(""".##..# #..##.""", 6),
}
GLYPHS[")"] = mirror(GLYPHS["("])
GLYPHS["]"] = mirror(GLYPHS["["])
GLYPHS["}"] = mirror(GLYPHS["{"])
GLYPHS[">"] = mirror(GLYPHS["<"])
GLYPHS["\\"] = mirror(GLYPHS["/"])


def ink_pixels(char):
    """The glyph's 1-pixel ink line as (column, row) pairs, rows counted down from the cap line."""
    bottom, rows = GLYPHS[char]
    top = bottom - len(rows) + 1
    return {(x, top + y) for y, row in enumerate(rows) for x, c in enumerate(row) if c == "#"}


def advance(char):
    if char == " ":
        return SPACE_WIDTH
    return (len(GLYPHS[char][1][0]) + 2) * PIXEL  # ink width plus a pixel of spacing each side


def draw_glyph(pixels):
    """One clockwise rectangle per horizontal run of pixels."""
    pen = TTGlyphPen(None)
    for row in sorted({y for _, y in pixels}):
        columns = sorted(x for x, y in pixels if y == row)
        start = columns[0]
        for i, column in enumerate(columns):
            if i + 1 < len(columns) and columns[i + 1] == column + 1:
                continue
            left, right = start * PIXEL, (column + 1) * PIXEL
            top, bottom = (BASELINE + 1 - row) * PIXEL, (BASELINE - row) * PIXEL
            pen.moveTo((left, bottom))
            pen.lineTo((left, top))
            pen.lineTo((right, top))
            pen.lineTo((right, bottom))
            pen.closePath()
            if i + 1 < len(columns):
                start = columns[i + 1]
    return pen.glyph()


def notdef_glyph():
    pen = TTGlyphPen(None)
    for left, bottom, right, top, clockwise in [(0, 0, 6, 11, True), (1, 1, 5, 10, False)]:
        corners = [(left, bottom), (left, top), (right, top), (right, bottom)]
        for i, (x, y) in enumerate(corners if clockwise else corners[::-1]):
            (pen.moveTo if i == 0 else pen.lineTo)((x * PIXEL, y * PIXEL))
        pen.closePath()
    return pen.glyph()


def build(output=OUTPUT):
    for char, (_, rows) in GLYPHS.items():
        assert len({len(row) for row in rows}) == 1, f"rows of {char!r} differ in width"

    names = {char: f"uni{ord(char):04X}" for char in GLYPHS}
    names[" "] = "space"
    glyphs = {".notdef": notdef_glyph(), "space": TTGlyphPen(None).glyph()}
    metrics = {".notdef": (8 * PIXEL, 0), "space": (SPACE_WIDTH, 0)}
    for char in GLYPHS:
        pixels = {(x + 1, y) for x, y in ink_pixels(char)}  # one pixel of spacing on each side
        glyphs[names[char]] = draw_glyph(pixels)
        metrics[names[char]] = (advance(char), min(x for x, _ in pixels) * PIXEL)

    font = FontBuilder(UNITS_PER_EM, isTTF=True)
    font.setupGlyphOrder(list(glyphs))
    font.setupCharacterMap({ord(char): name for char, name in names.items()})
    font.setupGlyf(glyphs)
    font.setupHorizontalMetrics(metrics)
    font.setupHorizontalHeader(ascent=ASCENT, descent=-DESCENT, lineGap=LINE_GAP)
    font.setupNameTable({"familyName": "Cookieloon", "styleName": "Regular"})
    font.setupOS2(sTypoAscender=ASCENT, sTypoDescender=-DESCENT, sTypoLineGap=LINE_GAP,
                  usWinAscent=ASCENT, usWinDescent=DESCENT,
                  sxHeight=X_HEIGHT * PIXEL, sCapHeight=CAP_HEIGHT * PIXEL)
    font.setupPost()
    output.parent.mkdir(parents=True, exist_ok=True)
    font.save(str(output))
    return output


if __name__ == "__main__":
    print(f"Wrote {build()}")
