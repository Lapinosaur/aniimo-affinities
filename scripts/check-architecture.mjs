import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { root, filesUnder, resources } from './project.mjs';

const sources = await filesUnder(path.join(root, 'src'));
assert(sources.length > 0, 'src/ must contain the runtime sources.');
for (const file of sources) {
  const relative = path.relative(root, file).replaceAll('\\', '/');
  const source = await fs.readFile(file, 'utf8');
  assert(source.split(/\r?\n/).length <= 600, `${relative}: split responsibilities before exceeding 600 lines.`);
  assert(!/RegressionTests|DiagnosticsProgram|--preview|--self-test/.test(source), `${relative}: developer tooling belongs outside src/.`);
  assert(!/class Chrome\b/.test(source), `${relative}: use the focused theme/rendering/display owners.`);
  if (relative.startsWith('src/Domain/') || relative.startsWith('src/Infrastructure/Preferences/')) {
    assert(!/System\.Windows\.Forms|MainForm|PortableWindow|Theme\.|Drawing\./.test(source), `${relative}: core state and persistence must not depend on UI.`);
  }
  if (relative.startsWith('src/UI/Rendering/')) {
    assert(!/MainForm|MatrixDialog|RadarDialog|SettingsDialog/.test(source), `${relative}: renderers must not depend on view windows.`);
  }
  if (!relative.endsWith('/PreferenceFiles.cs')) {
    assert(!/File\.(WriteAllText|WriteAllLines|Replace|Move)\(/.test(source), `${relative}: preference writes must use PreferenceFiles.`);
  }
}
assert(!(await fs.readdir(root)).some(file => file.endsWith('.cs')), 'Runtime C# sources belong under src/.');
await resources();
for (const file of ['types.txt', 'matchups.txt', 'matrix-emphasis.txt']) await fs.access(path.join(root, 'data', file));
console.log(`Architecture OK: ${sources.length} runtime files; runtime/tooling boundaries and resources checked.`);
