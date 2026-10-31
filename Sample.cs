public GameResult Play()
{
    // Allow each Game instance to be played only once.
    if (_hasPlayed)
    {
        throw new InvalidOperationException(
            "A game instance can only be played once.");
    }

    _hasPlayed = true;

    var rollsPlayed = 0;

    // Keep a separate list of players who are still in the game.
    var activePlayers = new List<Player>(_players);

    // The index of the player whose turn it is.
    var currentIndex = 0;

    for (var turn = 0; turn < _rolls.Length; turn++)
    {
        // Stop if nobody is left.
        if (activePlayers.Count == 0)
        {
            break;
        }

        // Get the current active player.
        var player = activePlayers[currentIndex];

        Console.WriteLine(
            $"Turn: {turn}, Player: {player.Name}, " +
            $"Money: {player.Money}, Position: {player.Position}");

        // Move the player.
        var (position, goPasses) =
            _board.Advance(player.Position, _rolls[turn]);

        player.Position = position;

        // Give the player money for passing GO.
        player.Receive(goPasses * PassGoBonus);

        // Handle property purchase and rent.
        ResolveLanding(player);

        rollsPlayed++;

        // Check whether the player went bankrupt.
        if (player.IsBankrupt)
        {
            Console.WriteLine($"{player.Name} is bankrupt!");

            // Release all properties owned by this player.
            RemovePlayerProperties(player);

            // Remove the player from the active game.
            activePlayers.Remove(player);

            // End the game if only one player remains.
            if (activePlayers.Count <= 1)
            {
                break;
            }

            // If the removed player was the last player in the list,
            // move the index back to the last valid position.
           

            // Do not increment currentIndex here.
            // The next player has shifted into this index.
        }
        else
        {
            // Move to the next player.
            currentIndex = (currentIndex + 1) % activePlayers.Count;
        }
    }

    // Determine the winner from the players still in the game.
    var eligible = activePlayers;

    // If everybody went bankrupt, fall back to all players.
    if (eligible.Count == 0)
    {
        eligible = _players;
    }

    var highestMoney = eligible.Max(player => player.Money);

    var winners = eligible
        .Where(player => player.Money == highestMoney)
        .Select(player => player.Name)
        .ToArray();

    // Include every player in the results, including bankrupt players.
    var playerResults = _players
        .Select(player => new PlayerResult(
            player.Name,
            player.Money,
            _board[player.Position].Name,
            player.IsBankrupt))
        .ToArray();

    return new GameResult(
        winners,
        playerResults,
        _players.FirstOrDefault(player => player.IsBankrupt)?.Name,
        rollsPlayed,
        _rolls.Length);
}