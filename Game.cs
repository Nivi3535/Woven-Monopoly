using System.Text.Json;

namespace WovenMonopoly;

public sealed class Game
{

    private static readonly string[] PlayerNames = ["Peter", "Billy", "Charlotte", "Sweedal"];
    private const int StartingMoney = 16;
    private const int PassGoBonus = 1;

    private readonly Board _board;
    private readonly int[] _rolls;
    private readonly List<Player> _players;
    //dictionary as a register of property ownership
    private readonly Dictionary<PropertySpace, Player> _owners = []; //_owner = jamica beach , peter
    private bool _hasPlayed;

    public Game(Board board, int[] rolls)
    {
        //Constructor arguments aren't checked for null
        if (rolls.Any(roll => roll <= 0))
            throw new GameInputException("rolls must be a JSON array of positive whole numbers");
        _board = board;
        _rolls = rolls;
        _players = PlayerNames.Select(name => new Player(name, StartingMoney, board.GoIndex)).ToList();
    }

    //Loading rolls.json into rolls
    public static int[] LoadRolls(string path)
    {
        string text;
        try { text = File.ReadAllText(path); }
        catch (IOException exception) { throw new GameInputException($"could not read rolls file '{path}': {exception.Message}"); }
        catch (UnauthorizedAccessException exception) { throw new GameInputException($"could not read rolls file '{path}': {exception.Message}"); }

        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new GameInputException("rolls must be a JSON array of positive whole numbers");
            //Catch 8 - I did a test with empty array No exception but the rolls are set ahead so that doesn't make sense
            if (document.RootElement.GetArrayLength() == 0)
            {
                throw new GameInputException("rolls cannot be empty in woven monopoly");
            }
            return document.RootElement.EnumerateArray().Select((element, index) =>
            {

                //Catch 7 - TryGetInt32 will work only if its integer number if in case json has string or float throw invaidOperation error 
                //do one more check element.valueKind == jsonValueKind.Nuber && then do trygetint32 so it will work as expected
                //test to be done - I wanna check how this will behave if i have 2.0 in the json 
                if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var roll) || roll <= 0)
                {
                    throw new GameInputException(
                        $"Invalid roll at position {index}: {element.GetRawText()} is not a positive whole number");
                }
                return roll;
            }).ToArray();
        }
        catch (JsonException exception)
        {
            throw new GameInputException($"rolls file is not valid JSON: {exception.Message}");
        }
    }

    //Referenced by Program.cs 
    public GameResult Play()
    {
        //Only one game instance 
        if (_hasPlayed) throw new InvalidOperationException("A game instance can only be played once.");
        _hasPlayed = true;

        var rollsPlayed = 0;
        var activePlayers = new List<Player>(_players); //new list gets its initial contents from _players 
        var currentIndex = 0;

        for (var turn = 0; turn < _rolls.Length; turn++)
        {
            // Select the player for this turn.
            var player = activePlayers[currentIndex];

            // AI Generated version - Players take turns in order, wrapping back to the first player
            //var player = _players[turn % _players.Count];       
            //Turn 5:  5 % 4 = 1  →  Billy plays and goes bankrupt
            //Remove Billy:          [Peter, Charlotte, Sweedal]
            //Turn 6:  6 % 3 = 0  →  Peter plays

            // Select the roll for this turn
            var roll = _rolls[turn];

            var (position, goPasses) = _board.Advance(player.Position, roll);
            player.Position = position;
            player.Receive(goPasses * PassGoBonus);
            ResolveLanding(player);
            rollsPlayed++;

            //No bank so assumpting if 1 player is bankrupt then game is finished  
            if (player.IsBankrupt)
            {
                break;
                //Review Check - remove the properties of bankrupted player, remove him from active player list
                //if (currentIndex >= activePlayers.Count)
                //{
                //  currentIndex = 0;
                //}
            }
            else
            {
                // Move to the next player.
                currentIndex = (currentIndex + 1) % activePlayers.Count;
            }
        }

        var eligible = activePlayers
       .Where(player => !player.IsBankrupt)
       .ToList();

        if (eligible.Count == 0)
        {
            eligible = _players;
        }

        var highestMoney = eligible.Max(player => player.Money);

        var winners = eligible
            .Where(player => player.Money == highestMoney)
            .Select(player => player.Name)
            .ToArray();

        var playerResults = _players.Select(player => new PlayerResult(
        player.Name,
        player.Money,
        _board[player.Position].Name,
        player.IsBankrupt)).ToArray();

        return new GameResult(
            winners,
            playerResults,
            _players.FirstOrDefault(player => player.IsBankrupt)?.Name,
            rollsPlayed,
            _rolls.Length);
    }


    //When a player lands on a space 
    private void ResolveLanding(Player player)
    {
        if (_board[player.Position] is not PropertySpace property) return;

        //If the property is already exisit in dict - TryGetVAlue gives true - !true = false
        if (!_owners.TryGetValue(property, out var owner))
        {
            // runs only when the property is NOT owned (not in dictionary) yet
            var paid = player.Pay(property.Price);
            if (paid == property.Price) _owners[property] = player;
            return;
        }
        if (owner == player) return;

        var ownsGroup = _board.ColourGroup(property.Colour).All(space =>
            _owners.TryGetValue(space, out var groupOwner) && groupOwner == owner);

        //I have got ouner value from above <player position - use that position and get property value to owner>
        Console.WriteLine($"ownsGroup = {ownsGroup}");
        // prints: ownsGroup = True  (or False)
        var rent = property.Price * (ownsGroup ? 2 : 1);
        owner.Receive(player.Pay(rent)); //adding up the rent to the owner
    }

    private void RemovePlayerProperties(Player player)
    {
        // Find all properties owned by this player.
        var propertiesToRemove = _owners
            .Where(entry => entry.Value == player)
            .Select(entry => entry.Key)
            .ToList();

        // Remove those properties from the ownership dictionary.
        foreach (var property in propertiesToRemove)
        {
            _owners.Remove(property);
        }
    }
}



public sealed class Player(string name, int money, int position)
{
    public string Name { get; } = name;
    public int Money { get; private set; } = money;
    public int Position { get; set; } = position;
    public bool IsBankrupt { get; private set; }

    public void Receive(int amount)
    {
        Money += amount;
    }

    public int Pay(int amount_to_be_paid)
    {
        //if (amount_to_be_paid < money)
        //{

        // paid = amount
        //} else {
        //paid = money set bankrupt to true also thera is a cndition check}  
        //
        var paid = Math.Min(amount_to_be_paid, Money);
        Money -= paid;
        if (paid < amount_to_be_paid) IsBankrupt = true;
        return paid;
    }
}

public sealed record PlayerResult(string Name, int Money, string Space, bool Bankrupt);
public sealed record GameResult(string[] Winners, PlayerResult[] Players, string? BankruptPlayer,
    int RollsPlayed, int RollsTotal)
{
    public string EndedBy => BankruptPlayer is null ? "rolls_exhausted" : "bankruptcy";

    public string Summary()
    {
        var verdict = Winners.Length == 1 ? $"Winner: {Winners[0]}" : $"Tie between: {string.Join(", ", Winners)}";
        var reason = BankruptPlayer is null
            ? $"all {RollsTotal} rolls were played and nobody went bankrupt"
            : $"{BankruptPlayer} went bankrupt on roll {RollsPlayed} of {RollsTotal}";
        var lines = new List<string> { $"{verdict} ({reason})", "" };
        lines.AddRange(Players.Select(player =>
            $"  {player.Name,-10} ${player.Money,-4} on {player.Space}{(player.Bankrupt ? "  (bankrupt)" : "")}"));
        return string.Join(Environment.NewLine, lines);
    }
}
