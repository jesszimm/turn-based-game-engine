using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.ConsoleUI.Renderers;

/// <summary>
/// Renders a tactical grid with 1-indexed coordinates.
/// </summary>
public sealed class ConsoleBoardRenderer 
{
    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public void Clear()
    {
        System.Console.Clear();
    }

    /// <summary>
    /// Renders the game board as a tactical grid.
    /// </summary>
    public void RenderBoard(
        GameBoard board,
        Guid player1Id,
        string player1Name,
        Guid player2Id,
        string player2Name,
        Position? highlightedPosition = null,
        bool showControlTile = false)
    {
        if (board == null)
            throw new ArgumentNullException(nameof(board));

        var yLabelWidth = Math.Max(2, board.Height.ToString().Length);
        var borderPadding = new string(' ', yLabelWidth + 1);
        var controlX = board.Width / 2;
        var controlY = board.Height / 2;

        // X-axis tick labels, centered over each column cell.
        System.Console.Write(borderPadding);
        for (int x = 0; x < board.Width; x++)
        {
            System.Console.Write($" {(x + 1),2} ");
        }
        System.Console.Write(" X");
        System.Console.WriteLine();

        System.Console.Write($"{borderPadding}+");
        for (int x = 0; x < board.Width; x++)
        {
            System.Console.Write("---+");
        }
        System.Console.WriteLine();

        for (int y = 0; y < board.Height; y++)
        {
            System.Console.Write($"{(y + 1).ToString().PadLeft(yLabelWidth)} |");

            for (int x = 0; x < board.Width; x++)
            {
                var position = new Position(x, y);
                var unit = board.GetUnitAtPosition(position);
                var isHighlighted =
                    highlightedPosition != null &&
                    highlightedPosition.X == x &&
                    highlightedPosition.Y == y;
                var isControlTile = showControlTile && x == controlX && y == controlY;

                char symbol;
                if (unit == null)
                {
                    symbol = isControlTile ? 'C' : '.';
                    if (isHighlighted)
                        WriteHighlightedCell(symbol, isControlTile ? ConsoleColor.Green : ConsoleColor.Yellow, ConsoleColor.DarkBlue);
                    else if (isControlTile)
                        WriteColoredCell(symbol, ConsoleColor.Green);
                    else
                        System.Console.Write($" {symbol} |");
                }
                else if (unit.OwnerId == player1Id)
                {
                    symbol = GetUnitAbbreviation(unit);
                    if (isHighlighted)
                        WriteHighlightedCell(symbol, ConsoleColor.Blue, ConsoleColor.DarkYellow);
                    else
                    {
                        WriteColoredSymbol(symbol, ConsoleColor.Blue);
                        System.Console.Write("|");
                    }
                }
                else if (unit.OwnerId == player2Id)
                {
                    symbol = GetUnitAbbreviation(unit);
                    if (isHighlighted)
                        WriteHighlightedCell(symbol, ConsoleColor.Red, ConsoleColor.DarkYellow);
                    else
                    {
                        WriteColoredSymbol(symbol, ConsoleColor.Red);
                        System.Console.Write("|");
                    }
                }
                else
                {
                    symbol = '?';
                    if (isHighlighted)
                        WriteHighlightedCell(symbol, ConsoleColor.White, ConsoleColor.DarkYellow);
                    else
                        System.Console.Write($" {symbol} |");
                }
            }

            System.Console.WriteLine();
            System.Console.Write($"{borderPadding}+");
            for (int x = 0; x < board.Width; x++)
            {
                System.Console.Write("---+");
            }
            System.Console.WriteLine();
        }

        WriteLineColored($"{new string(' ', yLabelWidth)} Y", ConsoleColor.DarkGray);
        System.Console.Write("        Colors: ");
        WriteColored("Blue", ConsoleColor.Blue);
        System.Console.Write(" = ");
        WriteColored(player1Name, ConsoleColor.Blue);
        System.Console.Write(", ");
        WriteColored("Red", ConsoleColor.Red);
        System.Console.Write(" = ");
        WriteColored(player2Name, ConsoleColor.Red);
        System.Console.WriteLine();
        if (showControlTile)
        {
            System.Console.Write("        ");
            WriteColored("Green", ConsoleColor.Green);
            System.Console.WriteLine(" = Control tile");
        }
        System.Console.WriteLine();
    }

    private static char GetUnitAbbreviation(Unit unit)
    {
        var first = unit.Name.FirstOrDefault(char.IsLetterOrDigit);
        return first == default ? '?' : char.ToUpperInvariant(first);
    }

    private static void WriteColoredSymbol(char symbol, ConsoleColor color)
    {
        System.Console.Write(" ");
        var previousColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(symbol);
        System.Console.ForegroundColor = previousColor;
        System.Console.Write(" ");
    }

    private static void WriteHighlightedCell(char symbol, ConsoleColor foreground, ConsoleColor background)
    {
        var previousForeground = System.Console.ForegroundColor;
        var previousBackground = System.Console.BackgroundColor;

        System.Console.ForegroundColor = foreground;
        System.Console.BackgroundColor = background;
        System.Console.Write($" {symbol} ");

        System.Console.ForegroundColor = previousForeground;
        System.Console.BackgroundColor = previousBackground;
        System.Console.Write("|");
    }

    private static void WriteColoredCell(char symbol, ConsoleColor color)
    {
        System.Console.Write(" ");
        var previousColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(symbol);
        System.Console.ForegroundColor = previousColor;
        System.Console.Write(" |");
    }

    private static void WriteColored(string text, ConsoleColor color)
    {
        var previousColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(text);
        System.Console.ForegroundColor = previousColor;
    }

    private static void WriteLineColored(string text, ConsoleColor color)
    {
        WriteColored(text, color);
        System.Console.WriteLine();
    }

}
