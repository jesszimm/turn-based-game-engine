namespace TurnBasedGame.Web.Backend.Models;

public sealed class UnitDto
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Health { get; set; }
}
