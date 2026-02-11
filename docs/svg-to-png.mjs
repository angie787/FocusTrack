import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
const __dirname = path.dirname(fileURLToPath(import.meta.url));

const { Resvg } = await import('@resvg/resvg-js');
const svgPath = path.join(__dirname, 'architecture.svg');
const pngPath = path.join(__dirname, 'architecture.png');
let svg = fs.readFileSync(svgPath, 'utf8');
// Remove invalid XML control characters (e.g. U+0013) that break resvg
svg = svg.replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, '');
const resvg = new Resvg(svg);
const pngData = resvg.render();
const pngBuffer = pngData.asPng();
fs.writeFileSync(pngPath, pngBuffer);
console.log('Written:', pngPath);
