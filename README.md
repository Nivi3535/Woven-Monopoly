# Woven Monopoly (C#)

Run from this directory:

```powershell
dotnet run -- board.json rolls_1.json --json
dotnet run -- board.json rolls_2.json --json
```

To print a readable summary, omit `--json`. The app requires the .NET 10 SDK and uses only the .NET standard library; there are no third-party dependencies.

## Language choice

I understand team uses Ruby, but I chose to implement the solution in C#. I’m still learning Ruby, and building the initial version in a language I’m fluent in helps me:
-understand the problem domain clearly
-validate the behaviour against the JSON input format
-ensure the board, spaces, and movement logic are correct
-produce an implementation I can confidently explain
This approach lets me focus on correctness and clarity before handling with Ruby syntax and idiom

## Design

- `Board` loads and validates the board, stores its spaces in order, and handles circular movement and GO passes.
- `Game` manages turn order, money, ownership, rent, payment and the stopping condition.
- `Player` owns the player's balance and payment behavior.
- `GameResult` contains the final outcome and formats the readable summary. The CLI is in `Program.cs`.

The board can have any number of spaces and colour groups. Rent doubles when an owner has every property in that colour group.

-The initial solution was AI-assisted. I reviewed the generated implementation, validated it against the requirements, identified incorrect assumptions and missing edge-case handling, and implemented the fixes documented above. The review process included turn-order analysis, input validation, error handling, and additional test coverage

## Assumptions

- A player who cannot pay a purchase price or rent pays their remaining cash, becomes bankrupt, and the game stops immediately. The property is not transferred when the player cannot afford to buy it.
- If the rolls run out before bankruptcy, the player or players with the most money win. Equal balances are reported as a tie.
- Passing GO multiple times in a single move pays the bonus once per pass. Landing on GO exactly counts as passing it; the initial position does not.
- Rent equals the property’s price and double it when one owner holds every property of that colour, but it doesn’t give a base rent in the board.json. I used the property price to calculate the rent because it’s the only amount the board provides. For example, a $3 property costs $3 in rent, or $6 if the owner holds the full colour set.
- Partial payment causes bankruptcy.
- Rolls: The requirement did not mention anything about no. of dice. The provided roll files only contain 1 to 6, which fits a single six-sided die, but I don’t enforce a maximum. This avoids hard-coding a limit taken from the sample files. Any whole number of 1 or greater is accepted.
- roll must be written as a whole number of 1 or greater. Values like 1.5, 1.0, 0, -1 and "abc" are rejected with an error, because the dice rolls in the sample files are all written as plain whole numbers and strict validation catches bad input early. An empty rolls file is invalid, because the business requirement says roll are set ahead of time.

For the code

- Gospace assumption must contain only one go space

## Critical Analysis, Implementation during Review, Handled Exception and validation on AI Code

- I identified several edge cases that were not handled in the generated implementation

- The AI-generated code had few limitation, unhandled exception and unchecked validation also code was a bit difficult to follow, so I refactored it using more conventional coding patterns and simpler logic. My goal was to make the code easier to understand, maintain, and navigate, especially extend for someone new to the codebase.

1.  Turn order: In Game.cs - Play method - I track the current player with currentIndex instead of AI - Suggested using formula \_players[turn % _players.Count]. My implementation separates the two jobs: turn selects the roll, and currentIndex selects the player.
    Turn Order Handling - Bug found with AI suggested version - is a limitation if players are removed later.

                 //the order is Peter, Billy, Charlotte and Sweedal:
                  Turn 5: 5 % 4 = 1 → Billy (bankrupt)
                      Remove Billy
                  Turn 6: 6 % 3 = 0 → Peter <<ideally it should be Charlotte not Peter>>
                  Turn 7: 7 % 3 = 1 → Charlotte
                  Turn 8: 8 % 3 = 2 → Sweedal
                  Turn 9: 9 % 3 = 0 → Peter

The game currently ends at the first bankruptcy, so nobody is removed from activePlayers, and the simpler formula would work today. I introduced extra variables because this approach is easier to extend. If bankrupt players later stay out while the game continues, only the bankruptcy handling needs to change: remove the player and wrap currentIndex if needed. The roll selection stays the same. Separation of concerns.

2.  I designed each Game instance to represent one complete game. This prevents accidentally replaying the same instance with previously accumulated money, player positions, and property ownership. If we need to play another game, we can create a new Game instance.

3.  In program.cs the file related check condition were not identified and implemented by AI.
    When I reviewed I could find possible ways the application could crash or the exception go unhandled. The followeing checks were implemented by me:

         3.1
         What if the user swap board.json and roll.json, eg. dotnet run rolls.json board.json is handled because the Board.cs expects and can only handle data from board.json. If unhandled this will crash the application at the RUN time.
         3.2
         Multiple --json option are allowed becuase its normally supressed by dotnet and unknown --json options are handled in the code. Also, --json is OPTIONAL

4.  No 2 path of the argument are same, this ensure both the files are different. eg. dotnet run board.json board.json --json will throw Error: BOARD_FILE and ROLLS_FILE must be different files

5.  rolls.cs I reviewed and validated the following, checks for string, empty array, float ideally rolls should accept only whole number

6.  In Board.cs .json could be Empty Array [], all the properties like name, type, price and color are mandatory and validation is implmemented. AI did not check the above mentioned business requirement.

7.  A colour is treated as a group label, so any non-empty text is valid, including values like “Banana”. Colours are compared ignoring case and surrounding spaces. No Valid color are checked to avoid hard-code details from the sample files, so a misspelt colour creates a separate group. Eg. "Brown" != "brown" fixed it by ignoreCase, " Brown " fixed by Trimmin

## Hand-designed game

`rolls_3.json` is `[1,3,4,5,1,1,6,1,1,1]`. Before running it, I expected Peter to buy both Brown properties on turns 1 and 5. Charlotte starts turn 7 on space 4; a roll of 6 wraps her to The Burvale (space 1), where Peter owns the complete Brown group. She should pay Peter doubled rent of $2. The remaining three rolls continue the game without changing that scenario.

## Sample output

The following results were produced with the supplied board and roll files:
cleaned up the spacing for user friendly view:

`rolls_1.json`
Winner: Peter (Charlotte went bankrupt on roll 27 of 50)
Peter $39 on Massizim;
Billy $14 on GO;
Charlotte $0 on Gami Chicken (bankrupt); Bankruptcy on roll 27 of 50
Sweedal $1 on Gami Chicken

PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_2.json --json
{
"winners": [
"Charlotte"
],
"players": [
{
"name": "Peter",
"money": 5,
"space": "Lanzhou Beef Noodle",
"bankrupt": false
},
{
"name": "Billy",
"money": 20,
"space": "Fast Kebabs",
"bankrupt": false
},
{
"name": "Charlotte",
"money": 29,
"space": "GO",
"bankrupt": false
},
{
"name": "Sweedal",
"money": 0,
"space": "Massizim",
"bankrupt": true
}
],
"bankruptPlayer": "Sweedal",
"rollsPlayed": 36,
"rollsTotal": 100,
"endedBy": "bankruptcy"
}

.....Pasted from output...... without --json
dotnet run board.json rolls_3.json
Found GO space at index 0

Winner: Sweedal (all 10 rolls were played and nobody went bankrupt)

Peter $14 on The Grand Tofu
Billy $8 on Betty's Burgers
Charlotte $15 on The Burvale
Sweedal $16 on YOMG

## Test Handled

**Rolls Test** Rolls validation checks includes Empty array, string and whole number validation

**Test 1: "1" string in the roll file**
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Found GO space at index 0
Error: Invalid roll at position 0: "1" is not a positive whole number

**Test 2: rolls file input file contain - [,]**
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Found GO space at index 0
Error: rolls file is not valid JSON: ',' is an invalid start of a value. LineNumber: 1 | BytePositionInLine: 2.

**Test 3: Roll file contain Empty Array - []**
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Found GO space at index 0
Error: rolls cannot be empty in woven monopoly

**Board.json Test**

**Test 1: Removed GO space from board.json**
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Error: board must have exactly one GO space, found 0

**Test 2: property space must contain name, type, price, color**
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Error: space is missing a valid 'type' string
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Error: space 'The Burvale': price must be a positive integer
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp> dotnet run board.json rolls_1.json
Error: space is missing a valid 'colour' string
PS E:\Pronto\Pronto Woven Coding Test\WovenMonopolyCSharp>

````

## Build

```powershell
dotnet build
````
