namespace TurnBasedGame.Web.Backend.Models;

public sealed class GameStateDto
{
    public List<UnitDto> Units { get; set; } = new();
}
