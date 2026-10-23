import fs from 'node:fs/promises';
import path from 'node:path';
import { runTests } from './test-runner.mjs';
import { spawnSync } from 'node:child_process';
import { root, output, filesUnder, resources } from './project.mjs';
import { checkTextEncoding } from './text-encoding.mjs';

export function run(command, args, options = {}) {
  const result = spawnSync(command, args, { cwd: root, encoding: 'utf8', timeout: 120000, ...options });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${path.basename(command)} failed (exit ${result.status}).`);
}
export async function build(diagnostics = false) {
  await checkTextEncoding();
  if (process.platform !== 'win32') throw new Error('Building requires Windows and .NET Framework 4.x.');
  const compiler = path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
  await fs.access(compiler);
  const folder = diagnostics ? path.join(output, 'diagnostics') : output;
  await fs.mkdir(folder, { recursive: true });
  const executable = path.join(folder, diagnostics ? 'AniimoAffinities.Diagnostics.exe' : 'Affinisaur.exe');
  const metadata = JSON.parse(await fs.readFile(path.join(root, 'package.json'), 'utf8'));
  const versionSource = path.join(folder, 'Version.g.cs');
  await fs.writeFile(versionSource, `[assembly: System.Reflection.AssemblyTitle("Affinisaur")]\n[assembly: System.Reflection.AssemblyProduct("Affinisaur")]\n[assembly: System.Reflection.AssemblyCompany("Lapinosaur")]\n[assembly: System.Reflection.AssemblyDescription("Unofficial Aniimo companion by Lapinosaur.")]\n[assembly: System.Reflection.AssemblyVersion("${metadata.version}.0")]\n[assembly: System.Reflection.AssemblyFileVersion("${metadata.version}.0")]\n`);
  const sources = [...await filesUnder(path.join(root, 'src')), versionSource];
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
export async function test(serial = false) {
  await build();
  const executable = await build(true);
  run(process.execPath, ['--test', 'scripts/test-runner.test.mjs', 'scripts/text-encoding.test.mjs']);
  await runTests(executable, root, output, serial);
}
