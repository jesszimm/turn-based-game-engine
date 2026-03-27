using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.Domain.Entities;

namespace TurnBasedGame.Web.Backend.Stores;

public sealed class GameSession
{
    public GameSession(GameService service, AiDifficulty difficulty)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
        Difficulty = difficulty;
    }

    public GameService Service { get; }
    public char NextAiMoveUnitAbbreviation { get; set; } = 'W';
    public Guid? FocusTargetId { get; set; }
    public AiDifficulty Difficulty { get; }

    public void ExecuteAiTurn(Game game)
    {
        var aiService = new AiDecisionService();
        var decisionState = new AiDecisionState(game, Difficulty, NextAiMoveUnitAbbreviation, FocusTargetId);
        var decision = aiService.Decide(decisionState);
        FocusTargetId = decisionState.FocusTargetId;

        if (decision.ActionType == AiDecisionAction.Attack && decision.TargetId != null)
        {
            _ = Service.AttackUnit(new AttackUnitCommand(decision.UnitId, decision.TargetId.Value));
        }
        else if (decision.ActionType == AiDecisionAction.Move && decision.TargetPosition != null)
        {
            _ = Service.MoveUnit(new MoveUnitCommand(decision.UnitId, decision.TargetPosition.X, decision.TargetPosition.Y));
            NextAiMoveUnitAbbreviation = NextAiMoveUnitAbbreviation == 'W' ? 'S' : 'W';
        }

        _ = Service.EndTurn(new EndTurnCommand());
    }
}
