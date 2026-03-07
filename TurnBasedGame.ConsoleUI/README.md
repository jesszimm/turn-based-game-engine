# TurnBasedGame.ConsoleUI

Console presentation layer for the tactical game.

## Responsibility

- Gather player input.
- Render the board and game state.
- Display rules, unit info, and error messages.
- Call `GameService` commands.

No game rules are implemented here.

## UX Features

- 1-indexed board coordinates for player-facing input.
- Tactical-grid rendering with axis labels.
- Unit symbols (`W`, `S`, etc.) color-coded by player ownership.
- Persistent on-screen rules and roster summary.
- Invalid actions explain why they failed and re-prompt without ending turn.

## Main Files

- `Program.cs`
  - main game loop and input flow
- `Renderers/ConsoleBoardRenderer.cs`
  - grid rendering and unit symbol display

## Run

```bash
dotnet run --project TurnBasedGame.ConsoleUI/TurnBasedGame.ConsoleUI.csproj
```
