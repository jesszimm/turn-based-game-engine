using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;

namespace TurnBasedGame.Web.Backend.Stores;

public sealed class GameStore
{
    private readonly Dictionary<string, GameSession> _games = new();

    public string CreateGame(AiDifficulty difficulty)
    {
        var gameId = Guid.NewGuid().ToString("N");
        var service = new GameService();
        var createResult = service.CreateGame(new CreateGameCommand
        {
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            BoardWidth = 5,
            BoardHeight = 5
        });

        if (createResult.IsFailure || service.CurrentGame == null)
            throw new InvalidOperationException("Failed to create game");

        SeedUnits(service);
        _games[gameId] = new GameSession(service, difficulty);
        return gameId;
    }

    public GameSession? GetSession(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return _games.TryGetValue(id, out var game) ? game : null;
    }

    private static void SeedUnits(GameService service)
    {
        var game = service.CurrentGame!;
        var player1 = game.Player1;
        var player2 = game.Player2;

        var p1 = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "Warrior",
            PlayerId = player1.Id,
            X = 0,
            Y = 0,
            MaxHealth = 55,
            AttackPower = 24,
            Defense = 0,
            MovementRange = 2
        });

        if (p1.IsFailure)
            throw new InvalidOperationException($"Failed to place P1 unit: {p1.ErrorMessage}");

        var p2 = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "Warrior",
            PlayerId = player2.Id,
            X = 4,
            Y = 4,
            MaxHealth = 55,
            AttackPower = 24,
            Defense = 0,
            MovementRange = 2
        });

        if (p2.IsFailure)
            throw new InvalidOperationException($"Failed to place P2 unit: {p2.ErrorMessage}");

        var p1Scout = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "Scout",
            PlayerId = player1.Id,
            X = 1,
            Y = 0,
            MaxHealth = 40,
            AttackPower = 18,
            Defense = 0,
            MovementRange = 3
        });

        if (p1Scout.IsFailure)
            throw new InvalidOperationException($"Failed to place P1 scout: {p1Scout.ErrorMessage}");

        var p2Scout = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "Scout",
            PlayerId = player2.Id,
            X = 3,
            Y = 4,
            MaxHealth = 40,
            AttackPower = 18,
            Defense = 0,
            MovementRange = 3
        });

        if (p2Scout.IsFailure)
            throw new InvalidOperationException($"Failed to place P2 scout: {p2Scout.ErrorMessage}");
    }
}
