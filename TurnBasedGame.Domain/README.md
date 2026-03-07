# TurnBasedGame.Domain

The Domain layer contains the core game model and business rules.

## Responsibility

This layer answers:
- What is a valid move?
- What is a valid attack?
- Whose turn is it?
- When is the game over?

It does not know about console input, rendering, persistence, or APIs.

## Core Types

- `Entities/Game.cs`
  - aggregate root for a match
  - manages turn order, move/attack constraints, winner detection
- `Entities/GameBoard.cs`
  - board state, placement, movement, attack range checks, unit removal
- `Entities/Unit.cs`
  - unit state (position, health, move/act flags)
- `Entities/Player.cs`
- `ValueObjects/Position.cs`, `UnitStats.cs`, `TerrainType.cs`
- `Services/CombatResolver.cs`
  - deterministic damage calculation
- `Exceptions/*`
  - domain-specific invalid action errors

## Key Domain Rules

- Movement respects board bounds, occupancy, passability, and movement range.
- Combat is melee (adjacent tiles).
- Dead units are removed from board state.
- Turn ownership is enforced for actions.
- Winner is determined when one player has no living units.

## Design Notes

- Rich domain model over anemic data structures.
- Invariants are validated close to the data they protect.
- Deterministic combat makes behavior predictable and testable.
