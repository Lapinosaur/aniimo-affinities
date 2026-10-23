import fs from 'node:fs/promises';
import path from 'node:path';
import { root, filesUnder } from './project.mjs';

// Inspect shipped text only; never rewrite user data or strip legitimate accents.
export function validateText(bytes, name) {
  let text;
  try {
    text = new TextDecoder('utf-8', { fatal: true }).decode(bytes);
  } catch {
    throw new Error(`${name}: invalid UTF-8 encoding.`);
  }
  const broken = /\uFFFD|[\u0080-\u009F]|\u00C3[\u0080-\u00BF]|\u00C2[\u0080-\u00BF]|\u00E2[\u0080-\u00BF\u20AC]|\u00F0\u0178|\u00EF\u00BF\u00BD|\\u[fF]{3}[dD]/u.exec(text);
  if (broken) {
    const line = text.slice(0, broken.index).split('\n').length;
    throw new Error(`${name}:${line}: replacement character or likely mojibake; restore the original Unicode text.`);
  }
  return text;
}

export async function checkTextEncoding() {
  const groups = await Promise.all(['src', 'assets', 'data'].map(folder => filesUnder(path.join(root, folder), '')));
  const files = groups.flat().filter(file => /\.(cs|svg|txt|md|json|resx)$/i.test(file));
  files.push(path.join(root, 'docs/legal.md'));
  for (const file of files) validateText(await fs.readFile(file), path.relative(root, file));
  console.log(`Text encoding OK: ${files.length} source/resource files checked.`);
}
