using Microsoft.AspNetCore.Mvc;
using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.Web.Backend.Models;
using TurnBasedGame.Web.Backend.Stores;

namespace TurnBasedGame.Web.Backend.Controllers;

[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly GameStore _gameStore;
    private readonly ILogger<GameController> _logger;

    public GameController(GameStore gameStore, ILogger<GameController> logger)
    {
        _gameStore = gameStore;
        _logger = logger;
    }

    [HttpPost("create")]
    public ActionResult Create([FromBody] CreateGameRequestDto? request)
    {
        var difficulty = ParseDifficulty(request?.Difficulty);
        if (difficulty == null)
            return BadRequest("Invalid difficulty");

        var gameId = _gameStore.CreateGame(difficulty.Value);
        return Ok(new { gameId });
    }

    [HttpGet("{id}")]
    public ActionResult<GameStateDto> Get(string id)
    {
        if (!TryGetSession(id, out var session, out var game))
            return NotFound();

        return Ok(MapToDto(game, session));
    }

    [HttpPost("{id}/move")]
    public ActionResult<GameStateDto> Move(string id, [FromBody] MoveRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UnitId))
            return BadRequest("unitId is required");

        if (!Guid.TryParse(request.UnitId, out var unitId))
            return BadRequest("unitId must be a GUID");

        if (!TryGetSession(id, out var session, out var game))
            return NotFound();
        var service = session.Service;

        _logger.LogInformation("Move request unitId={UnitId}. Units: {Units}",
            unitId, DescribeUnits(game));

        var unit = game.Board.FindUnit(unitId);
        if (unit == null || !unit.IsAlive)
            return BadRequest("Unit no longer exists");

        if (IsGameOver(game, session, out var winner))
            return Ok(MapToDto(game, session, winner));

        var result = service.MoveUnit(new MoveUnitCommand(unitId, request.X, request.Y));
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage ?? "Invalid move");

        if (IsGameOver(game, session, out winner))
            return Ok(MapToDto(game, session, winner));

        var endTurn = service.EndTurn(new EndTurnCommand());
        if (endTurn.IsFailure)
        {
            if (IsGameOver(game, session, out winner))
                return Ok(MapToDto(game, session, winner));

            return BadRequest(endTurn.ErrorMessage ?? "Failed to end player turn");
        }

        if (!IsGameOver(game, session, out winner))
        {
            ExecuteAiTurn(session, game);
        }

        return Ok(MapToDto(game, session, winner));
    }

    [HttpPost("{id}/attack")]
    public ActionResult<GameStateDto> Attack(string id, [FromBody] AttackRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AttackerId) || string.IsNullOrWhiteSpace(request.TargetId))
            return BadRequest("attackerId and targetId are required");

        if (!Guid.TryParse(request.AttackerId, out var attackerId))
            return BadRequest("attackerId must be a GUID");

        if (!Guid.TryParse(request.TargetId, out var targetId))
            return BadRequest("targetId must be a GUID");

        if (!TryGetSession(id, out var session, out var game))
            return NotFound();
        var service = session.Service;

        _logger.LogInformation("Attack request attackerId={AttackerId} targetId={TargetId}. Units: {Units}",
            attackerId, targetId, DescribeUnits(game));

        var attacker = game.Board.FindUnit(attackerId);
        var target = game.Board.FindUnit(targetId);
        if (attacker == null || !attacker.IsAlive || target == null || !target.IsAlive)
            return BadRequest("Unit no longer exists");

        if (IsGameOver(game, session, out var winner))
            return Ok(MapToDto(game, session, winner));

        var result = service.AttackUnit(new AttackUnitCommand(attackerId, targetId));
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage ?? "Invalid attack");

        if (IsGameOver(game, session, out winner))
            return Ok(MapToDto(game, session, winner));

        var endTurn = service.EndTurn(new EndTurnCommand());
        if (endTurn.IsFailure)
        {
            if (IsGameOver(game, session, out winner))
                return Ok(MapToDto(game, session, winner));

            return BadRequest(endTurn.ErrorMessage ?? "Failed to end player turn");
        }

        if (!IsGameOver(game, session, out winner))
        {
            ExecuteAiTurn(session, game);
        }

        return Ok(MapToDto(game, session, winner));
    }

    private static void ExecuteAiTurn(GameSession session, TurnBasedGame.Domain.Entities.Game game)
    {
        var service = session.Service;
        var aiService = new AiDecisionService();
        var decisionState = new AiDecisionState(game, session.Difficulty, session.NextAiMoveUnitAbbreviation, session.FocusTargetId);
        var decision = aiService.Decide(decisionState);
        session.FocusTargetId = decisionState.FocusTargetId;

        if (decision.ActionType == AiDecisionAction.Attack && decision.TargetId != null)
        {
            _ = service.AttackUnit(new AttackUnitCommand(decision.UnitId, decision.TargetId.Value));
        }
        else if (decision.ActionType == AiDecisionAction.Move && decision.TargetPosition != null)
        {
            _ = service.MoveUnit(new MoveUnitCommand(decision.UnitId, decision.TargetPosition.X, decision.TargetPosition.Y));
            session.NextAiMoveUnitAbbreviation = session.NextAiMoveUnitAbbreviation == 'W' ? 'S' : 'W';
        }

        _ = service.EndTurn(new EndTurnCommand());
    }

    private static GameStateDto MapToDto(
        TurnBasedGame.Domain.Entities.Game game,
        GameSession session,
        string? winnerOverride = null)
    {
        var units = game.Board.GetAllUnits()
            .Select(unit => new UnitDto
            {
                Id = unit.Id.ToString(),
                Name = unit.Name,
                X = unit.Position.X,
                Y = unit.Position.Y,
                Health = unit.Stats.CurrentHealth,
                AttackPower = unit.Stats.AttackPower,
                Owner = unit.OwnerId == game.Player1.Id ? game.Player1.Name : game.Player2.Name,
                MovementRange = unit.Stats.MovementRange
            })
            .Where(unit => unit.Health > 0)
            .ToList();

        var maxTurns = session.Difficulty == AiDifficulty.Hard ? 30 : (int?)null;
        var isGameOver = IsGameOver(game, session, out var winner);

        return new GameStateDto
        {
            CurrentPlayer = game.CurrentPlayer.Name,
            Units = units,
            ControlTileEnabled = game.ControlTileEnabled,
            ControlTileX = game.ControlTileEnabled ? game.ControlPosition.X : null,
            ControlTileY = game.ControlTileEnabled ? game.ControlPosition.Y : null,
            TurnNumber = game.TurnNumber,
            MaxTurns = maxTurns,
            IsGameOver = isGameOver,
            Winner = winnerOverride ?? winner
        };
    }

    private bool TryGetSession(string id, out GameSession session, out TurnBasedGame.Domain.Entities.Game game)
    {
        session = null!;
        game = null!;

        var found = _gameStore.GetSession(id);
        if (found?.Service.CurrentGame == null)
            return false;

        session = found;
        game = found.Service.CurrentGame;
        return true;
    }

    private static string DescribeUnits(TurnBasedGame.Domain.Entities.Game game)
    {
        return string.Join(", ", game.Board.GetAllUnits().Select(unit =>
            $"{unit.Id}:{unit.Name}:HP{unit.Stats.CurrentHealth}"));
    }

    private static bool IsGameOver(TurnBasedGame.Domain.Entities.Game game, GameSession session, out string? winnerName)
    {
        winnerName = game.GetWinner()?.Name;
        if (winnerName != null)
            return true;

        if (session.Difficulty != AiDifficulty.Hard)
            return false;

        const int maxTurns = 30;
        if (game.TurnNumber < maxTurns)
            return false;

        var player1Hp = game.GetPlayer1Units().Sum(unit => unit.Stats.CurrentHealth);
        var player2Hp = game.GetPlayer2Units().Sum(unit => unit.Stats.CurrentHealth);

        if (player1Hp == player2Hp)
        {
            var unitOnControl = game.ControlTileEnabled
                ? game.Board.GetUnitAtPosition(game.ControlPosition)
                : null;
            if (unitOnControl != null)
            {
                winnerName = unitOnControl.OwnerId == game.Player1.Id ? game.Player1.Name : game.Player2.Name;
            }
            else
            {
                winnerName = game.Player2.Name;
            }
            return true;
        }

        winnerName = player1Hp > player2Hp ? game.Player1.Name : game.Player2.Name;
        return true;
    }

    private static AiDifficulty? ParseDifficulty(string? difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty))
            return AiDifficulty.Easy;

        return Enum.TryParse(difficulty, ignoreCase: true, out AiDifficulty parsed)
            ? parsed
            : null;
    }
}
