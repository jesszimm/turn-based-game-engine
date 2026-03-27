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
    private static bool _controlTileEnabled;
    private static string? _statusMessage;
    private static ConsoleColor _statusColor = ConsoleColor.Gray;

    public static void Main(string[] args)
    {
        var renderer = new ConsoleBoardRenderer();
        var gameService = new GameService();
        var nextAiMoveUnitAbbreviation = 'W';
        var aiFocusTargetId = (Guid?)null;

        renderer.Clear();
        WriteLineColored("=== Tactical Grid Game ===", ConsoleColor.Cyan);
        System.Console.WriteLine();

        var isPlayer2Ai = ReadYesNo("Play against AI opponent? (Y/N): ");
        var aiDifficulty = isPlayer2Ai ? ReadAiDifficulty() : AiDifficulty.Easy;
        var controlTileEnabled = isPlayer2Ai && (aiDifficulty == AiDifficulty.Medium || aiDifficulty == AiDifficulty.Hard);
        _controlTileEnabled = controlTileEnabled;
        var player1Name = ReadRequiredString("Enter Player 1 name: ");
        var player2Name = isPlayer2Ai ? "CPU" : ReadRequiredString("Enter Player 2 name: ");

        var createResult = gameService.CreateGame(new CreateGameCommand
        {
            Player1Name = player1Name,
            Player2Name = player2Name,
            BoardWidth = 5,
            BoardHeight = 5,
            ControlTileEnabled = controlTileEnabled
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

        if (isPlayer2Ai && aiDifficulty == AiDifficulty.Hard)
        {
            var endTurn = gameService.EndTurn(new EndTurnCommand());
            if (endTurn.IsFailure)
            {
                WriteLineColored($"Could not start with AI turn: {endTurn.ErrorMessage}", ConsoleColor.Red);
                return;
            }
        }

        while (!gameService.IsGameOver())
        {
            var actionCompleted = false;
            while (!actionCompleted && !gameService.IsGameOver())
            {
                renderer.Clear();
                RenderBoardAndTurnInfo(renderer, gameService, game, player1Name, player2Name);

                var currentPlayer = gameService.GetCurrentPlayer();
                if (isPlayer2Ai && currentPlayer?.Id == game.Player2.Id)
                {
                    System.Threading.Thread.Sleep(2000);
                    ExecuteAiTurn(gameService, game, aiDifficulty, ref nextAiMoveUnitAbbreviation, ref aiFocusTargetId);
                    actionCompleted = true;
                    continue;
                }

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
            }

            if (gameService.IsGameOver())
                break;

            var endResult = gameService.EndTurn(new EndTurnCommand());
            if (endResult.IsFailure)
            {
                WriteLineColored($"Could not end turn: {endResult.ErrorMessage}", ConsoleColor.Red);
                break;
            }

        }

        renderer.Clear();
        WriteLineColored("=== GAME OVER ===", ConsoleColor.Magenta);
        renderer.RenderBoard(game.Board, game.Player1.Id, player1Name, game.Player2.Id, player2Name, null, _controlTileEnabled);

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
                MaxHealth = 55, AttackPower = 24, Defense = 0, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player1.Id, X = 1, Y = 0,
                MaxHealth = 40, AttackPower = 18, Defense = 0, MovementRange = 3
            },
            new PlaceUnitCommand
            {
                UnitName = "Warrior", PlayerId = game.Player2.Id, X = 4, Y = 4,
                MaxHealth = 55, AttackPower = 24, Defense = 0, MovementRange = 2
            },
            new PlaceUnitCommand
            {
                UnitName = "Scout", PlayerId = game.Player2.Id, X = 3, Y = 4,
                MaxHealth = 40, AttackPower = 18, Defense = 0, MovementRange = 3
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
        renderer.RenderBoard(
            game.Board,
            game.Player1.Id,
            player1Name,
            game.Player2.Id,
            player2Name,
            highlightedPosition,
            _controlTileEnabled);
        var currentPlayer = gameService.GetCurrentPlayer();
        WriteColored($"Turn {game.TurnNumber} - Current Player: ", ConsoleColor.DarkCyan);
        var currentPlayerColor = currentPlayer?.Id == game.Player1.Id ? ConsoleColor.Blue : ConsoleColor.Red;
        WriteLineColored(currentPlayer?.Name ?? "Unknown", currentPlayerColor);
        RenderRulesReference(player1Name, player2Name);
        RenderUnitStatus(game, player1Name, player2Name);
        RenderStatusMessage();
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
                SetStatusMessage("Move canceled.", ConsoleColor.Yellow);
                return false;
            }

            var moveResult = gameService.MoveUnit(new MoveUnitCommand(selectedUnit.Id, x, y));
            if (moveResult.IsFailure)
            {
                if (moveResult.ErrorMessage?.Contains("occupied", StringComparison.OrdinalIgnoreCase) == true)
                    SetStatusMessage("Invalid move: target tile is occupied by another unit.", ConsoleColor.Red);
                else
                    SetStatusMessage($"Move failed: {moveResult.ErrorMessage}", ConsoleColor.Red);
                return false;
            }

            SetStatusMessage($"Moved to ({x + 1}, {y + 1}).", ConsoleColor.Green);
            return true;
        }

        var enemyUnits = gameService.GetOpponentUnits().ToList();
        if (enemyUnits.Count == 0)
        {
            SetStatusMessage("No enemy units available.", ConsoleColor.Yellow);
            return false;
        }

        var attackableTargets = enemyUnits
            .Where(enemy => selectedUnit.Position.IsAdjacentTo(enemy.Position, includeDiagonals: true))
            .ToList();

        if (attackableTargets.Count == 0)
        {
            SetStatusMessage(
                "Invalid attack: no enemy units are adjacent (including diagonals).",
                ConsoleColor.Yellow);
            return false;
        }

        if (attackableTargets.Count == 1)
        {
            var target = attackableTargets[0];
            var attackResult = gameService.AttackUnit(new AttackUnitCommand(selectedUnit.Id, target.Id));
            if (attackResult.IsFailure)
            {
                SetStatusMessage($"Attack failed: {attackResult.ErrorMessage}", ConsoleColor.Red);
                return false;
            }

            SetStatusMessage($"Attack dealt {attackResult.Value} damage.", ConsoleColor.Green);
            return true;
        }

        WriteLineColored("Select attack target:", ConsoleColor.Cyan);
        foreach (var enemy in attackableTargets)
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

            var target = attackableTargets.FirstOrDefault(unit => GetUnitAbbreviation(unit).ToString() == key);
            if (target == null)
            {
                SetStatusMessage("Invalid selection.", ConsoleColor.Red);
                continue;
            }

            var attackResult = gameService.AttackUnit(new AttackUnitCommand(selectedUnit.Id, target.Id));
            if (attackResult.IsFailure)
            {
                SetStatusMessage($"Attack failed: {attackResult.ErrorMessage}", ConsoleColor.Red);
                return false;
            }

            SetStatusMessage($"Attack dealt {attackResult.Value} damage.", ConsoleColor.Green);
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
        var originX = targetX;
        var originY = targetY;
        var maxRange = selectedUnit.Stats.MovementRange;

        while (true)
        {
            renderer.Clear();
            RenderBoardAndTurnInfo(
                renderer,
                gameService,
                game,
                player1Name,
                player2Name,
                new Position(targetX, targetY));
            WriteLineColored($"Selected unit: {selectedUnit.Name}", ConsoleColor.Cyan);
            WriteLineColored(
                "Use arrow keys to choose destination. Enter=confirm, Esc=cancel, H=help",
                ConsoleColor.DarkCyan);
            WriteLineColored($"Current target: ({targetX + 1}, {targetY + 1})", ConsoleColor.Gray);

            var key = System.Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (TryGetCursorUpdate(targetX - 1, targetY, originX, originY, maxRange, game, out var nextLeftX, out var nextLeftY))
                    {
                        targetX = nextLeftX;
                        targetY = nextLeftY;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (TryGetCursorUpdate(targetX + 1, targetY, originX, originY, maxRange, game, out var nextRightX, out var nextRightY))
                    {
                        targetX = nextRightX;
                        targetY = nextRightY;
                    }
                    break;
                case ConsoleKey.UpArrow:
                    if (TryGetCursorUpdate(targetX, targetY - 1, originX, originY, maxRange, game, out var nextUpX, out var nextUpY))
                    {
                        targetX = nextUpX;
                        targetY = nextUpY;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (TryGetCursorUpdate(targetX, targetY + 1, originX, originY, maxRange, game, out var nextDownX, out var nextDownY))
                    {
                        targetX = nextDownX;
                        targetY = nextDownY;
                    }
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

    private static bool TryGetCursorUpdate(
        int candidateX,
        int candidateY,
        int originX,
        int originY,
        int maxRange,
        Game game,
        out int nextX,
        out int nextY)
    {
        nextX = candidateX;
        nextY = candidateY;

        if (candidateX < 0 || candidateX >= game.Board.Width)
            return false;
        if (candidateY < 0 || candidateY >= game.Board.Height)
            return false;

        var distance = Math.Abs(candidateX - originX) + Math.Abs(candidateY - originY);
        if (distance > maxRange)
            return false;

        return true;
    }

    private static void ExecuteAiTurn(
        GameService gameService,
        Game game,
        AiDifficulty difficulty,
        ref char nextAiMoveUnitAbbreviation,
        ref Guid? focusTargetId)
    {
        var aiDecisionService = new AiDecisionService();
        var decisionState = new AiDecisionState(
            game,
            difficulty,
            nextAiMoveUnitAbbreviation,
            focusTargetId);

        var decision = aiDecisionService.Decide(decisionState);
        focusTargetId = decisionState.FocusTargetId;

        if (decision.ActionType == AiDecisionAction.Skip)
        {
            WriteLineColored("CPU had no valid attack or move and skipped action.", ConsoleColor.Yellow);
            return;
        }

        if (decision.ActionType == AiDecisionAction.Attack)
        {
            var attacker = game.Board.FindUnit(decision.UnitId);
            var defender = decision.TargetId == null ? null : game.Board.FindUnit(decision.TargetId.Value);
            if (attacker != null && defender != null)
            {
                var attack = gameService.AttackUnit(new AttackUnitCommand(attacker.Id, defender.Id));
                if (attack.IsSuccess)
                {
                    WriteLineColored(
                        $"CPU used {attacker.Name} and attacked {defender.Name} for {attack.Value} damage.",
                        ConsoleColor.Green);
                    return;
                }
            }
        }

        if (decision.ActionType == AiDecisionAction.Move)
        {
            var unit = game.Board.FindUnit(decision.UnitId);
            var position = decision.TargetPosition;
            if (unit != null && position != null)
            {
                var move = gameService.MoveUnit(new MoveUnitCommand(unit.Id, position.X, position.Y));
                if (move.IsSuccess)
                {
                    WriteLineColored(
                        $"CPU moved {unit.Name} to ({position.X + 1}, {position.Y + 1}).",
                        ConsoleColor.Green);
                    if (difficulty == AiDifficulty.Easy)
                        nextAiMoveUnitAbbreviation = nextAiMoveUnitAbbreviation == 'W' ? 'S' : 'W';
                    return;
                }
            }
        }

        WriteLineColored("CPU had no valid attack or move and skipped action.", ConsoleColor.Yellow);
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

    private static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            WriteColored(prompt, ConsoleColor.DarkCyan);
            var input = System.Console.ReadLine()?.Trim().ToUpperInvariant();
            if (input is "Y" or "YES")
                return true;

            if (input is "N" or "NO")
                return false;

            WriteLineColored("Please enter Y or N.", ConsoleColor.Red);
        }
    }

    private static AiDifficulty ReadAiDifficulty()
    {
        WriteLineColored("Select AI difficulty:", ConsoleColor.Cyan);
        System.Console.WriteLine("[E] - Easy");
        System.Console.WriteLine("[M] - Medium");
        System.Console.WriteLine("[H] - Hard");
        while (true)
        {
            WriteColored("Difficulty (E/M/H): ", ConsoleColor.DarkCyan);
            var input = System.Console.ReadLine()?.Trim().ToUpperInvariant();
            if (input == "E")
                return AiDifficulty.Easy;
            if (input == "M")
                return AiDifficulty.Medium;
            if (input == "H")
                return AiDifficulty.Hard;

            WriteLineColored("Please enter E, M, or H.", ConsoleColor.Red);
        }
    }

    private static void ShowUnitGuide(string player1Name, string player2Name)
    {
        System.Console.WriteLine();
        WriteLineColored("Unit Guide (Symmetric Roster)", ConsoleColor.Cyan);
        WriteLineColored("────────────────────────────────────────────────", ConsoleColor.DarkGray);
        WriteLineColored("Warrior  HP:55   ATK:24  MOVE:2", ConsoleColor.Gray);
        WriteLineColored("Scout    HP:40   ATK:18  MOVE:3", ConsoleColor.Gray);
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
        WriteLineColored("MOVE = Max tiles a unit can move per turn.", ConsoleColor.Gray);
        WriteLineColored("Damage formula: damage = Attacker ATK.", ConsoleColor.DarkYellow);
        WriteLineColored("Combat range is melee (8-direction adjacent tiles only).", ConsoleColor.DarkYellow);
        WriteLineColored("Each turn: a player may move OR attack (not both).", ConsoleColor.DarkYellow);
        if (_controlTileEnabled)
            WriteLineColored("Control tile: hold the green center tile for 5 turns to win.", ConsoleColor.DarkYellow);
        WriteLineColored("Type HELP during the game to view this sheet again.", ConsoleColor.DarkCyan);
        System.Console.WriteLine();
    }

    private static void SetStatusMessage(string message, ConsoleColor color)
    {
        _statusMessage = message;
        _statusColor = color;
    }

    private static void RenderStatusMessage()
    {
        if (string.IsNullOrWhiteSpace(_statusMessage))
            return;

        WriteLineColored(_statusMessage, _statusColor);
    }

    private static void RenderRulesReference(string player1Name, string player2Name)
    {
        WriteLineColored(
            "Rules: Can only attack if opponent's piece is adjacent to attacker (including diagonals)",
            ConsoleColor.DarkYellow);
        WriteLineColored("Rule: On a turn, a player may move OR attack (not both).", ConsoleColor.DarkYellow);
        if (_controlTileEnabled)
            WriteLineColored("Rule: Hold the green center tile for 5 turns to win.", ConsoleColor.DarkYellow);
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

    private static void RenderUnitStatus(Game game, string player1Name, string player2Name)
    {
        WriteColored("Blue - ", ConsoleColor.Blue);
        WriteColored(player1Name, ConsoleColor.Blue);
        System.Console.WriteLine();
        foreach (var unit in game.Board.GetPlayerUnits(game.Player1.Id).OrderBy(u => u.Name))
        {
            System.Console.WriteLine(
                $"  {unit.Name,-8} HP:{unit.Stats.CurrentHealth} ATK:{unit.Stats.AttackPower} MOVE:{unit.Stats.MovementRange}");
        }

        WriteColored("Red - ", ConsoleColor.Red);
        WriteColored(player2Name, ConsoleColor.Red);
        System.Console.WriteLine();
        foreach (var unit in game.Board.GetPlayerUnits(game.Player2.Id).OrderBy(u => u.Name))
        {
            System.Console.WriteLine(
                $"  {unit.Name,-8} HP:{unit.Stats.CurrentHealth} ATK:{unit.Stats.AttackPower} MOVE:{unit.Stats.MovementRange}");
        }
    }

    private static string FormatUnitLine(string displayLabel, Unit unit)
    {
        var x = unit.Position.X + 1;
        var y = unit.Position.Y + 1;
        var abbreviation = GetUnitAbbreviation(unit);
        if (string.Equals(displayLabel, abbreviation.ToString(), StringComparison.OrdinalIgnoreCase))
            return $"[{displayLabel}] - {unit.Name,-8}";

        return $"[{displayLabel}] {abbreviation} - {unit.Name,-8}";
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
