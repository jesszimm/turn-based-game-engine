using TurnBasedGame.Application.DTOs;

namespace TurnBasedGame.ConsoleUI.Renderers;

/// <summary>
/// Defines the contract for rendering the game to the console.
/// Abstraction allows for different rendering styles or testing with mock renderers.
/// </summary>
public interface IBoardRenderer
{
    /// <summary>
    /// Clears the console screen.
    /// </summary>
    void Clear();

    /// <summary>
    /// Renders the complete game state including board, units, and game info.
    /// </summary>
    /// <param name="gameState">Current game state to render.</param>
    void RenderGame(GameStateDto gameState);

    /// <summary>
    /// Renders the game board with highlighted valid move positions.
    /// </summary>
    /// <param name="gameState">Current game state.</param>
    /// <param name="validMoves">Valid positions to highlight.</param>
    void RenderGameWithHighlights(GameStateDto gameState, ValidMovesDto validMoves);

    /// <summary>
    /// Renders a success message.
    /// </summary>
    void RenderSuccess(string message);

    /// <summary>
    /// Renders an error message.
    /// </summary>
    void RenderError(string message);

    /// <summary>
    /// Renders help information showing available commands.
    /// </summary>
    void RenderHelp();

    /// <summary>
    /// Renders unit details.
    /// </summary>
    void RenderUnitDetails(UnitDto unit);

    /// <summary>
    /// Renders combat result.
    /// </summary>
    void RenderCombatResult(CombatResultDto combatResult, UnitDto? attacker, UnitDto? defender);

    /// <summary>
    /// Renders the game over screen with winner.
    /// </summary>
    void RenderGameOver(GameStateDto gameState);
}