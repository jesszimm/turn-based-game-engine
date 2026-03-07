# TurnBasedGame.Application

The Application layer orchestrates use-cases and exposes a clean API to the UI layer.

## Responsibility

- Accept user-intent commands.
- Coordinate Domain objects through `GameService`.
- Return `Result` / `Result<T>` instead of leaking exceptions to UI.

## Core Types

- `Services/GameService.cs`
  - single application façade used by the console app
- `Commands/GameCommand.cs`
  - command contracts:
    - `CreateGameCommand`
    - `PlaceUnitCommand`
    - `MoveUnitCommand`
    - `AttackUnitCommand`
    - `EndTurnCommand`
- `Result.cs`
  - success/failure wrapper with error messaging

## Why This Layer Exists

The UI should not need to know domain internals or exception handling details. It issues commands and receives clear outcomes.

## Example

```csharp
var service = new GameService();

var create = service.CreateGame(new CreateGameCommand
{
    Player1Name = "Alice",
    Player2Name = "Bob",
    BoardWidth = 5,
    BoardHeight = 5
});

if (create.IsFailure)
{
    Console.WriteLine(create.ErrorMessage);
}
```

## Design Notes

- Keeps controller/UI code thin.
- Centralizes orchestration for consistency.
- Improves testability and maintainability.
