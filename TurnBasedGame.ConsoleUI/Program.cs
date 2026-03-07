using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Services;
using TurnBasedGame.ConsoleUI.Renderers;
using TurnBasedGame.Domain.Entities;

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
        System.Console.WriteLine("=== Tactical Grid Game ===");
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
            System.Console.WriteLine($"Failed to create game: {createResult.ErrorMessage}");
            return;
        }

        var game = gameService.CurrentGame;
        if (!PlaceStartingUnits(gameService, game))
        {
            System.Console.WriteLine("Failed to place starting units.");
            return;
        }

        ShowUnitGuide();
        System.Console.WriteLine("Press Enter to start the match...");
        System.Console.ReadLine();

        while (!gameService.IsGameOver())
        {
            var actionCompleted = false;
            while (!actionCompleted && !gameService.IsGameOver())
            {
                renderer.Clear();
                RenderBoardAndTurnInfo(renderer, gameService, game);

                var selectedUnit = SelectCurrentPlayerUnit(gameService);
                if (selectedUnit == null)
                    break;

                var action = ReadActionChoice();
                actionCompleted = ExecuteAction(gameService, selectedUnit, action);

                if (!actionCompleted)
                {
                    System.Console.WriteLine("Try again. Press Enter to continue.");
                    System.Console.ReadLine();
                }
            }

            if (gameService.IsGameOver())
                break;

            var endResult = gameService.EndTurn(new EndTurnCommand());
            if (endResult.IsFailure)
            {
                System.Console.WriteLine($"Could not end turn: {endResult.ErrorMessage}");
                break;
            }

            System.Console.WriteLine("Turn ended. Press Enter to continue.");
            System.Console.ReadLine();
        }

        renderer.Clear();
        System.Console.WriteLine("=== GAME OVER ===");
        renderer.RenderBoard(game.Board, game.Player1.Id, game.Player2.Id);

        var winner = gameService.GetWinner();
        System.Console.WriteLine(winner == null ? "Draw." : $"Winner: {winner.Name}");
    }

    private static bool PlaceStartingUnits(GameService gameService, Game game)
    {
        var placements = new[]
        {
            new PlaceUnitCommand
            {
                UnitName = "Warrior", PlayerId = game.Player1.Id, X = 0, Y = 0,
                MaxHealth = 100, AttackPower = 18, Defense = 5, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player1.Id, X = 1, Y = 0,
                MaxHealth = 75, AttackPower = 12, Defense = 3, MovementRange = 3
            },
            new PlaceUnitCommand
            {
                UnitName = "Warrior", PlayerId = game.Player2.Id, X = 4, Y = 4,
                MaxHealth = 100, AttackPower = 18, Defense = 5, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player2.Id, X = 3, Y = 4,
                MaxHealth = 75, AttackPower = 12, Defense = 3, MovementRange = 3
            }
        };

        foreach (var command in placements)
        {
            var result = gameService.PlaceUnit(command);
            if (result.IsFailure)
            {
                System.Console.WriteLine($"Placement failed: {result.ErrorMessage}");
                return false;
            }
        }

        return true;
    }

    private static void RenderBoardAndTurnInfo(ConsoleBoardRenderer renderer, GameService gameService, Game game)
    {
        renderer.RenderBoard(game.Board, game.Player1.Id, game.Player2.Id);
        var currentPlayer = gameService.GetCurrentPlayer();
        System.Console.WriteLine($"Turn {game.TurnNumber} - Current Player: {currentPlayer?.Name}");
        RenderRulesReference();
        System.Console.WriteLine();
    }

    private static Unit? SelectCurrentPlayerUnit(GameService gameService)
    {
        var myUnits = gameService.GetCurrentPlayerUnits().ToList();
        if (myUnits.Count == 0)
        {
            System.Console.WriteLine("No units available.");
            return null;
        }

        System.Console.WriteLine("Select a unit:");
        for (int i = 0; i < myUnits.Count; i++)
        {
            var unit = myUnits[i];
            System.Console.WriteLine(FormatUnitLine(i + 1, unit));
        }

        while (true)
        {
            System.Console.Write("Unit #: ");
            var input = System.Console.ReadLine();
            if (int.TryParse(input, out var selectedIndex) &&
                selectedIndex >= 1 &&
                selectedIndex <= myUnits.Count)
            {
                return myUnits[selectedIndex - 1];
            }

            System.Console.WriteLine("Invalid selection.");
        }
    }

    private static string ReadActionChoice()
    {
        while (true)
        {
            System.Console.Write("Action (move/attack): ");
            var input = System.Console.ReadLine()?.Trim().ToLowerInvariant();
            if (input is "move" or "attack")
                return input;

            System.Console.WriteLine("Please type 'move' or 'attack'.");
        }
    }

    private static bool ExecuteAction(GameService gameService, Unit selectedUnit, string action)
    {
        if (action == "move")
        {
            System.Console.Write("Target X (1-based): ");
            if (!int.TryParse(System.Console.ReadLine(), out var displayX) || displayX < 1)
            {
                System.Console.WriteLine("Invalid X coordinate.");
                return false;
            }

            System.Console.Write("Target Y (1-based): ");
            if (!int.TryParse(System.Console.ReadLine(), out var displayY) || displayY < 1)
            {
                System.Console.WriteLine("Invalid Y coordinate.");
                return false;
            }

            var x = displayX - 1;
            var y = displayY - 1;
            var moveResult = gameService.MoveUnit(new MoveUnitCommand(selectedUnit.Id, x, y));
            if (moveResult.IsFailure)
            {
                System.Console.WriteLine($"Move failed: {moveResult.ErrorMessage}");
                return false;
            }

            System.Console.WriteLine($"Moved to ({displayX}, {displayY}).");
            return true;
        }

        var enemyUnits = gameService.GetOpponentUnits().ToList();
        if (enemyUnits.Count == 0)
        {
            System.Console.WriteLine("No enemy units available.");
            return false;
        }

        System.Console.WriteLine("Select attack target:");
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            var enemy = enemyUnits[i];
            System.Console.WriteLine(FormatUnitLine(i + 1, enemy));
        }

        while (true)
        {
            System.Console.Write("Target #: ");
            var input = System.Console.ReadLine();
            if (!int.TryParse(input, out var targetIndex) ||
                targetIndex < 1 ||
                targetIndex > enemyUnits.Count)
            {
                System.Console.WriteLine("Invalid selection.");
                continue;
            }

            var target = enemyUnits[targetIndex - 1];
            var attackResult = gameService.AttackUnit(new AttackUnitCommand(selectedUnit.Id, target.Id));
            if (attackResult.IsFailure)
            {
                System.Console.WriteLine($"Attack failed: {attackResult.ErrorMessage}");
                return false;
            }

            System.Console.WriteLine($"Attack dealt {attackResult.Value} damage.");
            return true;
        }
    }

    private static string ReadRequiredString(string prompt)
    {
        while (true)
        {
            System.Console.Write(prompt);
            var input = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
                return input;

            System.Console.WriteLine("Value cannot be empty.");
        }
    }

    private static void ShowUnitGuide()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Unit Guide (Symmetric Roster)");
        System.Console.WriteLine("────────────────────────────────────────────────");
        System.Console.WriteLine("Warrior  HP:100  ATK:18  DEF:5  MOVE:2  Role: balanced frontline fighter");
        System.Console.WriteLine("Scout    HP:75   ATK:12  DEF:3  MOVE:3  Role: fast flanker");
        System.Console.WriteLine("Combat range is melee (adjacent tiles).");
        System.Console.WriteLine("Board colors: Blue = Player 1, Red = Player 2.");
        System.Console.WriteLine("Coordinates shown in UI are 1-indexed (top-left is 1,1).");
        System.Console.WriteLine();
    }

    private static void RenderRulesReference()
    {
        System.Console.WriteLine("Rules: Melee range only (adjacent tiles).");
        System.Console.WriteLine("W - Warrior  HP:100  ATK:18  DEF:5  MOVE:2  Role: balanced frontline");
        System.Console.WriteLine("S - Scout    HP:75   ATK:12  DEF:3  MOVE:3  Role: fast flanker");
        System.Console.WriteLine("Colors: Blue = Player 1, Red = Player 2");
    }

    private static string FormatUnitLine(int displayIndex, Unit unit)
    {
        var x = unit.Position.X + 1;
        var y = unit.Position.Y + 1;
        var abbreviation = GetUnitAbbreviation(unit);
        return $"[{displayIndex}] {abbreviation} - {unit.Name,-8} HP:{unit.Stats.CurrentHealth,-3} Pos:({x},{y})";
    }

    private static char GetUnitAbbreviation(Unit unit)
    {
        var first = unit.Name.FirstOrDefault(char.IsLetterOrDigit);
        return first == default ? '?' : char.ToUpperInvariant(first);
    }
}
