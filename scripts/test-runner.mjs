import { spawn } from 'node:child_process';
import fs from 'node:fs/promises';
import path from 'node:path';
import { performance } from 'node:perf_hooks';

export function execute(command, args, cwd, timeout = 120000) {
  return new Promise(resolve => {
    const started = performance.now();
    const child = spawn(command, args, { cwd, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
    let stdout = '', stderr = '', failure = '', timedOut = false;
    child.stdout.on('data', chunk => { stdout += chunk; });
    child.stderr.on('data', chunk => { stderr += chunk; });
    child.on('error', error => { failure = error.message; });
    const timer = setTimeout(() => { timedOut = true; child.kill(); }, timeout);
    child.on('close', (code, signal) => {
      clearTimeout(timer);
      resolve({ ok: code === 0 && !failure && !timedOut, code, signal, timedOut, stdout, stderr,
        error: failure, milliseconds: Math.round(performance.now() - started) });
    });
  });
}

export async function schedule(groups, concurrency, worker) {
  if (!Number.isInteger(concurrency) || concurrency < 1) throw new Error('Invalid worker limit.');
  const results = new Array(groups.length);
  let next = 0;
  await Promise.all(Array.from({ length: Math.min(concurrency, groups.length) }, async () => {
    while (next < groups.length) {
      const index = next++;
      try { results[index] = await worker(groups[index]); }
      catch (error) { results[index] = { ok: false, error: error.message }; }
    }
  }));
  return results;
}

export async function runTests(executable, root, output, serial = false) {
  const report = path.join(output, 'tests.txt');
  await fs.writeFile(report, 'RUNNING: test groups have not completed.\n');
  await fs.writeFile(path.join(output, 'tests.json'), JSON.stringify({ status: 'running' }));
  const plan = await execute(executable, ['--test-groups'], root);
  const groups = plan.stdout.trim().split(',');
  if (!plan.ok || groups.some(group => !/^[a-z]+$/.test(group)) || new Set(groups).size !== groups.length) {
    await fs.writeFile(report, `FAIL: cannot discover test groups\n${plan.error}\n${plan.stderr}`);
    await fs.writeFile(path.join(output, 'tests.json'), JSON.stringify({ status: 'failed', stage: 'discovery', ...plan }, null, 2));
    throw new Error('Cannot discover test groups. See dist/tests.txt.');
  }
  const logs = path.join(output, 'test-results');
  await fs.mkdir(logs, { recursive: true });
  const started = performance.now();
  const concurrency = serial ? 1 : 3;
  console.log(`Tests: ${groups.join(', ')}; up to ${concurrency} isolated processes (one UI worker).`);
  const results = await schedule(groups, concurrency, async group => {
    const groupReport = path.join(logs, `${group}.txt`);
    await fs.writeFile(groupReport, 'RUNNING\n');
    console.log(`[${group}] started`);
    const result = await execute(executable, ['--self-test', groupReport, group], root);
    const log = `${result.stdout}${result.stderr}${result.error ? `\n${result.error}` : ''}${result.timedOut ? '\nTIMEOUT after 120 seconds' : ''}`;
    // Also retain console output when killed before the C# report can be written.
    await fs.writeFile(path.join(logs, `${group}.log`), log);
    console.log(`[${group}] ${result.ok ? 'PASS' : 'FAIL'} (${result.milliseconds} ms)`);
    if (!result.ok) console.error(await fs.readFile(groupReport, 'utf8'));
    return { group, ...result };
  });
  const milliseconds = Math.round(performance.now() - started);
  const ok = results.every(result => result.ok);
  const summary = `${ok ? 'PASS' : 'FAIL'}: ${groups.length} groups in ${milliseconds} ms (${serial ? 'serial' : 'parallel'}).\n`;
  await fs.writeFile(report, summary + results.map((result, index) =>
    `\n[${groups[index]}] ${result.ok ? 'PASS' : 'FAIL'} (${result.milliseconds ?? '?'} ms)\n${result.stdout ?? ''}${result.stderr ?? ''}${result.error ?? ''}${result.timedOut ? '\nTIMEOUT' : ''}`).join(''));
  await fs.writeFile(path.join(output, 'tests.json'), JSON.stringify({ status: ok ? 'passed' : 'failed', milliseconds, concurrency,
    groups: results.map((result, index) => ({ ...result, group: groups[index] })) }, null, 2));
  console.log(summary.trim());
  if (!ok) throw new Error('Regression tests failed. See dist/tests.txt and dist/test-results/.');
}
