import path from 'node:path';
import { run } from './build.mjs';
import { root, filesUnder } from './project.mjs';

// SDK analyzers are development-only. The distributed executable still uses Framework csc.
run('dotnet', ['build', 'AniimoAffinities.csproj', '--configuration', 'Release', '--no-incremental', '--nologo']);
const files = [...await filesUnder(path.join(root, 'scripts'), '.mjs')];
for (const file of files) run(process.execPath, ['--check', file]);
console.log('Lint OK: C# correctness/security analyzers and JavaScript syntax.');
