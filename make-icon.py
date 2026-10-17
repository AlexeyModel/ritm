"""Write a small pine tray icon as ritm.ico (PNG-in-ICO, Windows Vista+)."""
import struct
import zlib
from pathlib import Path


def chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def png(size: int) -> bytes:
    raw = bytearray()
    cx = cy = (size - 1) / 2
    radius = size * 0.46
    for y in range(size):
        raw.append(0)
        for x in range(size):
            dx, dy = x - cx, y - cy
            if dx * dx + dy * dy > radius * radius:
                raw.extend((0, 0, 0, 0))
                continue
            bar = max(1, round(size * 0.12))
            gap = size * 0.06
            top = cy - size * 0.22
            bot = cy + size * 0.22
            left_a = cx - gap - bar
            left_b = cx + gap
            on_bar = top <= y <= bot and (
                left_a <= x < left_a + bar or left_b <= x < left_b + bar
            )
            raw.extend((251, 249, 244, 255) if on_bar else (47, 107, 79, 255))
    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")


def ico(images: list[tuple[int, bytes]]) -> bytes:
    header = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b""
    blob = b""
    for size, data in images:
        dim = 0 if size >= 256 else size
        entries += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
        blob += data
    return header + entries + blob


out = Path(__file__).with_name("Ritm.App") / "ritm.ico"
images = [(size, png(size)) for size in (16, 32, 48, 256)]
out.write_bytes(ico(images))
print(out, out.stat().st_size)
