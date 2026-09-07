const assert = require('node:assert/strict');
const {test} = require('node:test');
const {readFileSync} = require('node:fs');
const {join} = require('node:path');
const vm = require('node:vm');
const {EventEmitter} = require('node:events');
const ts = require('typescript');
test('authoritative snapshot waits for world/menu and corrects rejected perks without refund inflation',()=>{
  const exports={};
  const deps={ './clientListener':{ClientListener:class{}}, '../../messages':{MsgType:{CustomPacket:1}}, '../../logging':{logTrace(){},logError(){}} };
  vm.runInNewContext(ts.transpileModule(readFileSync(join(__dirname,'../src/services/services/perkSyncService.ts'),'utf8'),{
    compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2020}
  }).outputText,{exports,require:id=>{assert.ok(deps[id],id);return deps[id];}});
  const events=new EventEmitter(),emitter=new EventEmitter(),perks=new Set([0xcb413]);
  let world=false,points=99;
  const skills={};
  const player={getParentCell:()=>world?{}:null,getFormID:()=>20,hasPerk:id=>perks.has(id),removePerk:id=>perks.delete(id),addPerk:id=>perks.add(id),setActorValue:(key,value)=>skills[key]=value};
  new exports.PerkSyncService({Game:{getPlayer:()=>player,getFormEx:id=>id,setPerkPoints:value=>points=value},Perk:{from:id=>id}},{on:events.on.bind(events),emitter});
  const packet=content=>emitter.emit('customPacketMessage',{message:{contentJsonDump:JSON.stringify(content)}});
  packet({customPacketType:'syncPerks',perks:[0x5218e],perkPoints:2,skills:{Smithing:30,Alchemy:20,Enchanting:20}});
  events.emit('update');
  assert.equal(points,99);
  world=true;
  events.emit('menuOpen',{name:'StatsMenu'});
  events.emit('update');
  assert.equal(points,99);
  events.emit('menuClose',{name:'StatsMenu'});
  events.emit('update');
  assert.equal(points,2);
  assert.deepEqual([...perks],[0x5218e]);
  assert.equal(skills.Smithing,30);
  packet({customPacketType:'selectPerkRejected',perkId:0x5218e});
  assert.equal(points,2);
  assert.equal(perks.size,0);
});
