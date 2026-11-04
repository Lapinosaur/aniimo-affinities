import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { gunzipSync } from 'node:zlib';
import { test } from 'node:test';
import { packLocales } from './pack-locales.mjs';
import { root } from './project.mjs';

test('packed catalogs preserve every source byte and rebuild deterministically', async () => {
  const directory = await fs.mkdtemp(path.join(os.tmpdir(), 'affinisaur-locales-'));
  try {
    const files = await packLocales(directory);
    const originals = new Map();
    let rawSize = 0, packedSize = 0;
    for (const file of files) {
      const packed = await fs.readFile(file);
      const raw = await fs.readFile(path.join(root, 'data', path.basename(file, '.gz')));
      assert.deepEqual(gunzipSync(packed), raw);
      originals.set(file, packed);
      rawSize += raw.length;
      packedSize += packed.length;
    }
    assert.ok(files.length > 0 && packedSize < rawSize / 2);
    assert.deepEqual(await packLocales(directory), files);
    for (const file of files) assert.deepEqual(await fs.readFile(file), originals.get(file));
  } finally {
    await fs.rm(directory, { recursive: true, force: true });
  }
});
