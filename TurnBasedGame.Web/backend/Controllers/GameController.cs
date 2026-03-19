using Microsoft.AspNetCore.Mvc;
using TurnBasedGame.Web.Backend.Stores;
using TurnBasedGame.Web.Backend.Models;

namespace TurnBasedGame.Web.Backend.Controllers;

[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly GameStore _gameStore;

    public GameController(GameStore gameStore)
    {
        _gameStore = gameStore;
    }

    [HttpPost("create")]
    public ActionResult Create()
    {
        var gameId = _gameStore.CreateGame();
        return Ok(new { gameId });
    }

    [HttpGet("{id}")]
    public ActionResult<GameStateDto> Get(string id)
    {
        var game = _gameStore.GetGame(id);
        if (game == null)
            return NotFound();

        return Ok(MapToDto(game));
    }

    private static GameStateDto MapToDto(TurnBasedGame.Domain.Entities.Game game)
    {
        var units = game.Board.GetAllUnits()
            .Select(unit => new UnitDto
            {
                Id = unit.Id.ToString(),
                X = unit.Position.X,
                Y = unit.Position.Y,
                Health = unit.Stats.CurrentHealth
            })
            .ToList();

        return new GameStateDto { Units = units };
    }
}
