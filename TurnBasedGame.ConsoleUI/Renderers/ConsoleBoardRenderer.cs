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
    public void RenderBoard(GameBoard board, Guid player1Id, Guid player2Id)
    {
        if (board == null)
            throw new ArgumentNullException(nameof(board));

        var yLabelWidth = Math.Max(2, board.Height.ToString().Length);
        var headerPadding = new string(' ', yLabelWidth + 3);
        var borderPadding = new string(' ', yLabelWidth + 2);

        System.Console.Write(headerPadding);
        for (int x = 0; x < board.Width; x++)
        {
            System.Console.Write($" {x + 1,2} ");
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

                char symbol;
                if (unit == null)
                {
                    symbol = '.';
                    System.Console.Write($" {symbol} |");
                }
                else if (unit.OwnerId == player1Id)
                {
                    symbol = GetUnitAbbreviation(unit);
                    WriteColoredSymbol(symbol, ConsoleColor.Blue);
                    System.Console.Write("|");
                }
                else if (unit.OwnerId == player2Id)
                {
                    symbol = GetUnitAbbreviation(unit);
                    WriteColoredSymbol(symbol, ConsoleColor.Red);
                    System.Console.Write("|");
                }
                else
                {
                    symbol = '?';
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

        System.Console.WriteLine($"{new string(' ', yLabelWidth)} Y");
        System.Console.WriteLine("        Colors: Blue = Player 1, Red = Player 2");
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
}
