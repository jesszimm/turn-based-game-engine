# TurnBasedGame Structure

Current repository structure after cleanup.

## Solution Projects

- `TurnBasedGame.Domain`
- `TurnBasedGame.Application`
- `TurnBasedGame.ConsoleUI`
- `TurnBasedGame.Tests`

## Layer Map

- `Domain`:
  - Core game model and business rules.
  - Key files:
    - `Entities/Game.cs`
    - `Entities/GameBoard.cs`
    - `Entities/Unit.cs`
    - `Services/CombatResolver.cs`
- `Application`:
  - Orchestration and command contract surface.
  - Key files:
    - `Services/GameService.cs`
    - `Commands/GameCommand.cs`
    - `Result.cs`
- `ConsoleUI`:
  - Game loop and board rendering.
  - Key files:
    - `Program.cs`
    - `Renderers/ConsoleBoardRenderer.cs`
- `Tests`:
  - Integration-style tests for gameplay flow.
  - Key file:
    - `UnitTest1.cs`

## Dependency Direction

`ConsoleUI -> Application -> Domain`

`Tests` reference `Application` and `Domain`.

## Notes

- The placeholder Infrastructure project was removed because it was unused.
- Legacy empty folders from older architecture drafts were removed.
