const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { test } = require("node:test");
const vm = require("node:vm");
const ts = require("typescript");

// Execute the real service with only the Skyrim/native boundary replaced.
const source = readFileSync(join(__dirname, "../src/services/services/settingsService.ts"), "utf8");
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
}).outputText;

function makeService(settings, responder) {
  const requests = [];
  const waits = [];
  const clients = [];
  class HttpClient {
    constructor(baseUrl) { this.baseUrl = baseUrl; clients.push(baseUrl); }
    async get(path) {
      const request = { baseUrl: this.baseUrl, path };
      requests.push(request);
      return responder(request, requests.length);
    }
  }
  const dependencies = {
    "skyrimPlatform": { HttpClient, printConsole() {}, Utility: { async wait(seconds) { waits.push(seconds); } } },
    "./authService": { AuthService: class {} },
    "./clientListener": { ClientListener: class {} },
    "./timersService": { TimersService: class {} },
    "../../logging": { logTrace() {} },
  };
  const exports = {};
  vm.runInNewContext(compiled, {
    exports,
    require(id) {
      assert.ok(id in dependencies, `Unexpected dependency: ${id}`);
      return dependencies[id];
    },
  });
  const service = new exports.SettingsService({ settings: { "skymp5-client": settings } }, {});
  return { service, requests, waits, clients };
}

const mods = [{ filename: "Skyrim.esm", size: 123, crc32: 456 }];
const success = () => ({ status: 200, body: JSON.stringify({ versionMajor: 1, mods }) });

test("local HTTP configuration fetches the server directly without a gateway client", async () => {
  const { service, requests, clients } = makeService({
    "server-http-url": "http://127.0.0.1:3000/", "server-ip": "127.0.0.1", "server-port": 7777,
  }, success);
  assert.equal(JSON.stringify(await service.getServerMods()), JSON.stringify(mods));
  assert.deepEqual(requests, [{ baseUrl: "http://127.0.0.1:3000", path: "/manifest.json" }]);
  assert.deepEqual(clients, ["http://127.0.0.1:3000"]);
});

test("existing online configuration retains its master and server key", async () => {
  const { service, requests } = makeService({ master: "https://example.test/", "server-master-key": "rp-server" }, success);
  await service.getServerMods();
  assert.deepEqual(requests, [{ baseUrl: "https://example.test", path: "/api/servers/rp-server/manifest.json" }]);
});

test("legacy default gateway still resolves the host and port as its key", async () => {
  const { service, requests } = makeService({ "server-ip": "10.0.0.5", "server-port": 7777 }, success);
  await service.getServerMods();
  assert.deepEqual(requests, [{ baseUrl: "https://gateway.skymp.net", path: "/api/servers/10.0.0.5:7777/manifest.json" }]);
});

test("transient request errors retry and recover", async () => {
  const { service, requests, waits } = makeService({ "server-http-url": "http://localhost:3000" }, (_, attempt) => {
    if (attempt < 3) throw new Error("connection refused");
    return success();
  });
  assert.equal(JSON.stringify(await service.getServerMods()), JSON.stringify(mods));
  assert.equal(requests.length, 3);
  assert.equal(waits.length, 2);
});

test("unavailable local server rejects instead of pretending to have no mods", async () => {
  const { service, requests, waits, clients } = makeService({ "server-http-url": "http://localhost:3000" }, () => ({ status: 503 }));
  await assert.rejects(service.getServerMods(), /Could not verify server mod list/);
  assert.equal(requests.length, 5);
  assert.equal(waits.length, 4);
  assert.deepEqual(clients, ["http://localhost:3000"]);
});

for (const body of ['{', JSON.stringify({ versionMajor: 2, mods }), JSON.stringify({ versionMajor: 1 })]) {
  test(`invalid manifest is rejected: ${body}`, async () => {
    const { service } = makeService({ "server-http-url": "http://localhost:3000" }, () => ({ status: 200, body }));
    await assert.rejects(service.getServerMods(), /Could not verify server mod list/);
  });
}

for (const value of ["", "localhost:3000", "file:///manifest.json", "http://localhost:3000?x=1", 3000, null]) {
  test(`invalid direct-server configuration does not silently use the gateway: ${value}`, async () => {
    const { service, clients } = makeService({ "server-http-url": value }, success);
    await assert.rejects(service.getServerMods(), /server-http-url/);
    assert.deepEqual(clients, []);
  });
}
