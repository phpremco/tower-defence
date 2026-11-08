"""Redraws K and X in the Kenney Future fonts (CC0) so they no longer read as H.

The shipped fonts draw K as an H with a small notch on the right and X as an H with notches on both
sides, so "SKITTER" reads "SHITTER" and "CODEX" reads "CODEH" at UI sizes. This script replaces the
outlines of K, k, X and x with diagonal shapes in the font's own style (128-unit stems, 45-degree
strokes like its V, square ends) and rewrites the TTF with fresh checksums. Running it twice gives
the same file. Usage: python -I Tools/Fonts/patch_kenney_glyphs.py Assets/_Project/Art/UI/Fonts
"""
import os
import struct
import sys

# Outlines are single clockwise contours of on-curve points, in font units (1024 per em, cap height 640).
WIDE = {
    'K': [(0, 0), (0, 640), (128, 640), (128, 384), (192, 384), (448, 640), (640, 640), (320, 320),
          (640, 0), (448, 0), (192, 256), (128, 256), (128, 0)],
    'X': [(0, 0), (0, 96), (224, 320), (0, 544), (0, 640), (96, 640), (320, 416), (544, 640), (640, 640),
          (640, 544), (416, 320), (640, 96), (640, 0), (544, 0), (320, 224), (96, 0)],
}
NARROW = {
    'K': [(0, 0), (0, 640), (128, 640), (128, 384), (176, 384), (368, 640), (512, 640), (272, 320),
          (512, 0), (368, 0), (176, 256), (128, 256), (128, 0)],
    'X': [(0, 0), (0, 96), (179, 320), (0, 544), (0, 640), (77, 640), (256, 416), (435, 640), (512, 640),
          (512, 544), (333, 320), (512, 96), (512, 0), (435, 0), (256, 224), (77, 0)],
}
FONTS = {'KenneyFuture.ttf': WIDE, 'KenneyFutureNarrow.ttf': NARROW}


def read_tables(data):
    count = struct.unpack('>H', data[4:6])[0]
    tables = {}
    order = []
    for i in range(count):
        tag, _, offset, length = struct.unpack('>4sIII', data[12 + 16 * i:28 + 16 * i])
        tables[tag] = bytearray(data[offset:offset + length])
        order.append(tag)
    return tables, order


def glyph_for(tables, char):
    cmap = tables[b'cmap']
    count = struct.unpack('>H', cmap[2:4])[0]
    for i in range(count):
        sub = struct.unpack('>I', cmap[8 + 8 * i:12 + 8 * i])[0]
        if struct.unpack('>H', cmap[sub:sub + 2])[0] != 4:
            continue
        seg = struct.unpack('>H', cmap[sub + 6:sub + 8])[0] // 2
        ends = struct.unpack('>%dH' % seg, cmap[sub + 14:sub + 14 + 2 * seg])
        starts = struct.unpack('>%dH' % seg, cmap[sub + 16 + 2 * seg:sub + 16 + 4 * seg])
        deltas = struct.unpack('>%dh' % seg, cmap[sub + 16 + 4 * seg:sub + 16 + 6 * seg])
        range_pos = sub + 16 + 6 * seg
        ranges = struct.unpack('>%dH' % seg, cmap[range_pos:range_pos + 2 * seg])
        code = ord(char)
        for s in range(seg):
            if starts[s] <= code <= ends[s]:
                if ranges[s] == 0:
                    return (code + deltas[s]) & 0xFFFF
                address = range_pos + 2 * s + ranges[s] + 2 * (code - starts[s])
                glyph = struct.unpack('>H', cmap[address:address + 2])[0]
                return (glyph + deltas[s]) & 0xFFFF if glyph else 0
    raise ValueError('no glyph for ' + char)


def encode_glyph(points):
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    out = struct.pack('>hhhhh', 1, min(xs), min(ys), max(xs), max(ys))
    out += struct.pack('>HH', len(points) - 1, 0)
    out += bytes([1] * len(points))
    previous = 0
    for x in xs:
        out += struct.pack('>h', x - previous)
        previous = x
    previous = 0
    for y in ys:
        out += struct.pack('>h', y - previous)
        previous = y
    return out


def checksum(blob):
    padded = bytes(blob) + b'\0' * (-len(blob) % 4)
    return sum(struct.unpack('>%dI' % (len(padded) // 4), padded)) & 0xFFFFFFFF


def patch(path, shapes):
    data = open(path, 'rb').read()
    tables, order = read_tables(data)
    head = tables[b'head']
    long_loca = struct.unpack('>h', head[50:52])[0] == 1
    glyph_count = struct.unpack('>H', tables[b'maxp'][4:6])[0]
    loca = tables[b'loca']
    if long_loca:
        offsets = list(struct.unpack('>%dI' % (glyph_count + 1), loca[:4 * (glyph_count + 1)]))
    else:
        offsets = [o * 2 for o in struct.unpack('>%dH' % (glyph_count + 1), loca[:2 * (glyph_count + 1)])]
    glyf = tables[b'glyf']
    glyphs = [bytes(glyf[offsets[i]:offsets[i + 1]]) for i in range(glyph_count)]

    for char, points in shapes.items():
        for variant in (char, char.lower()):
            glyphs[glyph_for(tables, variant)] = encode_glyph(points)

    new_glyf = bytearray()
    new_offsets = []
    for glyph in glyphs:
        new_offsets.append(len(new_glyf))
        new_glyf += glyph + b'\0' * (-len(glyph) % 4)
    new_offsets.append(len(new_glyf))
    tables[b'glyf'] = new_glyf
    if long_loca:
        tables[b'loca'] = bytearray(struct.pack('>%dI' % len(new_offsets), *new_offsets))
    else:
        tables[b'loca'] = bytearray(struct.pack('>%dH' % len(new_offsets), *[o // 2 for o in new_offsets]))
    head[8:12] = b'\0\0\0\0'

    write(path, data[:12], tables, order)


def write(path, header, tables, order):
    directory_size = 12 + 16 * len(order)
    offset = directory_size
    entries = []
    body = bytearray()
    for tag in sorted(order):
        blob = tables[tag]
        entries.append((tag, checksum(blob), offset, len(blob)))
        padded = bytes(blob) + b'\0' * (-len(blob) % 4)
        body += padded
        offset += len(padded)
    directory = bytearray(header)
    for entry in entries:
        directory += struct.pack('>4sIII', *entry)
    font = directory + body
    adjustment = (0xB1B0AFBA - checksum(font)) & 0xFFFFFFFF
    head_offset = next(e[2] for e in entries if e[0] == b'head')
    font[head_offset + 8:head_offset + 12] = struct.pack('>I', adjustment)
    open(path, 'wb').write(bytes(font))


def main():
    folder = sys.argv[1]
    for name, shapes in FONTS.items():
        patch(os.path.join(folder, name), shapes)
        print('patched', name)


if __name__ == '__main__':
    main()
