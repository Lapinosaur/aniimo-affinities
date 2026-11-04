import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { gzipSync } from 'node:zlib';
import { root } from './project.mjs';

export async function packLocales(destination) {
  await fs.mkdir(destination, { recursive: true });
  const names = (await fs.readdir(path.join(root, 'data'))).filter(name => /^locale-[a-z]+\.txt$/.test(name)).sort();
  const files = [];
  for (const name of names) {
    const compressed = gzipSync(await fs.readFile(path.join(root, 'data', name)), { level: 9 });
    const target = path.join(destination, `${name}.gz`);
    let previous;
    try { previous = await fs.readFile(target); }
    catch (error) { if (error.code !== 'ENOENT') throw error; }
    if (!previous || !previous.equals(compressed)) await fs.writeFile(target, compressed);
    files.push(target);
  }
  return files;
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  if (!process.argv[2]) throw new Error('Usage: node scripts/pack-locales.mjs <destination>');
  await packLocales(path.resolve(process.argv[2]));
}
