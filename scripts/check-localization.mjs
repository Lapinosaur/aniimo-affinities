import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import { root, filesUnder } from './project.mjs';

const source = JSON.parse(await fs.readFile(path.join(root, 'data/locale-en.txt'), 'utf8'));
const languages = await fs.readFile(path.join(root, 'src/Data/Language.cs'), 'utf8');
const codes = [...languages.matchAll(/new Choice\("([a-z]+)"/g)].map(match => match[1]);
for (const code of codes) {
  const catalog = JSON.parse(await fs.readFile(path.join(root, `data/locale-${code}.txt`), 'utf8'));
  for (const key of Object.keys(source)) assert(typeof catalog[key] === 'string' && catalog[key].trim(), `${code}: missing ${key}`);
}
for (const file of await filesUnder(path.join(root, 'src'))) {
  const code = await fs.readFile(file, 'utf8');
  for (const match of code.matchAll(/Language\.T\(("(?:\\.|[^"\\])*")\)/g)) {
    const key = JSON.parse(match[1]);
    assert(Object.hasOwn(source, key), `${path.relative(root,file)}: missing catalog source ${key}`);
  }
}
console.log(`Localization OK: ${codes.length} languages, ${Object.keys(source).length} source strings.`);
