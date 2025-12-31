# InertiCorp: A Bureaucracy Simulator

*Nudge a massive corporation toward your goals—and watch it misunderstand you in real time.*

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test
```

## Project Structure

- `src/InertiCorp.Core` - Simulation engine (Godot-free, deterministic)
- `src/InertiCorp.Game` - Godot UI layer
- `tests/InertiCorp.Core.Tests` - Unit tests for Core

## AI Guardrails

See [CLAUDE.md](CLAUDE.md) for engineering constraints and working practices.
