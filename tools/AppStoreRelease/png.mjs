// A PNG helper for screenshots.mjs; Node's own zlib is all it needs.

import { crc32, deflateSync, inflateSync } from 'node:zlib';

/**
 * The same image without its alpha channel, which App Store Connect does not want and simctl
 * always writes. Handles what simctl writes: 8 bits per channel, not interlaced.
 */
export function withoutAlpha(png) {
  const [width, height, depth, color, interlace] = [png.readUInt32BE(16), png.readUInt32BE(20), png[24], png[25], png[28]];
  if (color !== 6) return png;
  if (depth !== 8 || interlace !== 0) throw new Error('an RGBA image of an unexpected kind');

  const data = [];
  const others = [];
  for (let at = 8; at < png.length;) {
    const length = png.readUInt32BE(at);
    const type = png.toString('latin1', at + 4, at + 8);
    if (type === 'IDAT') data.push(png.subarray(at + 8, at + 8 + length));
    else if (type !== 'IHDR' && type !== 'IEND') others.push(png.subarray(at, at + 12 + length));
    at += 12 + length;
  }

  // Undo each line's filter (PNG specification, section 9), and write it back without alpha,
  // filtered as "up", which suits flat pictures of a screen.
  const raw = inflateSync(Buffer.concat(data));
  const stride = width * 4;
  const out = Buffer.alloc((width * 3 + 1) * height);
  let above = Buffer.alloc(stride);
  let aboveRgb = Buffer.alloc(width * 3);
  for (let y = 0; y < height; y++) {
    const start = y * (stride + 1);
    const filter = raw[start];
    const line = Buffer.from(raw.subarray(start + 1, start + 1 + stride));
    for (let x = 0; x < stride; x++) {
      const a = x >= 4 ? line[x - 4] : 0;
      const b = above[x];
      const c = x >= 4 ? above[x - 4] : 0;
      let predicted = 0;
      if (filter === 1) predicted = a;
      else if (filter === 2) predicted = b;
      else if (filter === 3) predicted = (a + b) >> 1;
      else if (filter === 4) {
        const p = a + b - c;
        const [pa, pb, pc] = [Math.abs(p - a), Math.abs(p - b), Math.abs(p - c)];
        predicted = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
      }
      line[x] = (line[x] + predicted) & 255;
      if (x % 4 === 3 && line[x] !== 255) throw new Error('an image with transparent pixels');
    }
    const rgb = Buffer.alloc(width * 3);
    for (let x = 0; x < width; x++) line.copy(rgb, x * 3, x * 4, x * 4 + 3);
    const target = y * (width * 3 + 1);
    out[target] = 2;
    for (let x = 0; x < rgb.length; x++) out[target + 1 + x] = (rgb[x] - aboveRgb[x]) & 255;
    above = line;
    aboveRgb = rgb;
  }

  const chunk = (type, body) => {
    const head = Buffer.alloc(8);
    head.writeUInt32BE(body.length, 0);
    head.write(type, 4, 'latin1');
    const tail = Buffer.alloc(4);
    tail.writeUInt32BE(crc32(Buffer.concat([head.subarray(4), body])), 0);
    return Buffer.concat([head, body, tail]);
  };
  const header = Buffer.from(png.subarray(16, 29));
  header[9] = 2;
  return Buffer.concat([png.subarray(0, 8), chunk('IHDR', header), ...others, chunk('IDAT', deflateSync(out, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}
