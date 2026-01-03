# InertiCorp: A Bureaucracy Simulator

*Nudge a massive corporation toward your goals—and watch it misunderstand you in real time.*

A satirical card game where you play as a corporate CEO trying to survive board meetings, manage organizational metrics, and retire with a golden parachute before the board fires you.

## Gameplay

### The Goal
Accumulate enough bonus in your compensation package to trigger voluntary retirement before the board loses faith and terminates your employment. Survive quarters, hit profit targets, and manage five organizational meters while dealing with crises that threaten to derail your carefully laid plans.

### The Inbox
Your primary interface is the corporate inbox. All game events manifest as emails:
- **Project results** arrive as status updates from department heads
- **Crises** appear as urgent messages requiring immediate decisions
- **Board directives** set quarterly profit targets
- **Performance reviews** arrive at the end of each quarter

### Project Cards
Play projects from your hand to generate profit and affect organizational health. Each card has:
- **Profit potential** - Base revenue generation with outcome variance
- **Meter effects** - Impact on organizational metrics (positive and negative)
- **Evil score** - Moral cost of certain decisions (downsizing, corner-cutting, etc.)

**77 project cards** across three categories:
- **Revenue** - Direct money-making initiatives
- **Corporate** - Infrastructure and organizational investments
- **Action** - Strategic moves with trade-offs

### The Five Meters
Balance these competing organizational priorities:

| Meter | Represents |
|-------|------------|
| **Delivery** | Engineering velocity and product output |
| **Morale** | Employee satisfaction and retention |
| **Governance** | Compliance, security, and risk management |
| **Alignment** | Strategic focus and executive coherence |
| **Runway** | Financial reserves and operational buffer |

If any meter hits zero, you're in crisis territory. If it stays there too long, the board will notice.

### Board Favorability
The board starts with a baseline opinion of you. Each quarter they:
- Issue a **profit directive** (target you must hit)
- Evaluate your **performance** against that target
- Adjust their **favorability** based on results
- Grant **bonuses** (or penalties) to your golden parachute

When favorability drops too low, they'll terminate your employment. When it's high enough and you've accumulated sufficient bonus, you can trigger **voluntary retirement** and win.

### Crises
**30 crisis events** can trigger randomly or from failed projects. Each presents:
- A situation requiring your response
- Multiple choice options with different risk/reward profiles
- Consequences that affect meters, profit, and board opinion

### Difficulty Settings
Three modes named after famous CEOs:

| Difficulty | Name | Board Patience | Retirement Threshold |
|------------|------|----------------|---------------------|
| Easy | **The Welch** | Very patient, generous rewards | $120M |
| Regular | **The Nadella** | Balanced expectations | $140M |
| Hard | **The Icahn** | Activist investor, demanding | $180M |

## AI-Generated Emails

InertiCorp uses a local LLM (via [LLamaSharp](https://github.com/SciSharp/LLamaSharp)) to generate dynamic email content. Emails are written in Dilbert/Office Space style corporate satire with:
- Passive-aggressive status updates
- Credit-taking and blame-shifting
- Meaningless corporate buzzwords
- Panicked crisis communications

The AI knows who is sending each email and maintains character consistency. GPU acceleration is supported for faster generation.

### Supported Models
The game works with GGUF-format models. Recommended:
- **Phi-3 Mini** (3.8B) - Good balance of quality and speed
- **Qwen2** (1.5B) - Fast generation for slower hardware
- **TinyLlama** (1.1B) - Fastest, lower quality

## Technical Stack

- **Engine**: Godot 4.5.1 (C#/.NET 8.0)
- **Core**: Deterministic simulation, UI-agnostic
- **LLM**: LLamaSharp 0.25.0 with CUDA 12 support
- **Tests**: 440 unit tests

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run specific test category
dotnet test --filter "BalanceTests"
```

### Project Structure

```
src/
├── InertiCorp.Core/     # Simulation engine (Godot-free, deterministic)
│   ├── Cards/           # Project and crisis card logic
│   ├── Content/         # JSON card definitions
│   ├── Email/           # Email generation and templates
│   └── Llm/             # LLM integration
├── InertiCorp.Game/     # Godot UI layer
│   ├── Dashboard/       # Main game interface
│   ├── Settings/        # Options and configuration
│   └── Audio/           # Music management
tests/
└── InertiCorp.Core.Tests/  # Unit tests
```

### GPU Support

For CUDA acceleration:
1. NVIDIA GPU with CUDA Compute 5.0+
2. NVIDIA drivers 516.x or newer
3. Place CUDA runtime DLLs in `src/InertiCorp.Core/native/cuda12/`:
   - `cublas64_12.dll`
   - `cublasLt64_12.dll`
   - `cudart64_12.dll`

The game falls back to CPU if CUDA is unavailable.

## AI Guardrails

See [CLAUDE.md](CLAUDE.md) for engineering constraints and working practices used during development.

## License

MIT
