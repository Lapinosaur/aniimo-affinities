import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const output = path.join(root, 'dist');
export async function filesUnder(directory, extension = '.cs') {
  const entries = await fs.readdir(directory, { withFileTypes: true });
  const groups = await Promise.all(entries.map(entry => {
    const file = path.join(directory, entry.name);
    return entry.isDirectory() ? filesUnder(file, extension) : entry.name.endsWith(extension) ? [file] : [];
  }));
  return groups.flat().sort();
}
export async function resources() {
  const data = await filesUnder(path.join(root, 'data'), '.txt');
  const icons = await filesUnder(path.join(root, 'assets/types'), '.svg');
  const branding = ['app.ico', 'buy-me-a-coffee.png', 'support-qr.png'].map(name => path.join(root, 'assets/branding', name));
  const resources = [...data, ...icons, ...branding, path.join(root, 'docs/legal.md')];
  const names = resources.map(file => path.basename(file));
  if (new Set(names).size !== names.length) throw new Error('Embedded resource names must be unique.');
  return resources.map(file => `/resource:${file},${path.basename(file)}`);
}
