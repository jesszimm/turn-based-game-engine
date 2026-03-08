# TurnBasedGame.ConsoleUI

Console presentation layer for the tactical game.

## Responsibility

- Gather player input.
- Render the board and game state.
- Display rules, unit info, and error messages.
- Call `GameService` commands.
- Run the minimal AI turn loop when Human vs AI mode is enabled.

No game rules are implemented here.

## UX Features

- 1-indexed board coordinates for player-facing input.
- Tactical-grid rendering with axis labels.
- Unit symbols (`W`, `S`, etc.) color-coded by player ownership.
- Unit and target selection by abbreviation keys (`W`, `S`) instead of numeric IDs.
- Action selection by abbreviation keys (`A`, `M`).
- Move destination selection with arrow keys (`Enter` confirm, `Esc` cancel).
- Live board highlight for current arrow-key target tile.
- Persistent on-screen rules and roster summary.
- `HELP` command available during prompts to re-open rules.
- Invalid actions explain why they failed and re-prompt without ending turn.

## Human vs AI Mode

- Startup prompt allows selecting Human vs Human or Human vs AI.
- In AI mode, Player 2 is controlled by CPU.
- Current AI policy:
  - prefer Scout attacks when both Scout and Warrior attacks are available
  - if no attack is available, alternate movement preference between Warrior and Scout each AI turn
  - if preferred unit cannot move, fallback to the other unit

## Main Files

- `Program.cs`
  - main game loop and input flow
- `Renderers/ConsoleBoardRenderer.cs`
  - grid rendering and unit symbol display

## Run

```bash
dotnet run --project TurnBasedGame.ConsoleUI/TurnBasedGame.ConsoleUI.csproj
```
