namespace TurnBasedGame.Web.Backend.Models;

public sealed class GameStateDto
{
    public string CurrentPlayer { get; set; } = string.Empty;
    public List<UnitDto> Units { get; set; } = new();
}
