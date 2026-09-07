# Repository Guidelines

## Project Structure & Module Organization

This is the shared SkyMP TR development repository. SkyMP source and upstream history are tracked here. Read `docs/MODEL_BRIEFING_TR.md` for latest architecture & inter-model briefing, `docs/HANDOFF_TR.md` for detailed historical status, and `docs/TEAM_WORKFLOW_TR.md` for collaboration. Documentation is Turkish. `sources.lock.json` records the upstream baseline, Custom Skills Framework version, and native artifact provenance. `.research` contains caches; `.local` contains machine settings, staged clients and saves. Both are ignored.

Edit `skymp5-client` for TypeScript client behavior, `skymp5-server` for native/server state and `skyrim-platform` for game integration. Native CMake builds use this repository's `build` directory; follow upstream `CONTRIBUTING.md` and `CLAUDE.md`. No root npm package exists. Keep upstream license files and history intact.

## Build, Test, and Development Commands

Run from this repository root in Windows PowerShell:

- `.\scripts\restore-sources.ps1` checks tracked upstream ancestry and restores the CSF research checkout without building it.
- `.\scripts\build-client.ps1` installs dependencies from the Yarn lockfile and compiles the client without deploying into Skyrim.
- `.\scripts\build-front.ps1` builds the tracked widget UI into `build/dist/client/Data/Platform/UI` without deploying into Skyrim.
- `node --test skymp5-client/tests/settingsService.test.cjs` runs manifest routing/failure tests without Skyrim.
- `.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'` prepares a fresh runtime using the caller's game installation. Requires GitHub CLI authentication and Node; change the game path as needed.
- `node .\scripts\test-local-server.mjs` starts the server, checks native readiness and the five-master manifest, then stops its own process.
- `.\scripts\start-local-server.ps1` runs the server until stopped.
- `.\scripts\prepare-local-client.ps1 -ProfileId 1` stages native files and the locally built client with offline settings.
- `.\scripts\check-client-prerequisites.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'` reports missing files; exit 2 means requirements remain.
- Follow `docs/GAME_LAB_TR.md` for the pinned Skyrim 1.6.1170 lab. `.\scripts\test-game-profile.ps1` verifies actual MO2 settings/save isolation; `.\scripts\start-game-lab.ps1` launches SKSE through that profile.

Do not run the smoke test alongside another server using the same ports. Preserve existing profiles and world data. Client compilation installs client dependencies; the server smoke test uses Node built-ins.

Do not use `Start-Process -Wait` to launch MO2: its job conflicts with MO2 child process breakaway. Use `-PassThru` and the process object's `WaitForExit()` instead. Keep the server and client on the same five master files; this machine's Steam 1.7 data differs from the 1.6.1170 lab.

## Evidence and Scope

Server startup, TypeScript compilation and manifest regression tests pass. Native runtime compatibility, two-player connections, persistence, C++ builds and perks remain unverified. Keep observations, source-code findings and proposals distinct. Old README versions and open issues do not prove current incompatibility or reproduce a bug.

The user's priority is playable infrastructure, then working perks using Skyrim's native menu, then RP professions. Custom Skills Framework is a researched candidate, not an installed dependency. Never report local perk visuals as proof of server-side effects.

Use `codex/` feature branches or separate worktrees. Lab CI builds the client and runs regression tests; upstream deployment workflows are preserved outside the active workflow directory. Follow `.editorconfig`, `.clang-format` and nearby TypeScript style. Update shared status when milestones change; exclude game assets, credentials and saves from commits.
