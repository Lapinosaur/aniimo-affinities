import test from 'node:test';
import assert from 'node:assert/strict';
import { schedule, execute } from './test-runner.mjs';

test('worker cap, deterministic results and continued execution after failure', async () => {
  let active = 0, peak = 0;
  const visited = [];
  const results = await schedule(['storage', 'ui', 'model', 'extra'], 2, async group => {
    active++; peak = Math.max(peak, active); visited.push(group);
    await new Promise(resolve => setTimeout(resolve, 10));
    active--;
    if (group === 'ui') throw new Error('fixture failure');
    return { ok: true, group };
  });
  assert.equal(peak, 2);
  assert.equal(visited.length, 4);
  assert.deepEqual(results.map(result => result.ok), [true, false, true, true]);
  assert.equal(results[1].error, 'fixture failure');
  assert.equal(results[2].group, 'model');
});

test('serial mode never overlaps workers', async () => {
  let active = false;
  await schedule(['one', 'two'], 1, async () => {
    assert.equal(active, false); active = true;
    await new Promise(resolve => setTimeout(resolve, 5));
    active = false;
    return { ok: true };
  });
});

test('process failures and timeouts retain output and fail', async () => {
  const failure = await execute(process.execPath, ['-e', "console.log('fixture'); process.exit(7)"], process.cwd());
  assert.equal(failure.ok, false);
  assert.equal(failure.code, 7);
  assert.match(failure.stdout, /fixture/);
  const timeout = await execute(process.execPath, ['-e', 'setInterval(() => {}, 1000)'], process.cwd(), 100);
  assert.equal(timeout.ok, false);
  assert.equal(timeout.timedOut, true);
  const missing = await execute('aniimo-missing-test-executable', [], process.cwd());
  assert.equal(missing.ok, false);
  assert.ok(missing.error);
});
