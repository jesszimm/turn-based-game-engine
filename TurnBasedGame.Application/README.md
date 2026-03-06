# TurnBasedGame.Application

The **Application Layer** orchestrates game operations by coordinating domain entities and services. This layer contains **no business logic** - it delegates all rules to the Domain layer.

## Architecture Principles

- **Orchestration, not logic**: GameEngine coordinates domain objects
- **Clean error handling**: All exceptions caught and returned as Result types
- **Immutable DTOs**: All data transfer objects use C# records
- **CQRS-lite**: Commands change state, Queries read state
- **No infrastructure**: No databases, files, or external dependencies

## Core Components

### Result Types
- **Result\<T\>**: Wraps successful values or error messages
- **Result**: For operations without return values
- Never throw exceptions to calling code - always return Results

### Commands (State-Changing)
- **CreateGameCommand**: Initialize new game with board and players
- **PlaceUnitCommand**: Add units to the board during setup
- **MoveUnitCommand**: Move a unit to a new position
- **AttackCommand**: Execute combat between units
- **EndTurnCommand**: Advance to next player's turn
- **SetTerrainCommand**: Configure board terrain during setup

### Queries (Read-Only)
- **GetGameStateQuery**: Retrieve complete game snapshot
- **GetValidMovesQuery**: Get positions a unit can move to
- **GetPlayerUnitsQuery**: Get all units for a player
- **CanPlayerActQuery**: Check if player has actionable units
- **GetUnitQuery**: Get details about a specific unit

### DTOs (Data Transfer Objects)
- **GameStateDto**: Complete game state snapshot
- **UnitDto**: Unit information for display
- **PlayerDto**: Player information with unit counts
- **TileDto**: Tile information with terrain details
- **CombatResultDto**: Results of an attack
- **ValidMovesDto**: Valid movement positions
- **PositionDto**: Simple X/Y coordinate

### GameEngine (IGameEngine)
The main application service that:
1. Receives commands/queries
2. Validates inputs at application level
3. Delegates to domain entities
4. Catches domain exceptions
5. Returns clean Result objects

## Design Patterns

### 1. Result Pattern
Avoids throwing exceptions across layer boundaries:
```csharp
var result = gameEngine.MoveUnit(command);
if (result.IsFailure)
{
    Console.WriteLine(result.ErrorMessage);
    return;
}
```

### 2. Command Pattern
Encapsulates requests as objects:
```csharp
var command = new MoveUnitCommand 
{ 
    UnitId = unitId, 
    TargetX = 5, 
    TargetY = 3 
};
var result = gameEngine.MoveUnit(command);
```

### 3. Query Pattern
Separates reads from writes:
```csharp
var query = new GetGameStateQuery();
var result = gameEngine.GetGameState(query);
```

### 4. DTO Pattern
Decouples domain models from external contracts:
- Domain changes don't break UI
- DTOs are optimized for display/serialization
- No domain logic leaks to presentation

## Responsibilities

### What This Layer DOES:
✅ Orchestrate domain objects  
✅ Validate application-level concerns (null checks, game state)  
✅ Convert between domain models and DTOs  
✅ Catch domain exceptions and return Results  
✅ Manage turn order and player progression  
✅ Track game state (current player, turn number)

### What This Layer DOES NOT DO:
❌ Implement game rules (that's Domain)  
❌ Render UI (that's Presentation)  
❌ Persist data (that's Infrastructure)  
❌ Handle user input directly (that's UI)  
❌ Contain business logic  

## Error Handling Strategy

All methods return `Result` or `Result<T>`:

```csharp
// Command failed - domain exception caught
var result = gameEngine.MoveUnit(command);
if (result.IsFailure)
    return result.ErrorMessage; // "Target position is occupied"

// Query failed - validation error
var stateResult = gameEngine.GetGameState(query);
if (stateResult.IsFailure)
    return stateResult.ErrorMessage; // "Game has not been created"

// Success - extract value
var gameState = stateResult.Value;
```

### Exception Categories Handled:
1. **Domain Exceptions**: InvalidMoveException, InvalidCombatException
2. **Argument Exceptions**: Null checks, invalid IDs
3. **General Exceptions**: Unexpected errors wrapped with context

## State Management

GameEngine maintains:
- `GameBoard`: The domain aggregate root
- `Dictionary<Guid, Player>`: All players in the game
- `Guid _currentPlayerId`: Whose turn it is
- `int _turnNumber`: Current turn counter

**Important**: This is in-memory state. Persistence is handled by Infrastructure layer.

## Turn Flow

1. Current player performs actions (move, attack)
2. Player calls `EndTurn`
3. GameEngine resets all current player's units
4. GameEngine advances to next active player
5. Turn number increments
6. Repeat

## Dependency Injection

GameEngine requires:
```csharp
public GameEngine(ICombatResolver combatResolver)
```

The Application layer depends on:
- **Domain abstractions** (ICombatResolver)
- **Domain entities** (GameBoard, Unit, Player)
- **Domain value objects** (Position, UnitStats)

The Application layer does NOT depend on:
- UI frameworks
- Database libraries
- File systems
- External APIs

## Testing Strategy

Application layer tests verify:
- Commands execute successfully with valid inputs
- Commands fail gracefully with invalid inputs
- Queries return correct DTOs
- Domain exceptions are caught and converted to Results
- Turn progression works correctly
- Player elimination is handled

Mock the ICombatResolver for unit tests.

## Next Steps

After the Application layer, you need:
1. **Infrastructure Layer**: State persistence, repositories
2. **Console UI Layer**: Renderers, input handlers, game loop
3. **Tests**: Unit tests for both Domain and Application

## Usage Example

```csharp
var combatResolver = new CombatResolver();
var engine = new GameEngine(combatResolver);

// Create game
var createResult = engine.CreateGame(new CreateGameCommand
{
    BoardWidth = 8,
    BoardHeight = 8,
    PlayerNames = new[] { "Alice", "Bob" }
});

if (createResult.IsFailure)
{
    Console.WriteLine(createResult.ErrorMessage);
    return;
}

// Get game state
var stateResult = engine.GetGameState(new GetGameStateQuery());
if (stateResult.IsSuccess)
{
    var state = stateResult.Value;
    Console.WriteLine($"Turn {state.TurnNumber}");
    Console.WriteLine($"Current Player: {state.CurrentPlayerId}");
}
```