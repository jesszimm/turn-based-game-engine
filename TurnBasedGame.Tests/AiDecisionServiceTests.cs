using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Tests;

public sealed class AiDecisionServiceTests
{
    [Fact]
    public void Decide_IsDeterministic_ForSameState()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        _ = PlaceUnit(service, game.Player2.Id, "Scout", 1, 1, 40, 12, 2);
        _ = PlaceUnit(service, game.Player1.Id, "Warrior", 1, 2, 50, 10, 2);

        AdvanceToPlayer2Turn(service);

        var aiService = new AiDecisionService();

        var stateA = new AiDecisionState(game, AiDifficulty.Easy, 'W', null);
        var stateB = new AiDecisionState(game, AiDifficulty.Easy, 'W', null);

        var decisionA = aiService.Decide(stateA);
        var decisionB = aiService.Decide(stateB);

        AssertEqualDecision(decisionA, decisionB);
    }

    [Fact]
    public void Decide_ReturnsValidMove_WhenNoAttackAvailable()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var aiUnit = PlaceUnit(service, game.Player2.Id, "Warrior", 0, 0, 55, 20, 2);
        _ = PlaceUnit(service, game.Player1.Id, "Scout", 4, 4, 40, 10, 2);

        AdvanceToPlayer2Turn(service);

        var aiService = new AiDecisionService();
        var state = new AiDecisionState(game, AiDifficulty.Easy, 'W', null);
        var decision = aiService.Decide(state);

        Assert.Equal(AiDecisionAction.Move, decision.ActionType);
        Assert.NotNull(decision.TargetPosition);
        Assert.Equal(aiUnit.Id, decision.UnitId);
        Assert.Contains(decision.TargetPosition!, game.Board.GetValidMovePositions(aiUnit));
    }

    [Fact]
    public void Decide_AttackDecision_IsValidForGameService()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var aiUnit = PlaceUnit(service, game.Player2.Id, "Scout", 1, 1, 40, 12, 2);
        var enemy = PlaceUnit(service, game.Player1.Id, "Warrior", 1, 2, 50, 10, 2);

        AdvanceToPlayer2Turn(service);

        var aiService = new AiDecisionService();
        var state = new AiDecisionState(game, AiDifficulty.Easy, 'W', null);
        var decision = aiService.Decide(state);

        Assert.Equal(AiDecisionAction.Attack, decision.ActionType);
        Assert.Equal(aiUnit.Id, decision.UnitId);
        Assert.Equal(enemy.Id, decision.TargetId);

        var result = service.AttackUnit(new AttackUnitCommand(decision.UnitId, decision.TargetId!.Value));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Decide_MoveDecision_IsValidForGameService()
    {
        var service = CreateBasicGame();
        var game = service.CurrentGame!;

        var aiUnit = PlaceUnit(service, game.Player2.Id, "Warrior", 0, 0, 55, 20, 2);
        _ = PlaceUnit(service, game.Player1.Id, "Scout", 4, 4, 40, 10, 2);

        AdvanceToPlayer2Turn(service);

        var aiService = new AiDecisionService();
        var state = new AiDecisionState(game, AiDifficulty.Easy, 'W', null);
        var decision = aiService.Decide(state);

        Assert.Equal(AiDecisionAction.Move, decision.ActionType);
        Assert.NotNull(decision.TargetPosition);

        var result = service.MoveUnit(new MoveUnitCommand(decision.UnitId, decision.TargetPosition!.X, decision.TargetPosition!.Y));
        Assert.True(result.IsSuccess);
        Assert.Equal(decision.TargetPosition, aiUnit.Position);
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

    private static void AssertEqualDecision(AiDecision left, AiDecision right)
    {
        Assert.Equal(left.ActionType, right.ActionType);
        Assert.Equal(left.UnitId, right.UnitId);
        Assert.Equal(left.TargetId, right.TargetId);
        Assert.Equal(left.TargetPosition, right.TargetPosition);
    }
}
