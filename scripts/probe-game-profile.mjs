// Run through MO2, then inspect the physical save path from outside MO2.
import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const [myGames, localAppGame, outputFile, marker] = process.argv.slice(2);
assert.ok(myGames && localAppGame && outputFile && marker, 'Expected game settings paths, output path and marker');
try {
  const ini = await readFile(path.join(myGames, 'skyrim.ini'), 'utf8');
  const preferences = await readFile(path.join(myGames, 'skyrimprefs.ini'), 'utf8');
  const plugins = await readFile(path.join(localAppGame, 'plugins.txt'), 'utf8');
  assert.match(ini, /sLocalSavePath=Saves\\/i, 'SkyMP requires sLocalSavePath=Saves\\');
  assert.match(preferences, /bFull Screen=0/i, 'MO2 must supply the lab preferences');
  assert.match(preferences, /bFreebiesSeen=1/i, 'MO2 must supply bFreebiesSeen=1');
  const masters = [
    'Skyrim.esm', 'Update.esm', 'Dawnguard.esm', 'HearthFires.esm', 'Dragonborn.esm',
  ];
  const entries = text => text.split(/\r?\n/).map(line => line.trim()).filter(line => line && !line.startsWith('#')).map(line => line.replace(/^\*/, ''));
  // MO2 omits the game's implicitly enabled masters from plugins.txt.
  assert.ok(entries(plugins).every(name => masters.includes(name)), 'Unexpected active plugin in the lab');
  const loadOrder = await readFile(path.join(localAppGame, 'loadorder.txt'), 'utf8');
  assert.deepEqual(entries(loadOrder), masters, 'MO2 must supply the five-master lab load order');
  await writeFile(outputFile, JSON.stringify({
    settingsMapped: true, pluginsMapped: true, savePathAligned: true, marker,
    scope: 'Verify profile isolation for settings and plugins; save path aligned with SkyMP runtime',
  }, null, 2));
} catch (error) {
  await writeFile(outputFile, JSON.stringify({ marker, error: String(error) }, null, 2));
  process.exitCode = 1;
}
