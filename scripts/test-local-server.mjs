import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';

const sourceLock = JSON.parse(await readFile(new URL('../sources.lock.json', import.meta.url), 'utf8'));
const serverUrl = new URL(`../${sourceLock.serverArtifact.runtimeDirectory}/`, import.meta.url);
const serverRoot = fileURLToPath(serverUrl);
const settings = JSON.parse(await readFile(new URL('server-settings.json', serverUrl), 'utf8'));
const uiPort = settings.port === 7777 ? 3000 : settings.port + 1;
const child = spawn(process.execPath, ['dist_back/skymp5-server.js'], {
  cwd: serverRoot,
  windowsHide: true,
  stdio: ['ignore', 'pipe', 'pipe'],
});

let output = '';
let spawnError;
child.on('error', error => { spawnError = error; });
child.stdout.on('data', data => { output += data.toString(); });
child.stderr.on('data', data => { output += data.toString(); });
const closed = once(child, 'close');
closed.catch(() => {});

try {
  const deadline = Date.now() + 30000;
  let manifest;
  while (Date.now() < deadline) {
    if (spawnError) throw spawnError;
    if (child.exitCode !== null || child.signalCode !== null) {
      throw new Error(`Server exited before readiness: ${child.exitCode ?? child.signalCode}`);
    }
    if (output.includes('[SKYMP_LAB_READY]')) {
      try {
        const response = await fetch(`http://127.0.0.1:${uiPort}/manifest.json`, {
          signal: AbortSignal.timeout(1000),
        });
        if (response.ok) {
          manifest = await response.json();
          break;
        }
      } catch {}
    }
    await delay(250);
  }
  assert.ok(manifest, 'Native server and HTTP manifest did not become ready');
  assert.equal(manifest.versionMajor, 1);
  assert.deepEqual(manifest.mods.map(mod => mod.filename), [
    'Skyrim.esm', 'Update.esm', 'Dawnguard.esm', 'HearthFires.esm', 'Dragonborn.esm',
  ]);
  assert.ok(manifest.mods.every(mod => mod.size > 0 && Number.isInteger(mod.crc32)));
  console.log(JSON.stringify({
    result: 'passed',
    node: process.version,
    nativeGamemodeReady: true,
    manifestMods: manifest.mods.length,
    uiPort,
    gamePort: settings.port,
    scope: 'Server startup and local game-data manifest; no game-client connection tested',
  }, null, 2));
} catch (error) {
  console.error(output.slice(-12000));
  throw error;
} finally {
  if (child.exitCode === null && child.signalCode === null) child.kill();
  await closed.catch(() => {});
  await writeFile(new URL('../smoke-test.log', serverUrl), output, 'utf8');
}
