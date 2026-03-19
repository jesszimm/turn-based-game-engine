using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Application.Services;

public enum AiDifficulty
{
    Easy,
    Medium,
    Hard
}

public static class AiDecisionAction
{
    public const string Attack = "attack";
    public const string Move = "move";
    public const string Skip = "skip";
}

public sealed class AiDecision
{
    public string ActionType { get; init; } = AiDecisionAction.Skip;
    public Guid UnitId { get; init; }
    public Guid? TargetId { get; init; }
    public Position? TargetPosition { get; init; }
}

public sealed class AiDecisionState
{
    public AiDecisionState(
        Game game,
        AiDifficulty difficulty,
        char nextAiMoveUnitAbbreviation,
        Guid? focusTargetId)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        Difficulty = difficulty;
        NextAiMoveUnitAbbreviation = nextAiMoveUnitAbbreviation;
        FocusTargetId = focusTargetId;
    }

    public Game Game { get; }
    public AiDifficulty Difficulty { get; }
    public char NextAiMoveUnitAbbreviation { get; }
    public Guid? FocusTargetId { get; set; }
}
