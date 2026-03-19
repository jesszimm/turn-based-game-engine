using System.Reflection;
using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Tests;

public sealed class AiDecisionCharacterizationTests
{
    [Fact]
    public void ExecuteAiTurn_Attacks_WhenAdjacentEnemyExists()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var aiUnit = PlaceUnit(service, game.Player2.Id, "Scout", 1, 1, maxHealth: 40, attackPower: 12, moveRange: 2);
        var enemyUnit = PlaceUnit(service, game.Player1.Id, "Warrior", 1, 2, maxHealth: 50, attackPower: 10, moveRange: 2);

        AdvanceToPlayer2Turn(service);

        var preEnemyHp = enemyUnit.Stats.CurrentHealth;
        var preAiPosition = aiUnit.Position;

        var nextAiMoveUnitAbbreviation = 'W';
        Guid? focusTargetId = null;
        ExecuteAiTurn(service, game, AiDifficulty.Easy, ref nextAiMoveUnitAbbreviation, ref focusTargetId);

        var postEnemy = game.Board.FindUnit(enemyUnit.Id);
        Assert.NotNull(postEnemy);
        Assert.True(postEnemy!.Stats.CurrentHealth < preEnemyHp);
        Assert.Equal(preAiPosition, aiUnit.Position);
    }

    [Fact]
    public void ExecuteAiTurn_Moves_WhenNoAdjacentEnemyExists()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var aiUnit = PlaceUnit(service, game.Player2.Id, "Warrior", 0, 0, maxHealth: 55, attackPower: 20, moveRange: 2);
        _ = PlaceUnit(service, game.Player1.Id, "Scout", 4, 4, maxHealth: 40, attackPower: 10, moveRange: 2);

        AdvanceToPlayer2Turn(service);

        var nextAiMoveUnitAbbreviation = 'W';
        Guid? focusTargetId = null;
        ExecuteAiTurn(service, game, AiDifficulty.Easy, ref nextAiMoveUnitAbbreviation, ref focusTargetId);

        Assert.Equal(new Position(1, 1), aiUnit.Position);
    }

    [Fact]
    public void ExecuteAiTurn_PrioritizesScoutAttack_OnEasyDifficulty()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var scout = PlaceUnit(service, game.Player2.Id, "Scout", 1, 1, maxHealth: 40, attackPower: 25, moveRange: 2);
        var warrior = PlaceUnit(service, game.Player2.Id, "Warrior", 3, 1, maxHealth: 55, attackPower: 25, moveRange: 2);

        var defenderNearScout = PlaceUnit(service, game.Player1.Id, "Defender A", 1, 2, maxHealth: 10, attackPower: 5, moveRange: 1);
        var defenderNearWarrior = PlaceUnit(service, game.Player1.Id, "Defender B", 3, 2, maxHealth: 10, attackPower: 5, moveRange: 1);

        AdvanceToPlayer2Turn(service);

        var nextAiMoveUnitAbbreviation = 'W';
        Guid? focusTargetId = null;
        ExecuteAiTurn(service, game, AiDifficulty.Easy, ref nextAiMoveUnitAbbreviation, ref focusTargetId);

        Assert.Null(game.Board.FindUnit(defenderNearScout.Id));
        Assert.NotNull(game.Board.FindUnit(defenderNearWarrior.Id));
        Assert.Equal(new Position(1, 1), scout.Position);
        Assert.Equal(new Position(3, 1), warrior.Position);
    }

    private static GameService CreateBasicGame()
    {
        var service = new GameService();
        var createResult = service.CreateGame(new CreateGameCommand
        {
            Player1Name = "Alice",
            Player2Name = "CPU",
            BoardWidth = 5,
            BoardHeight = 5
        });

        Assert.True(createResult.IsSuccess);
        return service;
    }

    private static Unit PlaceUnit(
        GameService service,
        Guid playerId,
        string unitName,
        int x,
        int y,
        int maxHealth,
        int attackPower,
        int moveRange)
    {
        var game = service.CurrentGame!;
        var result = service.PlaceUnit(new PlaceUnitCommand
        {
            UnitName = unitName,
            PlayerId = playerId,
            X = x,
            Y = y,
            MaxHealth = maxHealth,
            AttackPower = attackPower,
            Defense = 0,
            MovementRange = moveRange
        });

        Assert.True(result.IsSuccess);
        return game.Board.FindUnit(result.Value)!;
    }

    private static void AdvanceToPlayer2Turn(GameService service)
    {
        var endTurn = service.EndTurn(new EndTurnCommand());
        Assert.True(endTurn.IsSuccess);
    }

    private static void ExecuteAiTurn(
        GameService service,
        Game game,
        AiDifficulty difficulty,
        ref char nextAiMoveUnitAbbreviation,
        ref Guid? focusTargetId)
    {
        var programType = typeof(TurnBasedGame.ConsoleUI.Program);
        var controlTileField = programType.GetField("_controlTileEnabled", BindingFlags.NonPublic | BindingFlags.Static);
        controlTileField!.SetValue(null, false);

        var method = programType.GetMethod("ExecuteAiTurn", BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object?[] { service, game, difficulty, nextAiMoveUnitAbbreviation, focusTargetId };

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());
            method!.Invoke(null, args);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        nextAiMoveUnitAbbreviation = (char)args[3]!;
        focusTargetId = (Guid?)args[4];
    }
}
