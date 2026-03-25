namespace TurnBasedGame.Web.Backend.Models;

public sealed class MoveRequestDto
{
    public string UnitId { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
}
