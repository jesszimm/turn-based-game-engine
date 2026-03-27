namespace TurnBasedGame.Web.Backend.Models;

public sealed class GameStateDto
{
    public string CurrentPlayer { get; set; } = string.Empty;
    public List<UnitDto> Units { get; set; } = new();
    public bool ControlTileEnabled { get; set; }
    public int? ControlTileX { get; set; }
    public int? ControlTileY { get; set; }
    public int TurnNumber { get; set; }
    public int? MaxTurns { get; set; }
    public bool IsGameOver { get; set; }
    public string? Winner { get; set; }
}
