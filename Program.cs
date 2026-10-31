using System.Text.Json;
using WovenMonopoly;

var options = args.Where(arg => arg.StartsWith('-')).ToList();
var paths = args.Where(arg => !arg.StartsWith('-')).ToList();

//Catch 1: unknown options like --jsno --sjno handling
var unknown = options.Where(option => option != "--json").Distinct().ToList();
if (unknown.Count > 0)
{
    Console.Error.WriteLine($"Error: unknown option(s): {string.Join(", ", unknown)}");
    return 2;
}

//Catch 2: handling multiple --json
if (options.Count(option => option == "--json") > 1)
{
    Console.Error.WriteLine("Error: --json was given more than once");
    return 2;
}
var jsonOutput = options.Contains("--json");

//Catch 3: Exactly 2 path 
if (paths.Count != 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- BOARD_FILE ROLLS_FILE [--json]");
    return 2;
}
var boardName = Path.GetFileName(paths[0]);   // "data\board.json"   → "board.json"
var rollsName = Path.GetFileName(paths[1]);   // "data\rolls_1.json" → "rolls_1.json"

var boardOk = boardName.StartsWith("board", StringComparison.OrdinalIgnoreCase)
           && boardName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

var rollsOk = rollsName.StartsWith("rolls", StringComparison.OrdinalIgnoreCase)
           && rollsName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

if (!boardOk || !rollsOk)
{
    Console.Error.WriteLine("Error: expected a board file first (board*.json), then a rolls file (rolls*.json)");
    return 2;
}

//Catch 4: Enforcing both the files are different 
var boardPath = Path.GetFullPath(paths[0]);
var rollsPath = Path.GetFullPath(paths[1]);

if (string.Equals(boardPath, rollsPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Error: BOARD_FILE and ROLLS_FILE must be different files");
    return 2;
}

try
{
    var board = Board.Load(paths[0]);
    var rolls = Game.LoadRolls(paths[1]);
    var result = new Game(board, rolls).Play();
    Console.WriteLine(jsonOutput
        ? JsonSerializer.Serialize(result, new JsonSerializerOptions
        { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        : result.Summary());
    return 0;
}
catch (GameInputException exception)
{
    Console.Error.WriteLine($"Error: {exception.Message}");
    return 1;
}

