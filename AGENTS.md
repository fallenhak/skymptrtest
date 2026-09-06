# Repository Guidelines

## Project Structure & Module Organization

This is the SkyMP TR research and local-lab handoff repository, not a complete SkyMP source fork. Read `docs/HANDOFF_TR.md` first, then the task-relevant documents it links. User-facing documentation is Turkish. `sources.lock.json` pins the inspected SkyMP and Custom Skills Framework commits and the tested Windows server artifact. Scripts restore reference checkouts into ignored `.research` and prepare a server under ignored `.local`; neither directory is committed.

Native source edits must eventually live in a tracked development checkout with their upstream history and licenses preserved. Do not leave implementation work exclusively in an ignored research clone. Root-level `npm install`, `npm test`, and CMake builds are not defined here; upstream build commands belong inside the restored SkyMP source tree.

## Build, Test, and Development Commands

Run from this repository root in Windows PowerShell:

- `.\scripts\restore-sources.ps1` restores pinned sources without initializing submodules or building them.
- `.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'` prepares a fresh runtime using the caller's game installation. Requires GitHub CLI authentication and Node; change the game path as needed.
- `node .\scripts\test-local-server.mjs` starts the server, checks native readiness and the five-master manifest, then stops its own process.
- `.\scripts\start-local-server.ps1` runs the server until stopped.

Do not run the smoke test alongside another server using the same ports. Preserve existing settings and world data. The root scripts use Node built-ins and PowerShell; no package installation is required.

## Evidence and Scope

The baseline passes server startup only. Client compatibility, two-player connections, persistence, native builds and perks remain unverified. Keep runtime observations, source-code findings and proposals distinct. Old README versions and open issues do not prove current incompatibility or reproduce a bug.

The user's priority is playable infrastructure, then working perks using Skyrim's native menu, then RP professions. Custom Skills Framework is a researched candidate, not an installed dependency. Never report local perk visuals as proof of server-side effects.

Update the handoff and dated evidence when milestones change. Keep game assets, credentials, machine-specific runtime settings and saves out of commits. No established commit convention, root formatter or CI pipeline existed at handoff; use `codex/` branches for subsequent feature work.
