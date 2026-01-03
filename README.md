# InertiCorp: A Bureaucracy Simulator

> *"Synergy Through Excellence"*

**INTERNAL MEMO — CONFIDENTIAL**

To: New CEO (You)
From: Board of Directors
Re: Orientation Materials & Expectations Management

---

Congratulations on your appointment as Chief Executive Officer of InertiCorp Holdings, LLC. Your predecessor's departure was... abrupt, but the board is confident you will exceed their four-quarter tenure. The bar is not high.

This document contains everything you need to know about steering our beloved organization. Please initial each section to confirm you've read and misunderstood it.

## Your Objective

Accumulate sufficient bonus compensation to trigger **voluntary retirement** before the board loses confidence and terminates your employment. This is not as easy as it sounds. Your predecessor thought it was easy. Your predecessor is now consulting.

## The Inbox

All corporate activity flows through email. This is by design. If it's not in writing, it didn't happen. If it *is* in writing, Legal will handle it.

Your inbox will contain:
- **Project Updates** — Status reports from department heads explaining why things are behind schedule
- **Crisis Notifications** — Urgent matters requiring your immediate attention and eventual blame
- **Board Directives** — Quarterly profit targets that seem reasonable until you try to meet them
- **Performance Reviews** — The board's quarterly assessment of your continued employability

## Project Cards

Your strategic toolkit consists of **77 initiatives** across three categories:

| Category | Description | Risk Profile |
|----------|-------------|--------------|
| **Revenue** | Money-making schemes of varying legality | Direct profit, collateral damage |
| **Corporate** | Infrastructure investments that pay off "eventually" | Long-term value, short-term pain |
| **Action** | Strategic pivots with plausible deniability | High variance, higher drama |

Each project affects profit margins and organizational health. Choose wisely. Or don't. The board will blame you either way.

## Organizational Metrics

You must balance five competing priorities. The previous CEO balanced zero of them.

| Meter | What It Measures | What Happens at Zero |
|-------|------------------|---------------------|
| **Delivery** | Whether Engineering ships anything | Products don't exist |
| **Morale** | Employee will to continue existing here | Mass exodus, Glassdoor reviews |
| **Governance** | Compliance, security, "doing things properly" | Regulatory intervention |
| **Alignment** | Whether anyone knows what we're doing | Strategic incoherence |
| **Runway** | Cash reserves and financial buffer | Payroll becomes "aspirational" |

The meters are interconnected in ways that will seem arbitrary until you understand them, and then they'll seem *deliberately* arbitrary.

## The Board

The board has opinions about your performance. These opinions manifest as:

- **Favorability** — A number representing their collective faith in you (it decreases)
- **Quarterly Directives** — Profit targets that increase as their patience decreases
- **Compensation Decisions** — Bonuses when you succeed, clawbacks when you don't
- **Employment Decisions** — Self-explanatory

When favorability drops below acceptable levels, you will be invited to pursue other opportunities.

## Crisis Management

**30 crisis events** may occur during your tenure. Each presents difficult choices:

- The ethical option (low reward, high morale)
- The profitable option (high reward, high evil score, probable consequences)
- The middle option (mediocre on all dimensions, satisfies no one)

Your responses will be documented for the inevitable post-mortem.

## Difficulty Settings

The board's temperament depends on who's chairing:

| Mode | Namesake | Description |
|------|----------|-------------|
| **The Welch** | Jack Welch | Patient capital, generous rewards. The good old days. |
| **The Nadella** | Satya Nadella | Balanced expectations. Transformation through empathy. |
| **The Icahn** | Carl Icahn | Activist investor. Results by quarter-end or else. |

Choose based on how much stress you enjoy.

## AI-Powered Communications

InertiCorp has deployed a local language model to generate authentic corporate correspondence. All emails are written in the house style:

- Passive-aggressive status updates
- Strategic credit-taking and blame redistribution
- Buzzword-compliant messaging
- Appropriately panicked crisis communications

The AI knows exactly who is sending each email and maintains character consistency. It has been explicitly instructed never to break character or acknowledge that any of this is satirical. *It's not satirical. This is how corporations work.*

GPU acceleration is available for executives who value their time.

---

## Technical Specifications

*For the IT department's records:*

- **Platform**: Godot 4.5.1 (C#/.NET 8.0)
- **Architecture**: Deterministic simulation core, UI layer, 440 unit tests
- **LLM Integration**: LLamaSharp 0.25.0, CUDA 12 supported
- **Recommended Models**: Phi-3 Mini (3.8B), Qwen2 (1.5B), TinyLlama (1.1B)

```bash
# Build (requires .NET 8 SDK)
dotnet build

# Run tests
dotnet test

# Verify nothing is broken before blaming someone else
dotnet test --filter "BalanceTests"
```

### GPU Configuration

For CUDA acceleration, place the following in `src/InertiCorp.Core/native/cuda12/`:
- `cublas64_12.dll`
- `cublasLt64_12.dll`
- `cudart64_12.dll`

The system falls back to CPU inference if CUDA is unavailable. This is fine. Everything is fine.

---

## Acknowledgments

See [CLAUDE.md](CLAUDE.md) for the engineering guardrails that kept this project on track, or at least prevented it from going *too* far off track.

---

*This document is the property of InertiCorp Holdings, LLC. Unauthorized distribution will be handled by Legal. They're very good at handling things.*

**Good luck. You'll need it.**

— The Board
