# Claude.md — InertiCorp Engineering Guardrails

Project: **InertiCorp: A Bureaucracy Simulator**  
Tagline: *Nudge a massive corporation toward your goals—and watch it misunderstand you in real time.*

This file defines non-negotiable constraints and working practices for all AI-assisted changes.

---

## 0) Prime Directive

Keep changes **small, testable, and modular**.

- Prefer multiple tiny PRs over one big “refactor + feature” PR.
- Never “just implement everything.” Implement the **minimum** to satisfy the current story’s acceptance criteria.

---

## 1) Architecture Rules (Hard)

### 1.1 Separate Simulation from Presentation
The core simulation must be UI-agnostic and deterministic.

- **Simulation/Core** code must not reference:
  - Godot nodes/scenes
  - Rendering, UI, input, audio
  - any Godot types (unless explicitly allowed by story)
- Godot layer is a thin adapter that:
  - reads user intent (button clicks, choices)
  - calls simulation services
  - renders the returned state

### 1.2 Determinism
- All randomness must flow through a single RNG abstraction (seeded).
- Given the same initial state + same inputs + same seed, the simulation must produce identical outputs.

### 1.3 Dependency Direction
- `Core` depends on nothing project-specific.
- `Core` may depend on `System.*` only.
- `Game` (Godot layer) depends on `Core`.
- Tests depend on `Core` (and optionally test helpers).

---

## 2) TDD Expectations

### 2.1 Default to TDD for Core
For simulation logic:
1. Write failing test
2. Implement minimal code to pass
3. Refactor safely

### 2.2 What Must Have Tests (Core)
- RNG behavior (seed reproducibility)
- Turn advancement
- Event card resolution
- State updates & invariants
- Serialization of state (if/when added)

### 2.3 What Can Be Lightly Tested
Godot UI nodes can be lightly tested or validated with manual smoke tests unless a story explicitly calls for automated UI tests.

---

## 3) Scope Control

### 3.1 No “Helpful” Extras
Do not add features, refactors, or architecture changes not required by the current story.

No:
- new frameworks
- new patterns
- “future-proofing”
- large redesigns
unless the story explicitly requires it.

### 3.2 If You Detect Missing Info
Make reasonable defaults and document them in the PR/commit message.

Do not block progress with questions unless it’s truly impossible.

---

## 4) Coding Standards

### 4.1 C# Style
- Use nullable reference types.
- Prefer immutable state objects for simulation (records) where practical.
- Use explicit types for public APIs (avoid `var` in public signatures).
- Keep methods small; avoid deep nesting.

### 4.2 Naming
Namespaces:
- `InertiCorp.Core.*` for simulation
- `InertiCorp.Game.*` for Godot integration
- `InertiCorp.Tests.*` for tests

### 4.3 Error Handling
- Core should validate inputs and fail fast with meaningful exceptions.
- Do not swallow exceptions.

---

## 5) Simulation Design Guidelines

### 5.1 State Model
Keep the initial model minimal:
- `GameState`
- `OrgState` (meters, hidden variables later)
- `TurnState` (turn index, quarter/week)
- `EventDeck` / `EventCard`
- `Choice` / `Effect`

### 5.2 Effects
Effects must be composable and explicit. Avoid hidden coupling.
Prefer:
- `IEffect.Apply(state, rng) -> newState + log entries`

### 5.3 Logs & Explainability
Simulation should emit a structured log of “what happened” each turn.
The UI will render these as headlines later.

---

## 6) Testing Tooling Guidance

Preferred test framework: **xUnit** (unless repo already sets a different standard).  
Avoid mocking unless necessary; favor pure functions and deterministic inputs.

---

## 7) Git Hygiene

- Keep commits focused and story-scoped.
- Update documentation only when it clarifies behavior or constraints.
- No large binary assets committed unless required.

---

## 8) Definition of Done (per story)

A story is done when:
- Acceptance criteria are met
- All tests pass
- No unrelated changes
- Core remains deterministic
- Modules respect dependency direction

---

## 9) Current Priorities

We are building a small, playable vertical slice:
1. Deterministic core loop (turn advance)
2. Event cards + choices + effects
3. Minimal Godot UI to play a “quarter” run
4. Expand content via data-driven events later

Keep the scope tight.
