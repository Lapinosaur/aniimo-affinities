import assert from 'node:assert/strict';
import test from 'node:test';
import { validateText } from './text-encoding.mjs';

test('preserves UTF-8 accents, separators, symbols and non-Latin names', () => {
  const text = 'Dénoue · À propos — ×1.6 → 日本語 😀';
  assert.equal(validateText(Buffer.from(text), 'valid.cs'), text);
  assert.equal(validateText(Buffer.from('\uFEFF' + text), 'bom.cs'), text);
});

test('rejects invalid bytes rather than silently substituting characters', () => {
  assert.throws(() => validateText(Buffer.from([0xc3, 0x28]), 'bad.cs'), /invalid UTF-8/);
});

test('rejects already-corrupted Unicode and reports the source line', () => {
  for (const bad of ['\uFFFD', 'Cr\u00C3\u00A9ateur', '\u00C2\u00B7', '\u00E2\u20AC\u201D', '\u00F0\u0178', '\u00EF\u00BF\u00BD', '\\uFFFD']) {
    assert.throws(() => validateText(Buffer.from('first line\n' + bad), 'bad.cs'), /bad.cs:2:/);
  }
});
