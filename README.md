# TurnBasedGame

A turn-based tactical grid game built in C#/.NET with clean layer boundaries and deterministic combat.

This project is designed as a portfolio example for recruiters and hiring teams: it demonstrates practical domain modeling, clear architecture, testability, and iterative product polish.

## What The Game Does

- Tactical match on a grid with either:
  - Human vs Human
  - Human vs AI
- Each player starts with the same roster:
  - `W - Warrior` (frontline)
  - `S - Scout` (mobile flanker)
- On a turn, a player selects one unit and performs one action:
  - unit selection uses abbreviations (`W`, `S`)
  - action selection uses abbreviations (`A`, `M`)
  - `move` (arrow-key destination selection)
  - `attack` (melee adjacency, including diagonals)
- Game ends when one side has no remaining units.
- Control tile win: hold the green center tile for 5 turns to win.
- Control tile only appears on Hard AI.

## AI Behavior (Current)

- AI controls Player 2 when enabled at game start.
- AI attack policy:
  - if both unit types can attack, prefer `Scout` over `Warrior`
- AI movement policy:
  - if no attack is available, alternate preferred mover each AI turn:
    - `Warrior` turn, then `Scout` turn, then repeat
  - if preferred unit cannot move, AI falls back to the other unit

## Why This Project Is Interesting

- Domain rules are explicit and enforced in code (movement, range, turn ownership, victory).
- The app uses a layered architecture (`Domain`, `Application`, `ConsoleUI`) rather than a monolith.
- Console UX was intentionally iterated:
  - 1-indexed coordinates for human-friendly input
  - tactical-grid rendering
  - arrow-key movement with live board cursor highlight
  - persistent on-screen rules and unit info
  - in-game `HELP` command to re-open rules
  - retry flow for invalid actions without auto-ending turns

## Architecture

- `TurnBasedGame.Domain`:
  - game rules, entities, value objects, exceptions
  - no UI concerns
- `TurnBasedGame.Application`:
  - command contracts, `GameService`, `Result` pattern
  - orchestrates domain behavior and returns user-safe outcomes
- `TurnBasedGame.ConsoleUI`:
  - input loop and board rendering
  - no business rules
- `TurnBasedGame.Tests`:
  - integration-style tests for key gameplay flows

See layer-specific READMEs for detail:
- `TurnBasedGame.Domain/README.md`
- `TurnBasedGame.Application/README.md`
- `TurnBasedGame.ConsoleUI/README.md`
- `STRUCTURE.md` (repository layout and dependency direction)

## Tech Stack

- `.NET 9`
- `C#`
- `xUnit`

## Install .NET (If You Don’t Have It)

You need the .NET SDK (not just the runtime). Install steps:

1. Download and install the .NET SDK for your OS:
```
https://dotnet.microsoft.com/download
```
2. Verify the install:
```bash
dotnet --version
```

## Run The Game

```bash
dotnet run --project TurnBasedGame.ConsoleUI/TurnBasedGame.ConsoleUI.csproj
```

## Build

```bash
dotnet build TurnBasedGame.sln
```

## Test

```bash
dotnet test TurnBasedGame.Tests/TurnBasedGame.Tests.csproj
```

## AI Assistance Disclosure

This project was built with AI-assisted development.

I used OpenAI and Claude tooling as an engineering copilot for:
- refactoring and architectural cleanup
- gameplay loop implementation
- UI/UX polish in the console layer
- test generation and bug-fix iteration
- documentation drafting

All design decisions, code review, and final acceptance were directed by me.

## What I Would Build Next

- Configurable maps and scenario presets
- Additional unit classes
- CI pipeline with automated build + tests
- Stronger tactical AI (lookahead/scoring) and difficulty settings
