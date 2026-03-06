using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.DTOs;
using TurnBasedGame.Application.Queries;

namespace TurnBasedGame.Application.Interfaces;

/// <summary>
/// Defines the contract for the game engine that orchestrates all game operations.
/// Application service that coordinates domain objects without containing business logic.
/// </summary>
public interface IGameEngine
{
    // Commands (state-changing operations)
    
    /// <summary>
    /// Creates a new game with the specified configuration.
    /// </summary>
    Result<Guid> CreateGame(CreateGameCommand command);

    /// <summary>
    /// Places a unit on the board during game setup.
    /// </summary>
    Result<Guid> PlaceUnit(PlaceUnitCommand command);

    /// <summary>
    /// Moves a unit to a new position.
    /// </summary>
    Result MoveUnit(MoveUnitCommand command);

    /// <summary>
    /// Executes an attack from one unit to another.
    /// </summary>
    Result<CombatResultDto> Attack(AttackCommand command);

    /// <summary>
    /// Ends the current player's turn and advances to the next player.
    /// </summary>
    Result EndTurn(EndTurnCommand command);

    /// <summary>
    /// Sets the terrain type for a tile during game setup.
    /// </summary>
    Result SetTerrain(SetTerrainCommand command);

    // Queries (read-only operations)

    /// <summary>
    /// Gets the complete current game state.
    /// </summary>
    Result<GameStateDto> GetGameState(GetGameStateQuery query);

    /// <summary>
    /// Gets all valid positions a unit can move to.
    /// </summary>
    Result<ValidMovesDto> GetValidMoves(GetValidMovesQuery query);

    /// <summary>
    /// Gets all units belonging to a specific player.
    /// </summary>
    Result<IReadOnlyList<UnitDto>> GetPlayerUnits(GetPlayerUnitsQuery query);

    /// <summary>
    /// Checks if a player has any units that can still act this turn.
    /// </summary>
    Result<bool> CanPlayerAct(CanPlayerActQuery query);

    /// <summary>
    /// Gets information about a specific unit.
    /// </summary>
    Result<UnitDto> GetUnit(GetUnitQuery query);
}