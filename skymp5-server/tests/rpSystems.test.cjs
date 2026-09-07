const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const vm = require('node:vm');
const { EventEmitter } = require('node:events');
const ts = require('typescript');
function load(file, dependencies = {}) {
  const exports = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname, file), 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 }
  }).outputText, { exports, require: id => dependencies[id] ?? require(id), process, console });
  return exports;
}
const rules = load('../../shared/rp/perkRules.ts');
const { PerkSystem } = load('../ts/systems/perkSystem.ts', { '../../../shared/rp/perkRules': rules });
const { Spawn } = load('../ts/systems/spawn.ts', { '../settings': { Settings: { get: async () => ({ startPoints: [{pos:[0,0,0],angleZ:0,worldOrCell:1}] }) } } });
test('perk ownership, validation, persistence and authenticated profile', async t => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'skymptr-perks-'));
  t.after(() => fs.rmSync(dir, { recursive:true, force:true }));
  const system = new PerkSystem(() => {});
  system.playersDir = dir;
  const packets = [];
  const ctx = {gm:new EventEmitter(),svr:{sendCustomPacket:(user,json)=>packets.push(JSON.parse(json))}};
  await system.initAsync(ctx);
  ctx.gm.emit('spawnAllowed',7,42);
  assert.equal(packets.at(-1).perkPoints,3);
  const select = perkId => system.customPacket(7,'requestSelectPerk',{perkId,profileId:999},ctx);
  for (const id of [0xcb40f,0xdeadbeef,'5218e',null,1.5]) {
    select(id);
    assert.equal(packets.at(-2).customPacketType,'selectPerkRejected');
    assert.equal(packets.at(-1).perkPoints,3);
  }
  for (const id of [0x5218e,0xcb40f,0xbe127]) select(id);
  assert.equal(packets.at(-1).perkPoints,0);
  select(0xbe127);
  assert.equal(packets.at(-1).customPacketType,'syncPerks');
  assert.equal(packets.at(-1).perkPoints,0);
  assert.equal(packets.at(-1).perks.length,3);
  assert.equal(fs.existsSync(path.join(dir,'999.json')),false);
  const saved=JSON.parse(fs.readFileSync(path.join(dir,'42.json')));
  assert.deepEqual(saved.perks,[0x5218e,0xcb40f,0xbe127]);
  system.disconnect(7,ctx);
  const count=packets.length;
  select(0xbee97);
  assert.equal(packets.length,count);
  ctx.gm.emit('spawnAllowed',8,42);
  assert.deepEqual(packets.at(-1).perks,saved.perks);
  fs.writeFileSync(path.join(dir,'43.json'),'{corrupt');
  assert.throws(()=>ctx.gm.emit('spawnAllowed',9,43),/preserved/);
  assert.equal(fs.readFileSync(path.join(dir,'43.json'),'utf8'),'{corrupt');
});
test('skill level gate',()=>{
  assert.match(rules.validatePerkSelection({perkPoints:3,perks:[],skills:{Smithing:10}},0x5218e),/seviyesi/);
});
for (const [label,existing,appearance,want] of [['new',false,null,true],['unfinished',true,null,true],['finished',true,{},false]]) {
  test(`character creation: ${label}, menu state precedes actor assignment`,async()=>{
    const calls=[];
    const ctx={gm:new EventEmitter(),svr:{getActorsByProfileId:()=>existing?[12]:[],createActor:()=>12,get:()=>appearance,
      setRaceMenuOpen:(id,open)=>calls.push(['menu',open]),setUserActor:()=>calls.push(['assign']),setEnabled(){},set(){}}};
    await new Spawn(()=>{}).initAsync(ctx);
    ctx.gm.emit('spawnAllowed',7,42,[]);
    assert.deepEqual(calls,[['menu',want],['assign']]);
  });
}
