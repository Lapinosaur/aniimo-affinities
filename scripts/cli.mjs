import path from 'node:path';
import fs from 'node:fs/promises';
import { build, test, run } from './build.mjs';
import { root, output, filesUnder } from './project.mjs';

const [command = 'check', ...args] = process.argv.slice(2);
try {
  switch (command) {
    case 'build': await build(); break;
    case 'test': await test(); break;
    case 'check':
      await import('./check-architecture.mjs');
      await test();
      break;
    case 'profile:resources': {
      const executable = await build(true);
      const destination = path.join(output, 'resources.txt');
      run(executable, ['--profile-resources', destination]);
      console.log(await fs.readFile(destination, 'utf8'));
      break;
    }
    case 'preview': {
      const variant = args[0] || 'main';
      if (!/^[a-z-]+$/.test(variant)) throw new Error('Preview variant must use lowercase letters and hyphens.');
      const executable = await build(true);
      const destination = path.resolve(args[1] || path.join(output, 'previews', `${variant}.png`));
      await fs.mkdir(path.dirname(destination), { recursive: true });
      run(executable, [variant === 'main' ? '--preview' : `--preview-${variant}`, destination]);
      console.log(destination);
      break;
    }
    case 'format':
    case 'format:check': {
      const files = (await Promise.all(['src', 'tests', 'tools/Diagnostics'].map(folder => filesUnder(path.join(root, folder))))).flat().map(file => path.relative(root, file));
      run('dotnet', ['format', 'whitespace', '--folder', root, '--include', ...files, ...(command === 'format:check' ? ['--verify-no-changes'] : [])]);
      break;
    }
    default: throw new Error('Commands: build, test, check, preview [variant] [file], format, format:check.');
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
