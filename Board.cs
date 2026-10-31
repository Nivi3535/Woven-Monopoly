using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WovenMonopoly;

public sealed class Board
{
    private readonly List<Space> _spaces;

    public int GoIndex { get; }

    public int Size => _spaces.Count;

    //// Indexer: square brackets, name is always "this"
    public Space this[int index] => _spaces[index];

    private Board(List<Space> spaces, int goIndex)
    {
        _spaces = spaces;
        GoIndex = goIndex;
    }

    public static Board Load(string path)
    {
        string text;

        try
        {
            text = File.ReadAllText(path);
        }
        catch (IOException exception)
        {
            throw new GameInputException(
                $"could not read board file '{path}': {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new GameInputException(
                $"could not read board file '{path}': {exception.Message}");
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new GameInputException(
                    "board must be a JSON array of spaces");
            }

            var spaces = new List<Space>();

            //Catch 5: This check is important Iterating [] will proceed with no board square.  
            if (document.RootElement.GetArrayLength() == 0)
            {
                throw new GameInputException("board cannot be empty");
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new GameInputException(
                        $"each space must be an object, got {item}");
                }

                var type = RequiredString(item, "type");
                var name = RequiredString(item, "name");

                switch (type)
                {
                    case "go":
                        spaces.Add(new GoSpace(name));
                        break;

                    case "property":

                        if (!item.TryGetProperty("price", out var priceElement) ||
                            !priceElement.TryGetInt32(out var price) ||
                            price <= 0)
                        {
                            throw new GameInputException(
                                $"space '{name}': price must be a positive integer");
                        }

                        spaces.Add(new PropertySpace(
                            name,
                            price,
                            RequiredString(item, "colour")));

                        break;

                    default:
                        throw new GameInputException(
                            $"unknown space type '{type}'");
                }
            }

            // Collect the indexes of all GO spaces.
            var goIndexes = new List<int>();

            for (var index = 0; index < spaces.Count; index++)
            {
                if (spaces[index] is GoSpace)
                {
                    goIndexes.Add(index);
                    Console.WriteLine($"Found GO space at index {index}");
                }
            }

            if (goIndexes.Count != 1)
            {
                throw new GameInputException(
                    $"board must have exactly one GO space, found {goIndexes.Count}");
            }

            return new Board(spaces, goIndexes[0]);
        }
        catch (JsonException exception)
        {
            throw new GameInputException(
                $"board is not valid JSON: {exception.Message}");
        }
    }

    public (int Position, int GoPasses) Advance(int from, int steps)
    {

        if (from < 0 || from >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(from), $"Value must be between 0 and {Size - 1}.");
        }

        if (steps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), $"Steps should not in positive number");
        }

        var distanceToGo = (GoIndex - from + Size) % Size;
        //GoPasses is 0 for the Entry , Handled if player is in start
        if (distanceToGo == 0)
        {
            distanceToGo = Size;
        }

        //Checked steps 3 DistancetoGo is 2 from 8 size 9 = 1 + (3-2)/9 Passes = 1
        var passes = steps < distanceToGo
            ? 0
            : 1 + (steps - distanceToGo) / Size;

        return ((from + steps) % Size, passes);
    }

    public List<PropertySpace> ColourGroup(string colour)
    {
        //Catch 6: comparing string BROWN with brown or Brown - Casing is important 
        if (string.IsNullOrWhiteSpace(colour))
        {
            throw new ArgumentException(
                "Colour cannot be empty",
                nameof(colour));
        }

        return _spaces
            .OfType<PropertySpace>()
            .Where(space => string.Equals(
                space.Colour,
                colour.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string RequiredString(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new GameInputException(
                $"space is missing a valid '{key}' string");
        }

        return value.GetString()!;
    }
}

public abstract class Space
{
    public string Name { get; }
    //constructor is mainly used by the classes that inherit from it so making it Protected
    protected Space(string name)
    {
        Name = name;
    }
}

public sealed class GoSpace : Space
{
    public GoSpace(string name) : base(name)
    {
    }
}

public sealed class PropertySpace : Space
{
    public int Price { get; }
    public string Colour { get; }
    public PropertySpace(string name, int price, string colour)
        : base(name)
    {
        Price = price;
        Colour = colour;
    }
}

public sealed class GameInputException : Exception
{
    public GameInputException(string message) : base(message)
    {
    }
}