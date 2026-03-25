namespace TurnBasedGame.Web.Backend.Models;

public sealed class AttackRequestDto
{
    public string AttackerId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}
