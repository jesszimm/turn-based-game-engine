# TurnBasedGame.Domain

This is the **core domain layer** of the turn-based strategy game. It contains all game logic, rules, and entities with **zero dependencies** on UI, persistence, or external frameworks.

## Architecture Principles

- **Pure domain logic**: No I/O, no UI concerns, no framework dependencies
- **Immutable value objects**: Position and UnitStats use C# records
- **Entity identity**: Players and Units have Guid-based identity
- **Encapsulation**: Internal methods prevent invalid state changes
- **Explicit invariants**: Validation in constructors and domain methods

## Core Concepts

### Value Objects (Immutable)
- **Position**: Coordinates on the game board (X, Y)
- **UnitStats**: Combat attributes (Health, Attack, Defense, Movement)
- **TerrainType**: Enum defining terrain properties

### Entities (Mutable State)
- **Player**: Represents a game participant
- **Unit**: Combat unit with stats, position, and turn state
- **Tile**: Single cell on the board with terrain and occupancy
- **GameBoard**: Aggregate root managing the grid and unit placement

### Domain Services
- **ICombatResolver / CombatResolver**: Handles combat calculations

### Domain Exceptions
- **DomainException**: Base for all domain errors
- **InvalidMoveException**: Movement rule violations
- **InvalidCombatException**: Combat rule violations

## Game Rules (Current Implementation)

### Movement
- Units have a `MovementRange` stat
- Can move to any unoccupied, passable tile within range
- Different terrains have different movement costs (currently simplified)
- Units can only move once per turn

### Combat
- Melee combat only (must be adjacent)
- Damage = Attacker.Attack - (Defender.Defense + Terrain Bonus)
- Minimum damage is always 1
- Units can only attack once per turn
- Cannot attack friendly units

### Terrain
- **Plains**: Normal terrain (no modifiers)
- **Forest**: +1 defense bonus, higher movement cost
- **Mountain**: +2 defense bonus, higher movement cost
- **Water**: Impassable

### Turn Structure
- Each unit can move once and act once per turn
- `ResetTurnState()` must be called at turn start
- Dead units cannot act

## Design Decisions

### Why internal setters and methods?
- Prevents external code from bypassing business rules
- Forces all state changes through validated domain methods
- GameBoard is the aggregate root controlling unit placement

### Why no randomness in combat?
- Makes testing deterministic
- Simplifies initial implementation
- Can easily swap CombatResolver with a randomized version later

### Why Guid for IDs instead of integers?
- Prevents ID collision in distributed scenarios
- Easier to merge game states
- More enterprise-like (think: microservices)

### Why separate Tile from Position?
- Position is a pure value (mathematical coordinate)
- Tile has state (terrain, occupancy)
- Separation of concerns: geometry vs. game state

## Testing Strategy

This layer should have extensive unit tests:
- Value object behavior (immutability, equality)
- Entity state transitions
- Domain rule enforcement (movement, combat)
- Exception throwing for invalid operations

## What's NOT Here

- ❌ Game loop / turn management → Application layer
- ❌ User input handling → UI layer
- ❌ Save/load functionality → Infrastructure layer
- ❌ AI opponent logic → Application layer
- ❌ Rendering / display → UI layer

## Next Steps

The domain layer is complete for initial implementation. Next phases:
1. **Testing**: Create unit tests for all domain logic
2. **Application Layer**: Add GameEngine, Commands, Queries
3. **Infrastructure**: Add repositories for state persistence
4. **Console UI**: Add renderers and input handlers
