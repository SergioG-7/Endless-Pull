# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.5 (6000.5.8f1)
- **Language**: C#
- **Rendering**: URP (2D)
- **Physics**: Physics2D

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC, Mobile (iOS/Android)
- **Input Methods**: Keyboard/Mouse, Touch
- **Primary Input**: Mouse (PC-first during development; touch layer follows for mobile)
- **Gamepad Support**: Partial
- **Touch Support**: Full
- **Platform Notes**: UI must work with both mouse clicks and touch taps. No hover-only interactions or tooltips as the sole source of information.

## Naming Conventions

- **Classes**: PascalCase (e.g., `HeroController`)
- **Variables**: PascalCase for public fields/properties (e.g., `MoveSpeed`); `_camelCase` for private fields (e.g., `_currentHealth`)
- **Signals/Events**: PascalCase C# events (e.g., `HealthChanged`)
- **Files**: PascalCase matching class (e.g., `HeroController.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `Hero_Base.prefab`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE

## Performance Budgets

- **Target Framerate**: 60fps
- **Frame Budget**: 16.6ms
- **Draw Calls**: mobile-appropriate — lean on the SRP Batcher, atlas 2D sprites where practical
- **Memory Ceiling**: [TO BE CONFIGURED — no specific target hardware set yet]

## Testing

- **Framework**: Unity Test Framework (NUnit) — already present in `Packages/manifest.json` (`com.unity.test-framework`), no `tests/` directory created yet
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP materials)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS, UGUI Canvas, runtime UI)
- **Additional Specialists**: unity-dots-specialist (ECS, Jobs system, Burst compiler), unity-addressables-specialist (asset loading, memory management, content catalogs)
- **Routing Notes**: Invoke primary for architecture and general C# code review. Invoke DOTS specialist for any ECS/Jobs/Burst code. Invoke shader specialist for rendering and visual effects. Invoke UI specialist for all interface implementation. Invoke Addressables specialist for asset management systems.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | Primary |
