# Unity Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6.5 (6000.5.8f1) |
| **Release Date** | Mid-June 2026 |
| **Release Type** | Supported (not LTS — the current LTS is 6.3) |
| **Project Pinned** | 2026-08-26 |
| **Last Docs Verified** | 2026-08-26 |
| **LLM Knowledge Cutoff** | May 2025 |

## Correction Log

Prior version of this file incorrectly pinned "Unity 6.3 LTS" — the actual
installed Editor (`ProjectSettings/ProjectVersion.txt`) is `6000.5.8f1`,
which is Unity 6.5, a Supported release from mid-June 2026, not the 6.3 LTS
line. Corrected 2026-08-26. Always cross-check `ProjectVersion.txt` before
trusting this file's version pin.

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2022 LTS (2022.3), and at
best early Unity 6000.x builds. Unity 6.5 (mid-June 2026) is well beyond
that cutoff. Always cross-reference this directory before suggesting Unity
API calls.

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 6.0 | Oct 2024 | HIGH | Unity 6 rebrand, new rendering features, Entities 1.3, DOTS improvements |
| 6.1 | Nov 2024 | MEDIUM | Bug fixes, stability improvements |
| 6.2 | Dec 2024 | MEDIUM | Performance optimizations, new input system improvements |
| 6.3 LTS | Dec 2025 | HIGH | First LTS since 6.0, production-ready DOTS, enhanced graphics features |
| 6.4 | ~Q1 2026 | HIGH | Render pipeline / AI tooling roadmap step toward CoreCLR |
| 6.5 | Jun 2026 | HIGH | **Project's current version.** Built-in Render Pipeline deprecated, dynamic batching deprecated, continues Mono→CoreCLR groundwork |

## Active Migration Risk: Mono → CoreCLR

Unity is replacing the Mono scripting runtime with Microsoft's CoreCLR —
the most significant change to Unity's C# layer in over a decade. Timeline:

- **6.5 (current)**: groundwork only, Mono still the active runtime — no
  action needed yet.
- **6.7**: experimental CoreCLR player becomes available.
- **6.8**: full removal of Mono — CoreCLR becomes the only runtime.

Re-verify this section with `/setup-engine refresh` before any upgrade past
6.5, and re-check third-party plugins for CoreCLR compatibility before that
upgrade.

## Other Deprecations Relevant Going Forward

- **Built-in Render Pipeline (BIRP)**: deprecated in 6.5, still functional
  through the 6.7 LTS lifecycle. This project uses URP (2D), so no direct
  impact — noted for the record only.
- **Dynamic batching**: deprecated in 6.5, scheduled for removal in a future
  release. This project's SRP Batcher-based 2D rendering is unaffected.

## Major Changes from 2022 LTS to Unity 6.x

### Breaking Changes
- **Entities/DOTS**: Major API overhaul in Entities 1.0+, complete redesign of ECS patterns
- **Input System**: Legacy Input Manager deprecated, new Input System is default
- **Rendering**: URP/HDRP significant upgrades, SRP Batcher improvements
- **Addressables**: Asset management workflow changes
- **Scripting**: C# 9 support, new API patterns

### New Features (Post-Cutoff)
- **DOTS**: Production-ready Entity Component System (Entities 1.3+)
- **Graphics**: Enhanced URP/HDRP pipelines, GPU Resident Drawer
- **Multiplayer**: Netcode for GameObjects improvements
- **UI Toolkit**: Production-ready for runtime UI (replaces UGUI for new projects)
- **Async Asset Loading**: Improved Addressables performance
- **Web**: WebGPU support

### Deprecated Systems
- **Legacy Input Manager**: Use new Input System package
- **Legacy Particle System**: Use Visual Effect Graph
- **UGUI**: Still supported, but UI Toolkit recommended for new projects
- **Old ECS (GameObjectEntity)**: Replaced by modern DOTS/Entities
- **Built-in Render Pipeline**: Deprecated as of 6.5 (see above)
- **Dynamic batching**: Deprecated as of 6.5 (see above)

## Verified Sources

- Official docs: https://docs.unity3d.com/6000.0/Documentation/Manual/index.html
- Unity 6 release: https://unity.com/releases/unity-6
- Unity 6.5 release notes: https://unity.com/releases/editor/whats-new/6000.5.8f1
- Unity 6.3 LTS vs 6.5 Supported: https://unity.com/releases/unity-6/support
- CoreCLR migration path: https://discussions.unity.com/t/path-to-coreclr-2026-upgrade-guide/1714279
- Migration guide: https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- C# API reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/index.html
