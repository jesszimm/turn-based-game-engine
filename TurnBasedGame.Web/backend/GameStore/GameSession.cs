using TurnBasedGame.Application.Services;

namespace TurnBasedGame.Web.Backend.Stores;

public sealed class GameSession
{
    public GameSession(GameService service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public GameService Service { get; }
    public char NextAiMoveUnitAbbreviation { get; set; } = 'W';
    public Guid? FocusTargetId { get; set; }
}
