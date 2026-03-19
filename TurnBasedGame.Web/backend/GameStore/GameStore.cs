using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.Services;

namespace TurnBasedGame.Web.Backend.Stores;

public sealed class GameStore
{
    private readonly Dictionary<string, Game> _games = new();

    public string CreateGame()
    {
        var gameId = Guid.NewGuid().ToString("N");
        var game = new Game("Player 1", "Player 2", new CombatResolver());
        SeedUnits(game);
        _games[gameId] = game;
        return gameId;
    }

    public Game? GetGame(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return _games.TryGetValue(id, out var game) ? game : null;
    }

    private static void SeedUnits(Game game)
    {
        var player1 = game.Player1;
        var player2 = game.Player2;

        game.PlaceUnit(
            player1,
            "Warrior",
            new TurnBasedGame.Domain.ValueObjects.Position(0, 0),
            new TurnBasedGame.Domain.ValueObjects.UnitStats(55, 24, 0, 2));

        game.PlaceUnit(
            player2,
            "Warrior",
            new TurnBasedGame.Domain.ValueObjects.Position(4, 4),
            new TurnBasedGame.Domain.ValueObjects.UnitStats(55, 24, 0, 2));
    }
}
