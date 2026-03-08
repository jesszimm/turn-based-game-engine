using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.ConsoleUI.Renderers;
using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.ConsoleUI;

/// <summary>
/// Console gameplay loop for the tactical grid game.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var renderer = new ConsoleBoardRenderer();
        var gameService = new GameService();

        renderer.Clear();
        WriteLineColored("=== Tactical Grid Game ===", ConsoleColor.Cyan);
        System.Console.WriteLine();

        var player1Name = ReadRequiredString("Enter Player 1 name: ");
        var player2Name = ReadRequiredString("Enter Player 2 name: ");

        var createResult = gameService.CreateGame(new CreateGameCommand
        {
            Player1Name = player1Name,
            Player2Name = player2Name,
            BoardWidth = 5,
            BoardHeight = 5
        });

        if (createResult.IsFailure || gameService.CurrentGame == null)
        {
            WriteLineColored($"Failed to create game: {createResult.ErrorMessage}", ConsoleColor.Red);
            return;
        }

        var game = gameService.CurrentGame;
        if (!PlaceStartingUnits(gameService, game))
        {
            WriteLineColored("Failed to place starting units.", ConsoleColor.Red);
            return;
        }

        ShowUnitGuide(player1Name, player2Name);
        ShowRulesSheet();
        WriteLineColored("Press Enter to start the match...", ConsoleColor.DarkCyan);
        System.Console.ReadLine();

        while (!gameService.IsGameOver())
        {
            var actionCompleted = false;
            while (!actionCompleted && !gameService.IsGameOver())
            {
                renderer.Clear();
                RenderBoardAndTurnInfo(renderer, gameService, game, player1Name, player2Name);

                var selectedUnit = SelectCurrentPlayerUnit(gameService);
                if (selectedUnit == null)
                    break;

                var action = ReadActionChoice();
                actionCompleted = ExecuteAction(
                    renderer,
                    gameService,
                    game,
                    selectedUnit,
                    action,
                    player1Name,
                    player2Name);

                if (!actionCompleted)
                {
                    WriteLineColored("Try again. Press Enter to continue.", ConsoleColor.Yellow);
                    System.Console.ReadLine();
                }
            }

            if (gameService.IsGameOver())
                break;

            var endResult = gameService.EndTurn(new EndTurnCommand());
            if (endResult.IsFailure)
            {
                WriteLineColored($"Could not end turn: {endResult.ErrorMessage}", ConsoleColor.Red);
                break;
            }

            WriteLineColored("Turn ended. Press Enter to continue.", ConsoleColor.DarkCyan);
            System.Console.ReadLine();
        }

        renderer.Clear();
        WriteLineColored("=== GAME OVER ===", ConsoleColor.Magenta);
        renderer.RenderBoard(game.Board, game.Player1.Id, player1Name, game.Player2.Id, player2Name);

        var winner = gameService.GetWinner();
        if (winner == null)
            WriteLineColored("Draw.", ConsoleColor.Yellow);
        else
            WriteLineColored($"Winner: {winner.Name}", ConsoleColor.Green);
    }

    private static bool PlaceStartingUnits(GameService gameService, Game game)
    {
        var placements = new[]
        {
            new PlaceUnitCommand
            {
                UnitName = "Warrior", PlayerId = game.Player1.Id, X = 0, Y = 0,
                MaxHealth = 55, AttackPower = 30, Defense = 2, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player1.Id, X = 1, Y = 0,
                MaxHealth = 40, AttackPower = 24, Defense = 1, MovementRange = 3
            },
            new PlaceUnitCommand
            {
                UnitName = "Warrior", PlayerId = game.Player2.Id, X = 4, Y = 4,
                MaxHealth = 55, AttackPower = 30, Defense = 2, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player2.Id, X = 3, Y = 4,
                MaxHealth = 40, AttackPower = 24, Defense = 1, MovementRange = 3
            }
        };

        foreach (var command in placements)
        {
            var result = gameService.PlaceUnit(command);
            if (result.IsFailure)
            {
                WriteLineColored($"Placement failed: {result.ErrorMessage}", ConsoleColor.Red);
                return false;
            }
        }

        return true;
    }

    private static void RenderBoardAndTurnInfo(
        ConsoleBoardRenderer renderer,
        GameService gameService,
        Game game,
        string player1Name,
        string player2Name,
        Position? highlightedPosition = null)
    {
        renderer.RenderBoard(game.Board, game.Player1.Id, player1Name, game.Player2.Id, player2Name, highlightedPosition);
        var currentPlayer = gameService.GetCurrentPlayer();
        WriteColored($"Turn {game.TurnNumber} - Current Player: ", ConsoleColor.DarkCyan);
        var currentPlayerColor = currentPlayer?.Id == game.Player1.Id ? ConsoleColor.Blue : ConsoleColor.Red;
        WriteLineColored(currentPlayer?.Name ?? "Unknown", currentPlayerColor);
        RenderRulesReference(player1Name, player2Name);
        System.Console.WriteLine();
    }

    private static Unit? SelectCurrentPlayerUnit(GameService gameService)
    {
        var myUnits = gameService.GetCurrentPlayerUnits().ToList();
        if (myUnits.Count == 0)
        {
            WriteLineColored("No units available.", ConsoleColor.Yellow);
            return null;
        }

        WriteLineColored("Select a unit:", ConsoleColor.Cyan);
        foreach (var unit in myUnits)
        {
            System.Console.WriteLine(FormatUnitLine(GetUnitAbbreviation(unit).ToString(), unit));
        }

        while (true)
        {
            var input = ReadInputWithHelp("Unit (W/S): ");
            var key = input.Trim().ToUpperInvariant();
            if (key.Length == 1)
            {
                var selected = myUnits.FirstOrDefault(unit => GetUnitAbbreviation(unit).ToString() == key);
                if (selected != null)
                    return selected;
            }

            WriteLineColored("Invalid selection.", ConsoleColor.Red);
        }
    }

    private static string ReadActionChoice()
    {
        WriteLineColored("Choose action:", ConsoleColor.Cyan);
        System.Console.WriteLine("[A] - Attack");
        System.Console.WriteLine("[M] - Move");

        while (true)
        {
            var input = ReadInputWithHelp("Action (A/M): ").Trim().ToUpperInvariant();
            if (input == "A")
                return "attack";

            if (input == "M")
                return "move";

            WriteLineColored("Please type A or M.", ConsoleColor.Red);
        }
    }

    private static bool ExecuteAction(
        ConsoleBoardRenderer renderer,
        GameService gameService,
        Game game,
        Unit selectedUnit,
        string action,
        string player1Name,
        string player2Name)
    {
        if (action == "move")
        {
            if (!TryReadMoveTargetWithArrowKeys(
                    renderer,
                    gameService,
                    game,
                    selectedUnit,
                    player1Name,
                    player2Name,
                    out var x,
                    out var y))
            {
                WriteLineColored("Move canceled.", ConsoleColor.Yellow);
                return false;
            }

            var moveResult = gameService.MoveUnit(new MoveUnitCommand(selectedUnit.Id, x, y));
            if (moveResult.IsFailure)
            {
                WriteLineColored($"Move failed: {moveResult.ErrorMessage}", ConsoleColor.Red);
                return false;
            }

            WriteLineColored($"Moved to ({x + 1}, {y + 1}).", ConsoleColor.Green);
            return true;
        }

        var enemyUnits = gameService.GetOpponentUnits().ToList();
        if (enemyUnits.Count == 0)
        {
            WriteLineColored("No enemy units available.", ConsoleColor.Yellow);
            return false;
        }

        WriteLineColored("Select attack target:", ConsoleColor.Cyan);
        foreach (var enemy in enemyUnits)
        {
            System.Console.WriteLine(FormatUnitLine(GetUnitAbbreviation(enemy).ToString(), enemy));
        }

        while (true)
        {
            var input = ReadInputWithHelp("Target (W/S): ");
            var key = input.Trim().ToUpperInvariant();
            if (key.Length != 1)
            {
                WriteLineColored("Invalid selection.", ConsoleColor.Red);
                continue;
            }

            var target = enemyUnits.FirstOrDefault(unit => GetUnitAbbreviation(unit).ToString() == key);
            if (target == null)
            {
                WriteLineColored("Invalid selection.", ConsoleColor.Red);
                continue;
            }

            var attackResult = gameService.AttackUnit(new AttackUnitCommand(selectedUnit.Id, target.Id));
            if (attackResult.IsFailure)
            {
                WriteLineColored($"Attack failed: {attackResult.ErrorMessage}", ConsoleColor.Red);
                return false;
            }

            WriteLineColored($"Attack dealt {attackResult.Value} damage.", ConsoleColor.Green);
            return true;
        }
    }

    private static bool TryReadMoveTargetWithArrowKeys(
        ConsoleBoardRenderer renderer,
        GameService gameService,
        Game game,
        Unit selectedUnit,
        string player1Name,
        string player2Name,
        out int targetX,
        out int targetY)
    {
        targetX = selectedUnit.Position.X;
        targetY = selectedUnit.Position.Y;

        while (true)
        {
            renderer.Clear();
            RenderBoardAndTurnInfo(renderer, gameService, game, player1Name, player2Name, new Position(targetX, targetY));
            WriteLineColored($"Selected unit: {selectedUnit.Name}", ConsoleColor.Cyan);
            WriteLineColored(
                "Use arrow keys to choose destination. Enter=confirm, Esc=cancel, H=help",
                ConsoleColor.DarkCyan);
            WriteLineColored($"Current target: ({targetX + 1}, {targetY + 1})", ConsoleColor.Gray);

            var key = System.Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    targetX = Math.Max(0, targetX - 1);
                    break;
                case ConsoleKey.RightArrow:
                    targetX = Math.Min(game.Board.Width - 1, targetX + 1);
                    break;
                case ConsoleKey.UpArrow:
                    targetY = Math.Max(0, targetY - 1);
                    break;
                case ConsoleKey.DownArrow:
                    targetY = Math.Min(game.Board.Height - 1, targetY + 1);
                    break;
                case ConsoleKey.Enter:
                    return true;
                case ConsoleKey.Escape:
                    return false;
                case ConsoleKey.H:
                    renderer.Clear();
                    ShowRulesSheet();
                    WriteLineColored("Press any key to return to move selection...", ConsoleColor.DarkCyan);
                    System.Console.ReadKey(intercept: true);
                    break;
            }
        }
    }

    private static string ReadRequiredString(string prompt)
    {
        while (true)
        {
            WriteColored(prompt, ConsoleColor.DarkCyan);
            var input = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
                return input;

            WriteLineColored("Value cannot be empty.", ConsoleColor.Red);
        }
    }

    private static void ShowUnitGuide(string player1Name, string player2Name)
    {
        System.Console.WriteLine();
        WriteLineColored("Unit Guide (Symmetric Roster)", ConsoleColor.Cyan);
        WriteLineColored("────────────────────────────────────────────────", ConsoleColor.DarkGray);
        WriteLineColored("Warrior  HP:55   ATK:30  DEF:2  MOVE:2", ConsoleColor.Gray);
        WriteLineColored("Scout    HP:40   ATK:24  DEF:1  MOVE:3", ConsoleColor.Gray);
        WriteLineColored("Combat range is melee (8-direction adjacent tiles).", ConsoleColor.DarkYellow);
        System.Console.Write("Board colors: ");
        WriteColored("Blue", ConsoleColor.Blue);
        System.Console.Write(" = ");
        WriteColored(player1Name, ConsoleColor.Blue);
        System.Console.Write(", ");
        WriteColored("Red", ConsoleColor.Red);
        System.Console.Write(" = ");
        WriteColored(player2Name, ConsoleColor.Red);
        System.Console.WriteLine(".");
        System.Console.WriteLine();
    }

    private static void ShowRulesSheet()
    {
        WriteLineColored("Game Rules", ConsoleColor.Cyan);
        WriteLineColored("────────────────────────────────────────────────", ConsoleColor.DarkGray);
        WriteLineColored("HP   = Health Points. Unit is defeated at 0.", ConsoleColor.Gray);
        WriteLineColored("ATK  = Attack. Base outgoing damage.", ConsoleColor.Gray);
        WriteLineColored("DEF  = Defense. Reduces incoming damage.", ConsoleColor.Gray);
        WriteLineColored("MOVE = Max tiles a unit can move per turn.", ConsoleColor.Gray);
        WriteLineColored("Damage formula: max(Attacker ATK - Defender DEF, 1).", ConsoleColor.DarkYellow);
        WriteLineColored("Combat range is melee (8-direction adjacent tiles only).", ConsoleColor.DarkYellow);
        WriteLineColored("Type HELP during the game to view this sheet again.", ConsoleColor.DarkCyan);
        System.Console.WriteLine();
    }

    private static void RenderRulesReference(string player1Name, string player2Name)
    {
        WriteLineColored("Rules: Melee range only (8-direction adjacent tiles).", ConsoleColor.DarkYellow);
        WriteLineColored("W - Warrior  HP:55   ATK:30  DEF:2  MOVE:2", ConsoleColor.Gray);
        WriteLineColored("S - Scout    HP:40   ATK:24  DEF:1  MOVE:3", ConsoleColor.Gray);
        System.Console.Write("Colors: ");
        WriteColored("Blue", ConsoleColor.Blue);
        System.Console.Write(" = ");
        WriteColored(player1Name, ConsoleColor.Blue);
        System.Console.Write(", ");
        WriteColored("Red", ConsoleColor.Red);
        System.Console.Write(" = ");
        WriteColored(player2Name, ConsoleColor.Red);
        System.Console.WriteLine();
        WriteLineColored("Type HELP to review full rules.", ConsoleColor.DarkCyan);
    }

    private static string FormatUnitLine(string displayLabel, Unit unit)
    {
        var x = unit.Position.X + 1;
        var y = unit.Position.Y + 1;
        var abbreviation = GetUnitAbbreviation(unit);
        if (string.Equals(displayLabel, abbreviation.ToString(), StringComparison.OrdinalIgnoreCase))
            return $"[{displayLabel}] - {unit.Name,-8} HP:{unit.Stats.CurrentHealth,-3} Pos:({x},{y})";

        return $"[{displayLabel}] {abbreviation} - {unit.Name,-8} HP:{unit.Stats.CurrentHealth,-3} Pos:({x},{y})";
    }

    private static char GetUnitAbbreviation(Unit unit)
    {
        var first = unit.Name.FirstOrDefault(char.IsLetterOrDigit);
        return first == default ? '?' : char.ToUpperInvariant(first);
    }

    private static string ReadInputWithHelp(string prompt)
    {
        while (true)
        {
            WriteColored(prompt, ConsoleColor.DarkCyan);
            var input = System.Console.ReadLine()?.Trim();
            if (string.Equals(input, "help", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine();
                ShowRulesSheet();
                continue;
            }

            return input ?? string.Empty;
        }
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
