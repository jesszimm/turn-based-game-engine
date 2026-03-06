using TurnBasedGame.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnBasedGame.ConsoleUI.Renderers;

/// <summary>
/// Renders the game to the console using ASCII art and colors.
/// Single class responsible for all console output.
/// </summary>
public sealed class ConsoleBoardRenderer : IBoardRenderer
{
    private const string HorizontalBorder = "─";
    private const string VerticalBorder = "│";
    private const string TopLeftCorner = "┌";
    private const string TopRightCorner = "┐";
    private const string BottomLeftCorner = "└";
    private const string BottomRightCorner = "┘";
    private const string CrossBorder = "┼";
    private const string TopTee = "┬";
    private const string BottomTee = "┴";
    private const string LeftTee = "├";
    private const string RightTee = "┤";

    private readonly int _cellWidth = 5;

    public void Clear()
    {
        System.Console.Clear();
    }

    public void RenderGame(GameStateDto gameState)
    {
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));

        Clear();
        RenderHeader(gameState);
        RenderBoard(gameState, null);
        RenderPlayerList(gameState);
    }

    public void RenderGameWithHighlights(GameStateDto gameState, ValidMovesDto validMoves)
    {
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));

        Clear();
        RenderHeader(gameState);
        RenderBoard(gameState, validMoves);
    }

    public void RenderSuccess(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"✓ {message}");
        System.Console.ResetColor();
    }

    public void RenderError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"✗ {message}");
        System.Console.ResetColor();
    }

    public void RenderHelp()
    {
        System.Console.WriteLine("Available Commands:");
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine("  state              - Show current game state");
        System.Console.WriteLine("  units              - List all your units");
        System.Console.WriteLine("  moves <unit-id>    - Show valid moves for a unit");
        System.Console.WriteLine("  move <id> <x> <y>  - Move unit to position");
        System.Console.WriteLine("  attack <id1> <id2> - Attack with unit 1 to unit 2");
        System.Console.WriteLine("  end                - End your turn");
        System.Console.WriteLine("  help               - Show this help message");
        System.Console.WriteLine("  quit               - Exit the game");
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine();
        RenderLegend();
    }

    public void RenderUnitDetails(UnitDto unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        System.Console.WriteLine("Unit Details:");
        System.Console.WriteLine("───────────────────────────────────────────────────────");
        System.Console.WriteLine($"  Name: {unit.Name}");
        System.Console.WriteLine($"  Position: ({unit.X}, {unit.Y})");
        System.Console.WriteLine($"  Health: {unit.CurrentHealth}/{unit.MaxHealth}");
        System.Console.WriteLine($"  Attack: {unit.AttackPower}");
        System.Console.WriteLine($"  Defense: {unit.Defense}");
        System.Console.WriteLine($"  Movement Range: {unit.MovementRange}");

        var canMove = !unit.HasMovedThisTurn ? "Yes" : "No";
        var canAct = !unit.HasActedThisTurn ? "Yes" : "No";

        System.Console.ForegroundColor = !unit.HasMovedThisTurn ? ConsoleColor.Green : ConsoleColor.Red;
        System.Console.WriteLine($"  Can Move: {canMove}");
        System.Console.ForegroundColor = !unit.HasActedThisTurn ? ConsoleColor.Green : ConsoleColor.Red;
        System.Console.WriteLine($"  Can Act: {canAct}");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }

    public void RenderCombatResult(CombatResultDto combatResult, UnitDto? attacker, UnitDto? defender)
    {
        if (combatResult == null)
            throw new ArgumentNullException(nameof(combatResult));

        System.Console.WriteLine("Combat Result:");
        System.Console.WriteLine("───────────────────────────────────────────────────────");

        if (attacker != null)
            System.Console.WriteLine($"  Attacker: {attacker.Name}");

        if (defender != null)
            System.Console.WriteLine($"  Defender: {defender.Name}");

        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"  Damage Dealt: {combatResult.DamageDealt}");
        System.Console.ResetColor();

        System.Console.WriteLine($"  Defender Health: {combatResult.DefenderHealthRemaining}");

        if (combatResult.DefenderDefeated)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("  *** UNIT DEFEATED ***");
            System.Console.ResetColor();
        }

        System.Console.WriteLine();
    }

    public void RenderGameOver(GameStateDto gameState)
    {
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));

        Clear();

        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine("                    GAME OVER!                         ");
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.ResetColor();
        System.Console.WriteLine();

        var winner = gameState.Players.FirstOrDefault(p => p.IsActive);
        if (winner != null)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"  Winner: {winner.Name}");
            System.Console.ResetColor();
            System.Console.WriteLine($"  Remaining Units: {winner.UnitCount}");
        }
        else
        {
            System.Console.WriteLine("  No winner - all players defeated!");
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"  Total Turns: {gameState.TurnNumber}");
        System.Console.WriteLine();

        RenderPlayerList(gameState);
    }

    private void RenderHeader(GameStateDto gameState)
    {
        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine($"  Turn {gameState.TurnNumber}");

        var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == gameState.CurrentPlayerId);
        if (currentPlayer != null)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"  Current Player: {currentPlayer.Name}");
            System.Console.ForegroundColor = ConsoleColor.Gray;
            System.Console.WriteLine($"  Units: {currentPlayer.UnitCount}");
        }

        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }

    private void RenderBoard(GameStateDto gameState, ValidMovesDto? validMoves)
    {
        var highlightedPositions = validMoves?.ValidPositions
            .Select(p => (p.X, p.Y))
            .ToHashSet() ?? new HashSet<(int, int)>();

        RenderColumnHeaders(gameState.BoardWidth);
        RenderTopBorder(gameState.BoardWidth);

        for (int y = 0; y < gameState.BoardHeight; y++)
        {
            RenderRow(gameState, y, highlightedPositions);

            if (y < gameState.BoardHeight - 1)
                RenderRowSeparator(gameState.BoardWidth);
            else
                RenderBottomBorder(gameState.BoardWidth);
        }

        System.Console.WriteLine();
    }

    private void RenderColumnHeaders(int width)
    {
        System.Console.Write("    ");
        for (int x = 0; x < width; x++)
        {
            System.Console.Write($"  {x}  ");
        }
        System.Console.WriteLine();
    }

    private void RenderTopBorder(int width)
    {
        System.Console.Write("   " + TopLeftCorner);
        for (int x = 0; x < width; x++)
        {
            System.Console.Write(new string(HorizontalBorder[0], _cellWidth));
            if (x < width - 1)
                System.Console.Write(TopTee);
        }
        System.Console.WriteLine(TopRightCorner);
    }

    private void RenderBottomBorder(int width)
    {
        System.Console.Write("   " + BottomLeftCorner);
        for (int x = 0; x < width; x++)
        {
            System.Console.Write(new string(HorizontalBorder[0], _cellWidth));
            if (x < width - 1)
                System.Console.Write(BottomTee);
        }
        System.Console.WriteLine(BottomRightCorner);
    }

    private void RenderRowSeparator(int width)
    {
        System.Console.Write("   " + LeftTee);
        for (int x = 0; x < width; x++)
        {
            System.Console.Write(new string(HorizontalBorder[0], _cellWidth));
            if (x < width - 1)
                System.Console.Write(CrossBorder);
        }
        System.Console.WriteLine(RightTee);
    }

    private void RenderRow(GameStateDto gameState, int y, HashSet<(int, int)> highlightedPositions)
    {
        System.Console.Write($" {y} ");

        for (int x = 0; x < gameState.BoardWidth; x++)
        {
            System.Console.Write(VerticalBorder);

            var tile = gameState.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);
            var unit = tile?.IsOccupied == true
                ? gameState.Units.FirstOrDefault(u => u.Id == tile.OccupyingUnitId)
                : null;

            var isHighlighted = highlightedPositions.Contains((x, y));

            RenderCell(tile, unit, isHighlighted);
        }

        System.Console.WriteLine(VerticalBorder);
    }

    private void RenderCell(TileDto? tile, UnitDto? unit, bool isHighlighted)
    {
        if (tile == null)
        {
            System.Console.Write(new string(' ', _cellWidth));
            return;
        }

        if (isHighlighted)
        {
            System.Console.BackgroundColor = ConsoleColor.DarkGreen;
        }

        if (unit != null)
        {
            RenderUnit(unit);
        }
        else
        {
            RenderTerrain(tile);
        }

        System.Console.ResetColor();
    }

    private void RenderUnit(UnitDto unit)
    {
        var ownerIndex = unit.OwnerId.GetHashCode() % 6;
        System.Console.ForegroundColor = ownerIndex switch
        {
            0 => ConsoleColor.Cyan,
            1 => ConsoleColor.Yellow,
            2 => ConsoleColor.Magenta,
            3 => ConsoleColor.Green,
            4 => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        var symbol = unit.Name.Length > 0 ? unit.Name[0] : 'U';
        var healthBar = GetHealthBar(unit.CurrentHealth, unit.MaxHealth);

        System.Console.Write($" {symbol}{healthBar} ");
    }

    private string GetHealthBar(int current, int max)
    {
        var percentage = (double)current / max;
        if (percentage > 0.66) return "█";
        if (percentage > 0.33) return "▓";
        if (percentage > 0) return "░";
        return "X";
    }

    private void RenderTerrain(TileDto tile)
    {
        var (symbol, color) = tile.Terrain switch
        {
            "Plains" => ("  .  ", ConsoleColor.Gray),
            "Forest" => ("  ♣  ", ConsoleColor.DarkGreen),
            "Mountain" => ("  ▲  ", ConsoleColor.DarkGray),
            "Water" => ("  ≈  ", ConsoleColor.Blue),
            _ => ("  ?  ", ConsoleColor.White)
        };

        System.Console.ForegroundColor = color;
        System.Console.Write(symbol);
    }

    private void RenderPlayerList(GameStateDto gameState)
    {
        System.Console.WriteLine("Players:");
        System.Console.WriteLine("───────────────────────────────────────────────────────");

        foreach (var player in gameState.Players)
        {
            var statusColor = player.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
            var status = player.IsActive ? "Active" : "Defeated";

            System.Console.Write($"  {player.Name}: ");
            System.Console.ForegroundColor = statusColor;
            System.Console.Write(status);
            System.Console.ResetColor();
            System.Console.WriteLine($" ({player.UnitCount} units)");
        }

        System.Console.WriteLine();
    }

    private void RenderLegend()
    {
        System.Console.WriteLine("Legend:");
        System.Console.WriteLine("───────────────────────────────────────────────────────");

        System.Console.Write("  ");
        System.Console.ForegroundColor = ConsoleColor.Gray;
        System.Console.Write(".");
        System.Console.ResetColor();
        System.Console.WriteLine(" = Plains");

        System.Console.Write("  ");
        System.Console.ForegroundColor = ConsoleColor.DarkGreen;
        System.Console.Write("♣");
        System.Console.ResetColor();
        System.Console.WriteLine(" = Forest (+1 Defense)");

        System.Console.Write("  ");
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.Write("▲");
        System.Console.ResetColor();
        System.Console.WriteLine(" = Mountain (+2 Defense)");

        System.Console.Write("  ");
        System.Console.ForegroundColor = ConsoleColor.Blue;
        System.Console.Write("≈");
        System.Console.ResetColor();
        System.Console.WriteLine(" = Water (Impassable)");

        System.Console.WriteLine();
        System.Console.WriteLine("  Units shown as: [First Letter][Health Bar]");
        System.Console.WriteLine("  Health: █ = High, ▓ = Medium, ░ = Low, X = Dead");
        System.Console.WriteLine();
    }
}