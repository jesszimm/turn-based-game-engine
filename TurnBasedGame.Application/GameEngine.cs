using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.DTOs;
using TurnBasedGame.Application.Interfaces;
using TurnBasedGame.Application.Queries;
using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.Interfaces;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Application;

/// <summary>
/// Orchestrates game operations by coordinating domain entities and services.
/// Does not contain business logic - delegates to domain layer.
/// </summary>
public sealed class GameEngine : IGameEngine
{
    private readonly ICombatResolver _combatResolver;
    private GameBoard? _board;
    private readonly Dictionary<Guid, Player> _players;
    private Guid _currentPlayerId;
    private int _turnNumber;

    public GameEngine(ICombatResolver combatResolver)
    {
        _combatResolver = combatResolver ?? throw new ArgumentNullException(nameof(combatResolver));
        _players = new Dictionary<Guid, Player>();
        _turnNumber = 0;
    }

    public Result<Guid> CreateGame(CreateGameCommand command)
    {
        try
        {
            if (command == null)
                return Result<Guid>.Failure("Command cannot be null");

            if (command.BoardWidth <= 0 || command.BoardHeight <= 0)
                return Result<Guid>.Failure("Board dimensions must be positive");

            if (command.PlayerNames == null || command.PlayerNames.Count < 2)
                return Result<Guid>.Failure("Game requires at least 2 players");

            // Create board
            _board = new GameBoard(command.BoardWidth, command.BoardHeight);

            // Create players
            _players.Clear();
            foreach (var playerName in command.PlayerNames)
            {
                var player = new Player(playerName);
                _players[player.Id] = player;
            }

            // Set first player as current
            _currentPlayerId = _players.First().Key;
            _turnNumber = 1;

            return Result<Guid>.Success(_currentPlayerId);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to create game: {ex.Message}");
        }
    }

    public Result<Guid> PlaceUnit(PlaceUnitCommand command)
    {
        try
        {
            if (_board == null)
                return Result<Guid>.Failure("Game has not been created");

            if (command == null)
                return Result<Guid>.Failure("Command cannot be null");

            if (!_players.ContainsKey(command.OwnerId))
                return Result<Guid>.Failure("Player not found");

            var position = new Position(command.X, command.Y);
            var stats = new UnitStats(
                command.MaxHealth,
                command.AttackPower,
                command.Defense,
                command.MovementRange);

            var unit = new Unit(
                command.UnitName,
                command.OwnerId,
                position,
                stats);

            _board.PlaceUnit(unit, position);

            return Result<Guid>.Success(unit.Id);
        }
        catch (InvalidMoveException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to place unit: {ex.Message}");
        }
    }

    public Result SetTerrain(SetTerrainCommand command)
    {
        try
        {
            if (_board == null)
                return Result.Failure("Game has not been created");

            if (command == null)
                return Result.Failure("Command cannot be null");

            var position = new Position(command.X, command.Y);

            if (!Enum.TryParse<TerrainType>(command.TerrainType, ignoreCase: true, out var terrainType))
                return Result.Failure($"Invalid terrain type: {command.TerrainType}");

            _board.SetTerrain(position, terrainType);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to set terrain: {ex.Message}");
        }
    }

    public Result MoveUnit(MoveUnitCommand command)
    {
        try
        {
            if (_board == null)
                return Result.Failure("Game has not been created");

            if (command == null)
                return Result.Failure("Command cannot be null");

            var unit = _board.FindUnit(command.UnitId);
            if (unit == null)
                return Result.Failure("Unit not found");

            if (unit.OwnerId != _currentPlayerId)
                return Result.Failure("Not this player's turn");

            var targetPosition = new Position(command.TargetX, command.TargetY);

            _board.MoveUnit(unit, targetPosition);

            return Result.Success();
        }
        catch (InvalidMoveException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to move unit: {ex.Message}");
        }
    }

    public Result<CombatResultDto> Attack(AttackCommand command)
    {
        try
        {
            if (_board == null)
                return Result<CombatResultDto>.Failure("Game has not been created");

            if (command == null)
                return Result<CombatResultDto>.Failure("Command cannot be null");

            var attacker = _board.FindUnit(command.AttackerId);
            if (attacker == null)
                return Result<CombatResultDto>.Failure("Attacker not found");

            var defender = _board.FindUnit(command.DefenderId);
            if (defender == null)
                return Result<CombatResultDto>.Failure("Defender not found");

            if (attacker.OwnerId != _currentPlayerId)
                return Result<CombatResultDto>.Failure("Not this player's turn");

            var defenderTile = _board.GetTile(defender.Position);
            var damageDealt = _combatResolver.CalculateDamage(attacker, defender, defenderTile);

            _combatResolver.ResolveCombat(attacker, defender, defenderTile);

            var combatResult = new CombatResultDto
            {
                AttackerId = attacker.Id,
                DefenderId = defender.Id,
                DamageDealt = damageDealt,
                DefenderHealthRemaining = defender.Stats.CurrentHealth,
                DefenderDefeated = !defender.IsAlive
            };

            // Remove defeated unit from board
            if (!defender.IsAlive)
            {
                _board.RemoveUnit(defender);
                CheckPlayerStatus(defender.OwnerId);
            }

            return Result<CombatResultDto>.Success(combatResult);
        }
        catch (InvalidCombatException ex)
        {
            return Result<CombatResultDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result<CombatResultDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<CombatResultDto>.Failure($"Failed to execute attack: {ex.Message}");
        }
    }

    public Result EndTurn(EndTurnCommand command)
    {
        try
        {
            if (_board == null)
                return Result.Failure("Game has not been created");

            if (command == null)
                return Result.Failure("Command cannot be null");

            if (command.PlayerId != _currentPlayerId)
                return Result.Failure("Not this player's turn");

            // Reset all units for the next turn
            var currentPlayerUnits = _board.GetPlayerUnits(_currentPlayerId);
            foreach (var unit in currentPlayerUnits)
            {
                unit.ResetTurnState();
            }

            // Advance to next player
            var nextPlayerId = GetNextPlayerId();
            if (!nextPlayerId.HasValue)
                return Result.Failure("No active players remaining");

            _currentPlayerId = nextPlayerId.Value;
            _turnNumber++;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to end turn: {ex.Message}");
        }
    }

    private void CheckPlayerStatus(Guid playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
            return;

        var playerUnits = _board?.GetPlayerUnits(playerId) ?? Enumerable.Empty<Unit>();
        var hasLivingUnits = playerUnits.Any(u => u.IsAlive);

        if (!hasLivingUnits && player.IsActive)
        {
            player.Deactivate();
        }
    }

    private Guid? GetNextPlayerId()
    {
        var activePlayers = _players.Values.Where(p => p.IsActive).ToList();
        if (activePlayers.Count == 0)
            return null;

        var currentIndex = activePlayers.FindIndex(p => p.Id == _currentPlayerId);
        var nextIndex = (currentIndex + 1) % activePlayers.Count;

        return activePlayers[nextIndex].Id;
    }

    // Query methods

    public Result<GameStateDto> GetGameState(GetGameStateQuery query)
    {
        try
        {
            if (_board == null)
                return Result<GameStateDto>.Failure("Game has not been created");

            var tiles = _board.Tiles.Select(t => new TileDto
            {
                X = t.Position.X,
                Y = t.Position.Y,
                Terrain = t.Terrain.ToString(),
                IsOccupied = t.IsOccupied,
                OccupyingUnitId = t.OccupyingUnitId,
                MovementCost = t.GetMovementCost(),
                DefenseBonus = t.GetDefenseBonus()
            }).ToList();

            var units = _board.GetAllUnits().Select(u => new UnitDto
            {
                Id = u.Id,
                Name = u.Name,
                OwnerId = u.OwnerId,
                X = u.Position.X,
                Y = u.Position.Y,
                CurrentHealth = u.Stats.CurrentHealth,
                MaxHealth = u.Stats.MaxHealth,
                AttackPower = u.Stats.AttackPower,
                Defense = u.Stats.Defense,
                MovementRange = u.Stats.MovementRange,
                HasMovedThisTurn = u.HasMovedThisTurn,
                HasActedThisTurn = u.HasActedThisTurn,
                IsAlive = u.IsAlive
            }).ToList();

            var players = _players.Values.Select(p => new PlayerDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                UnitCount = _board.GetPlayerUnits(p.Id).Count(u => u.IsAlive)
            }).ToList();

            var gameState = new GameStateDto
            {
                BoardWidth = _board.Width,
                BoardHeight = _board.Height,
                Tiles = tiles,
                Units = units,
                Players = players,
                CurrentPlayerId = _currentPlayerId,
                TurnNumber = _turnNumber
            };

            return Result<GameStateDto>.Success(gameState);
        }
        catch (Exception ex)
        {
            return Result<GameStateDto>.Failure($"Failed to get game state: {ex.Message}");
        }
    }

    public Result<ValidMovesDto> GetValidMoves(GetValidMovesQuery query)
    {
        try
        {
            if (_board == null)
                return Result<ValidMovesDto>.Failure("Game has not been created");

            if (query == null)
                return Result<ValidMovesDto>.Failure("Query cannot be null");

            var unit = _board.FindUnit(query.UnitId);
            if (unit == null)
                return Result<ValidMovesDto>.Failure("Unit not found");

            var validPositions = _board.GetValidMovePositions(unit)
                .Select(p => new PositionDto { X = p.X, Y = p.Y })
                .ToList();

            var result = new ValidMovesDto
            {
                UnitId = unit.Id,
                ValidPositions = validPositions
            };

            return Result<ValidMovesDto>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ValidMovesDto>.Failure($"Failed to get valid moves: {ex.Message}");
        }
    }

    public Result<IReadOnlyList<UnitDto>> GetPlayerUnits(GetPlayerUnitsQuery query)
    {
        try
        {
            if (_board == null)
                return Result<IReadOnlyList<UnitDto>>.Failure("Game has not been created");

            if (query == null)
                return Result<IReadOnlyList<UnitDto>>.Failure("Query cannot be null");

            if (!_players.ContainsKey(query.PlayerId))
                return Result<IReadOnlyList<UnitDto>>.Failure("Player not found");

            var units = _board.GetPlayerUnits(query.PlayerId)
                .Select(u => new UnitDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    OwnerId = u.OwnerId,
                    X = u.Position.X,
                    Y = u.Position.Y,
                    CurrentHealth = u.Stats.CurrentHealth,
                    MaxHealth = u.Stats.MaxHealth,
                    AttackPower = u.Stats.AttackPower,
                    Defense = u.Stats.Defense,
                    MovementRange = u.Stats.MovementRange,
                    HasMovedThisTurn = u.HasMovedThisTurn,
                    HasActedThisTurn = u.HasActedThisTurn,
                    IsAlive = u.IsAlive
                })
                .ToList();

            return Result<IReadOnlyList<UnitDto>>.Success(units);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UnitDto>>.Failure($"Failed to get player units: {ex.Message}");
        }
    }

    public Result<bool> CanPlayerAct(CanPlayerActQuery query)
    {
        try
        {
            if (_board == null)
                return Result<bool>.Failure("Game has not been created");

            if (query == null)
                return Result<bool>.Failure("Query cannot be null");

            if (!_players.ContainsKey(query.PlayerId))
                return Result<bool>.Failure("Player not found");

            var playerUnits = _board.GetPlayerUnits(query.PlayerId);
            var canAct = playerUnits.Any(u => u.CanMove || u.CanAct);

            return Result<bool>.Success(canAct);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check if player can act: {ex.Message}");
        }
    }

    public Result<UnitDto> GetUnit(GetUnitQuery query)
    {
        try
        {
            if (_board == null)
                return Result<UnitDto>.Failure("Game has not been created");

            if (query == null)
                return Result<UnitDto>.Failure("Query cannot be null");

            var unit = _board.FindUnit(query.UnitId);
            if (unit == null)
                return Result<UnitDto>.Failure("Unit not found");

            var unitDto = new UnitDto
            {
                Id = unit.Id,
                Name = unit.Name,
                OwnerId = unit.OwnerId,
                X = unit.Position.X,
                Y = unit.Position.Y,
                CurrentHealth = unit.Stats.CurrentHealth,
                MaxHealth = unit.Stats.MaxHealth,
                AttackPower = unit.Stats.AttackPower,
                Defense = unit.Stats.Defense,
                MovementRange = unit.Stats.MovementRange,
                HasMovedThisTurn = unit.HasMovedThisTurn,
                HasActedThisTurn = unit.HasActedThisTurn,
                IsAlive = unit.IsAlive
            };

            return Result<UnitDto>.Success(unitDto);
        }
        catch (Exception ex)
        {
            return Result<UnitDto>.Failure($"Failed to get unit: {ex.Message}");
        }
    }
}