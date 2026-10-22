import fs from 'node:fs/promises';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { root, output, filesUnder, resources } from './project.mjs';

export function run(command, args, options = {}) {
  const result = spawnSync(command, args, { cwd: root, encoding: 'utf8', timeout: 120000, ...options });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${path.basename(command)} failed (exit ${result.status}).`);
}
export async function build(diagnostics = false) {
  if (process.platform !== 'win32') throw new Error('Building requires Windows and .NET Framework 4.x.');
  const compiler = path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
  await fs.access(compiler);
  const folder = diagnostics ? path.join(output, 'diagnostics') : output;
  await fs.mkdir(folder, { recursive: true });
  const executable = path.join(folder, diagnostics ? 'AniimoAffinities.Diagnostics.exe' : 'AniimoAffinities.exe');
  const sources = await filesUnder(path.join(root, 'src'));
  if (diagnostics) sources.push(...await filesUnder(path.join(root, 'tests')), ...await filesUnder(path.join(root, 'tools/Diagnostics')));
  run(compiler, [
    '/nologo', diagnostics ? '/target:exe' : '/target:winexe', '/platform:x64', '/optimize+', '/codepage:65001',
    '/warn:4', '/warnaserror+', `/main:AniimoAffinities.${diagnostics ? 'DiagnosticsProgram' : 'Program'}`,
    `/win32icon:${path.join(root, 'assets/branding/app.ico')}`, `/out:${executable}`,
    ...['System.Windows.Forms', 'System.Drawing', 'System.Core', 'System.Xml', 'System.Web.Extensions'].map(name => `/reference:${name}.dll`),
    ...await resources(), ...sources,
  ]);
  console.log(`Built ${path.relative(root, executable)}`);
  return executable;
}
export async function test() {
  await build();
  const executable = await build(true);
  const report = path.join(output, 'tests.txt');
  await fs.rm(report, { force: true });
  run(executable, ['--self-test', report]);
  console.log(await fs.readFile(report, 'utf8'));
}
