using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;

namespace TurnBasedGame.Tests;

public sealed class GameServiceIntegrationTests
{
    [Fact]
    public void CreateGame_InitializesCurrentGameAndStartingPlayer()
    {
        var service = new GameService();

        var result = service.CreateGame(new CreateGameCommand
        {
            Player1Name = "Alice",
            Player2Name = "Bob",
            BoardWidth = 5,
            BoardHeight = 5
        });

        Assert.True(result.IsSuccess);
        Assert.True(service.HasActiveGame);
        Assert.NotNull(service.CurrentGame);
        Assert.Equal("Alice", service.GetCurrentPlayer()!.Name);
    }

    [Fact]
    public void MoveThenAttack_WithValidCommands_UpdatesBoardAndDamage()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var attacker = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "P1 Unit",
            PlayerId = game.Player1.Id,
            X = 0,
            Y = 0,
            MaxHealth = 100,
            AttackPower = 30,
            Defense = 4,
            MovementRange = 2
        });

        var defender = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "P2 Unit",
            PlayerId = game.Player2.Id,
            X = 2,
            Y = 0,
            MaxHealth = 100,
            AttackPower = 10,
            Defense = 2,
            MovementRange = 2
        });

        Assert.True(attacker.IsSuccess);
        Assert.True(defender.IsSuccess);

        var moveResult = service.MoveUnit(new MoveUnitCommand(attacker.Value, 1, 0));
        Assert.True(moveResult.IsSuccess);

        var attackResult = service.AttackUnit(new AttackUnitCommand(attacker.Value, defender.Value));
        Assert.True(attackResult.IsSuccess);
        Assert.True(attackResult.Value > 0);

        var defenderUnit = game.Board.FindUnit(defender.Value);
        Assert.NotNull(defenderUnit);
        Assert.True(defenderUnit!.Stats.CurrentHealth < defenderUnit.Stats.MaxHealth);
    }

    [Fact]
    public void AttackCanEndGame_WhenOnePlayerLosesAllUnits()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var attacker = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "P1 Finisher",
            PlayerId = game.Player1.Id,
            X = 0,
            Y = 0,
            MaxHealth = 100,
            AttackPower = 999,
            Defense = 0,
            MovementRange = 1
        });

        var defender = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = "P2 Last Unit",
            PlayerId = game.Player2.Id,
            X = 1,
            Y = 0,
            MaxHealth = 20,
            AttackPower = 1,
            Defense = 0,
            MovementRange = 1
        });

        Assert.True(attacker.IsSuccess);
        Assert.True(defender.IsSuccess);

        var attackResult = service.AttackUnit(new AttackUnitCommand(attacker.Value, defender.Value));
        Assert.True(attackResult.IsSuccess);
        Assert.True(service.IsGameOver());
        Assert.Equal(game.Player1.Id, service.GetWinner()!.Id);
    }

    [Fact]
    public void EndTurn_SwitchesCurrentPlayer()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        Assert.Equal(game.Player1.Id, service.GetCurrentPlayer()!.Id);

        var endTurnResult = service.EndTurn(new EndTurnCommand());
        Assert.True(endTurnResult.IsSuccess);
        Assert.Equal(game.Player2.Id, service.GetCurrentPlayer()!.Id);
    }

    private static GameService CreateBasicGame()
    {
        var service = new GameService();
        var createResult = service.CreateGame(new CreateGameCommand
        {
            Player1Name = "Alice",
            Player2Name = "Bob",
            BoardWidth = 5,
            BoardHeight = 5
        });

        Assert.True(createResult.IsSuccess);
        return service;
    }
}
